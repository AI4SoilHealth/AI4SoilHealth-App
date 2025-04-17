/**
 * @desc A mixin object containing methods related to editing objects on the map.
 * @module MapEditingMixin
 */
import { Draw } from "ol/interaction";
import { GeoJSON } from "ol/format";
import { noModifierKeys, primaryAction } from 'ol/events/condition';
import { getArea, getLength } from 'ol/sphere';
import Feature from "ol/Feature";
import { LineString, Point }  from "ol/geom";
import { fromLonLat } from "ol/proj";

export const MapEditingMixin = {
    methods: {
        /**
         * Reads custom features from the server and adds them to the editable source.
         * @returns {Promise<void>} A promise that resolves when the features are added.
         */
        async readCustomFeatures() {
            if (this.$store.userData == null) return;
            if (!this.customFeaturesAPIParams) this.customFeaturesAPIParams = {};
            //this.customFeaturesAPIParams.srid = this.srid;
            let ret = await this.get(this.customFeaturesAPI, this.customFeaturesAPIParams, true);
            this.editableSource.clear();

            if (ret && ret.features) {
                let features = this.format.readFeatures(ret);
                this.reprojectFeatures(features, this.projectionForSave, this.projection);
                this.editableSource.addFeatures(features);
                if (this.zoomToEditableSource) this.animateToExtent(this.editableSource);
            }
        },      
        
        /**
         * Clears the interactions on the map.
         */
        clearInteractions() {
            this.map.removeInteraction(this.draw);
        },

        /**
         * Clones a feature and transforms its geometry to a different projection.
         * @param {Feature} feature - The feature to clone.
         * @param {GeoJSON} format - The format used to serialize the feature.
         * @param {string} projection - The projection of the feature's geometry.
         * @param {string} projectionForSave - The projection to transform the feature's geometry to before saving.
         * @returns {Object} The cloned feature as a GeoJSON object.
         */
        cloneFeature(feature, format, projection, projectionForSave) {
            let clonedFeature = feature.clone();
            if (format) {
                clonedFeature.getGeometry().transform(projection, projectionForSave);
            }
            format = format ?? new GeoJSON();
            return format.writeFeatureObject(clonedFeature);
        },

        /**
         * Saves the geometry of a feature by sending it to the server.
         * @param {feature} feature - The feature to be saved.
         * @param {format} format - The format used to serialize the feature.
         * @param {projection} projection - The projection of the feature's geometry.
         * @param {projection} projectionForSave - The projection to transform the feature's geometry to before saving.
         * @returns {Promise<void>} A promise that resolves when the feature is successfully saved.
         */
        async saveFeature(feature) {
            let gj = this.cloneFeature(feature, this.format, this.projection, this.projectionForSave);

            let params = this.saveFeatureAPIParams ?? {};
            for (let key in this.saveFeatureAPIParams) {
                let value = feature.get(key);
                if (value) {
                    params[key] = value.toString();
                }
            }
            params.feature = JSON.stringify(gj);
            let ret = await this.post(this.saveFeatureAPI, params);
            if (ret) {
                for (let key in ret) {
                    feature.set(key, ret[key]);
                }
            }
        },

        /**
         * Formats the length of a line.
         * @param {LineString} line - The line to calculate the length for.
         * @returns {string} The formatted length with unit (meters or kilometers).
         */
        formatLength (line) {
            const length = getLength(line);
            let output;
            if (length > 100) {
                output = Math.round((length / 1000) * 100) / 100 + ' km';
            } else {
                output = Math.round(length * 100) / 100 + ' m';
            }
            return output;
        },

        /**
         * Calculates the area of a polygon and formats it as a string.
         * If the area is greater than 10000, it is formatted in square kilometers (km²).
         * Otherwise, it is formatted in square meters (m²).
         * @param {Polygon} polygon - The polygon for which to calculate the area.
         * @returns {string} The formatted area string.
         */
        formatArea (polygon) {
            const area = getArea(polygon);
            let output;
            if (area > 10000) {
                output = Math.round((area / 1000000) * 100) / 100 + ' km\xB2';
            } else {
                output = Math.round(area * 100) / 100 + ' m\xB2';
            }
            return output;
        },

        /**
         * Gets the area of a given feature.
         * Only works for features with a Polygon geometry type.
         *
         * @param {feature} feature.
         */
        getFeatureArea(feature) {
            if (feature.getGeometry().getType() == "Polygon") {
                return getArea(feature.getGeometry()).toFixed(0);
            } else {
                return null;
            }
        },
        
        /**
         * Creates a new object for editing on the map.
         * 
         * @param {Object} options - The options for creating the new object.
         * @param {string} options.type - The type of object to create.
         * @param {number} options.geometry_type_id - The ID of the geometry type.
         */
        newObject(options) {
            let actionTypes = {
                'Point': 1,
                'LineString': 3,
                'Polygon': 2
            };
            this.showEditableSource = true;
            this.clearInteractions();
            this.draw = new Draw({
                type: options.type,
                source: this.editableSource,
                stopClick: true,
                condition: (e) => noModifierKeys(e) && primaryAction(e),
                style: this.selectedStyleFunction,
            });                 
            this.map.addInteraction(this.draw);
            //this.map.addInteraction(this.select);
            this.activeAction = options;
            this.draw.on('drawend', async (e) => {
                e.feature.set("geometry_type_id", actionTypes[this.activeAction.type]);
                e.feature.set("props", []);
                e.feature.set("editor", options.editor);
                this.clearInteractions();
                this.selectedFeature = e.feature;
                this.draw = null;
                this.editProps(options);
            });
        },

        /**
         * Ends the current object editing mode.
         */
        endObject() {
            this.clearInteractions();
            this.activeAction = null;
            if (this.select) {
                this.select.getFeatures().clear();
                this.selectCount = 0;
            }
        },

        /**
         * Deletes the selected object.
         * @async
         * @function deleteObject
         * @returns {Promise<void>}
         */
        async deleteObject() {
            if (await this.confirmDialog(this.$t("Delete selected object?"))) {
                await this.delete("User/DeleteCustomGeometry/" + this.selectedFeature.get("key").toString());
                this.editableSource.removeFeature(this.selectedFeature);
                this.selectedFeature = null;
            };
        },

        filesUrl() {
            return [{
                url: this.axios.API.defaults.baseURL + "User/GetImage/"
                    + this.selectedFeature.get("image_id") + "/"
                    + this.selectedFeature.get("extension"),
                compass: this.selectedFeature.get("compass"),
                image_id : this.selectedFeature.get("image_id"),
                name: this.selectedFeature.get("name")
            }];
        },

        compassChanged(compass, id) {
            console.log("Compass changed to " + compass);
            this.selectedFeature.set("compass", compass);
            let features = this.editableLayer.getSource().getFeatures(); // Save the current features
            this.editableLayer.changed();
            this.editableLayer.getSource().refresh();
            this.editableLayer.getSource().addFeatures(features);
        },

        specificEditorSave() {
        
                // if we are editing a feature, update the feature
            if (this.parent && this.parent.selectedFeature) {
                this.parent.selectedFeature.set('id', this.parent.selectedFeature.get('tableAPI' + ret.id));

                if (this.editingRow.lon && this.editingRow.lat) {
                    let point = new Point([this.editingRow.lon, this.editingRow.lat]);
                    let transformed = point.transform(this.parent.projectionForSave, this.parent.projection);
                    this.parent.selectedFeature.setGeometry(transformed);
                }

                for (let key in this.editingRow) {
                    let realKey = key;
                    if (key == 'id') realKey = 'key';
                    this.parent.selectedFeature.set(realKey, this.editingRow[key]);
                }
            }
                
        },

        /**
         * Opens the property editor or the image viewer for the selected feature.
         * @param {Object} options - The options for editing the properties.
         */
        editProps(options) {
            if (this.selectedFeature) {
                if (this.selectedFeature.get("image_id")) {
                    this.initPopup( { component: "image-viewer", maximized: true, files : this.filesUrl(), index: 0, needsAuthentication: true, compassChangedFunction : this.compassChanged });
                } else if (this.selectedFeature.get("editor")) {
                    let tableAPI = this.selectedFeature.get("tableAPI");
                    if (!tableAPI && options) tableAPI = options.tableAPI;
                    this.initPopup( {
                        component : this.selectedFeature.get("editor"),
                        id : this.selectedFeature.get("key"),
                        tableAPI : tableAPI,
                        specificEditorSave : this.specificEditorSave,
                        title : this.$t("Location"),
                        feature : this.selectedFeature,
                        help: "Edit properties",
                        persistent: true
                    });
                } else {
                    this.showProps();
                }
            }
        },

    },
}