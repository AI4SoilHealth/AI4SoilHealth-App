/**
 * @desc A mixin object containing methods related to map layers
 * @module MapLayersMixin
 */
import { OSM, BingMaps, Tile, ImageTile } from 'ol/source';
import { transform } from "ol/proj";
import { GeoTIFF as GeoTIFFSource } from 'ol/source';
import { get as getProjection } from 'ol/proj';
import { Style, Fill, Stroke } from 'ol/style';
import { Vector as VectorLayer } from 'ol/layer';
import WebGLVectorLayer from 'ol/layer/WebGLVector';
//import {createXYZ} from 'ol/tilegrid';

export const MapLayersMixin = {
    methods: {

        /**
         * Sets the base layer of the map based on the selectedBaseLayer value.
         * If selectedBaseLayer is 1, sets the base layer source to OpenStreetMap (OSM).
         * If selectedBaseLayer is not 1, sets the base layer source to Bing Maps with the specified key and imagerySet.
         * Updates the map size and forces a component update.
         * @async
         * @returns {Promise<void>}
         */
        async setBaseLayer() {
            if (this.selectedBaseLayer.value == 1) {
                this.baseLayer.setSource(new OSM());
            } else {
                this.baseLayer.setSource(new BingMaps({
                    key: 'AnNHHAjCQ3arTU0FoTUB1zoCdKC2png2M2pstqoV5uFhCHorOx_aUz1Yt8RA6Xly',
                    imagerySet: 'AerialWithLabels',
                }));
            }
            this.baseLayer.getSource().changed();
            this.map.updateSize();
            this.$forceUpdate();
        },

        async setCustomRestrictedLayer() {
            if (this.customRestrictedAPI && this.customRestrictedLayer && this.zoom >= this.customRestrictedLayer.getMinZoom() && this.zoom <= this.customRestrictedLayer.getMaxZoom()) {
                 let res = await this.api(this.axios.API[this.customRestrictedAPI.method ?? "get"], this.customRestrictedAPI.url, 
                    { Srid: this.srid, 
                      Extent: this.extent.join(','),
                      Zoom: Math.round(this.zoom)
                    });  
                 this.customRestrictedSource.clear();
                 this.customRestrictedSource.addFeatures(this.format.readFeatures(res)); 
            }
        },

        /**
         * Clears the shape source and adds features to it based on the selected shape.
         * @async
         * @function setShapeLayer
         * @memberof module:mixins/ol-map-layers
         * @returns {Promise<void>} A promise that resolves when the shape layer is set.
         */
        // async setShapeLayer() {
        //     this.shapeSource.clear();
        //     if (this.selectedSite) {
        //         let ret = await this.get("Shape/GetGeometryOf", {
        //             Id: this.selectedSite.value,
        //             Srid: this.srid,
        //             GeometryTypeId: '1',
        //         });
        //         if (ret && ret.features) {
        //             let features = this.format.readFeatures(ret);
        //             this.shapeSource.addFeatures(features);
        //             this.animateToExtent(this.shapeSource);
        //         }
        //     }
        // },

        /**
         * Creates a point collection feature from the given data.
         *
         * @param {Object} data - The data used to create the point collection.
         * @param {string} projection - The projection used for transforming coordinates.
         * @returns {Object} - The created point collection feature.
         */
        createPointCollection(data, projection) {
            let self = this;
            var featureCollection = {
                type: 'FeatureCollection',
                features: data.data.map(function(row) {
                    return {
                        type: 'Feature',
                        geometry: {
                            type: 'Point',
                            coordinates: transform([row[0], row[1]], 'EPSG:4326', projection)
                        },
                        properties: {
                            value: row[2],
                            date: row[3],
                            color_code: row[4] ?? '#ff0000',
                            name: row[8] || row[5],
                            nuts_0: row[6],
                            description: row[7],
                            point_id: row[9],
                            sample: row[10],
                            has_spectrum: row[11],
                            data_source_order_no: self.selectedDataSourceIds.indexOf(row[12]),
                        },
                    };
                })
            };  
            return featureCollection;
        },

                /**
         * Fetches and sets the indicator values for the map.
         * 
         * @returns {Promise<void>} A promise that resolves when all asynchronous operations are complete.
         */
        async getIndicatorValues() {
            this.$store.working = true;
            await this.$nextTick(); // Wait for the DOM to update before proceeding
            if (this.selectedIndicator) {
                this.legend = await this.get("User/GetIndicatorLegend", { indicatorId: this.selectedIndicator.value });
                if (!this.legend) this.legend = [];
            } else {
                this.legend = [];
            }
            
            await this.setPointLayer();
            await this.setBoundariesLayer();
            this.$store.working = false;
        },

        /**
         * Sets the point layer on the map.
         * @async
         * @returns {Promise<void>} A promise that resolves when the point layer is set.
         */
        async setPointLayer() {
            this.pointSource.clear();
            this.pointFeatures = [];

            if (!this.showPoints) return;
            if (!this.selectedIndicator) return;
            let data = await this.get("User/GetIndicatorValues", { 
                indicatorId: this.selectedIndicator.value, 
                dataSourceId: this.selectedDataSourceIds.join(","), 
                valueFrom: this.valueFrom, 
                valueTo: this.valueTo,
                depthId: this.selectedIndicator.depth.value });   
            let pointCollection = this.createPointCollection(data, this.projection);
            this.pointFeatures = this.format.readFeatures(pointCollection);
            this.pointSource.addFeatures(this.pointFeatures);
         },
        
        async setMapTilerLayer() {
            if (!this.mapTilerSource) return;
            let mapTiler = this.mapTilerSource.toString().replaceAll ("'", '"');
            this.mapTilerLayer.setSource(new ImageTile({url: mapTiler}));
            this.mapTilerLayer.changed();
            this.mapTilerLayer.setVisible(true);
        },

        /**
         * Sets the tile layer containig GeoTiff sources for the map.
         * @async
         * @returns {Promise<void>} A promise that resolves when the tile layer is set.
         */
        async setTileLayer(selectedTitle, selectedCatalog, tiffLayer, tiffLegend, alt) {
            this.$store.working = true;
            // create appropriate source
            if (selectedCatalog) {
                let source = new GeoTIFFSource({
                    normalize: false,
                    sources: [{ url: selectedCatalog.source }],
                    projection: getProjection("EPSG:" + selectedTitle.srid),
                    //convertToRGB: true,
                    interpolate: false
                });               
                tiffLayer.setSource(source);  
                source.on("change", () => {
                    if (source.getState() === "error") {
                        this.showError(this.$t("Error loading GeoTIFF") + ": " + source.getError());
                        return;
                    } else if (source.getState() === "ready") {
                        console.log("source ready", source);
                        selectedTitle.tiffLayer = tiffLayer;             
                        this.createLegend(selectedTitle, tiffLegend);
                        // tiffLayer.setStyle({
                        //     color: JSON.parse(selectedTitle.color_map)
                        // }); 
                        tiffLayer.changed();
                        tiffLayer.setVisible(true);
                    }
                });

                if (selectedTitle.max_zoom) {	
                    tiffLayer.setMaxZoom(selectedTitle.max_zoom);	
                } else {
                    tiffLayer.setMaxZoom(30);
                }
                if (selectedTitle.custom_restricted_api) {
                    this.customRestrictedAPI = selectedTitle.custom_restricted_api;
                    this.customRestrictedLayer.setMinZoom(this.customRestrictedAPI.minZoom ?? 0);
                }     

            } else {
                tiffLayer.setSource(null);
                tiffLayer.setVisible(false);
            }
            this.$store.working = false;
        },

        /**
         * Sets the boundaries layer on the map based on the selected NUTS level.
         * If the NUTS level is greater than or equal to 0, it makes an asynchronous request to retrieve the boundaries data.
         * The retrieved data is then added to the boundaries source of the map.
         * If the NUTS level is less than 0, the boundaries source is cleared.
         * @returns {Promise<void>} A promise that resolves when the boundaries layer is set.
         */
        async setBoundariesLayer() {
            if (this.nutsLevel && this.nutsLevel.value >= 0) {
                let featureCollection = await this.get("User/GetNutsBoundaries", {
                    level: this.nutsLevel.value, 
                    srid: this.srid, 
                    zoom: Math.round(this.zoom), 
                    extent: this.extent.join(","), 
                    indicatorId: this.selectedIndicator ? this.selectedIndicator.value : null, 
                    dataSourceId: this.selectedDataSourceIds.join(","),
                    depthId: this.selectedIndicator ? this.selectedIndicator.depth.value : null,
                });
                this.boundariesSource.clear();
                if (featureCollection.features) {
                    this.boundariesSource.addFeatures(this.format.readFeatures(featureCollection));
                }
            } else {
                this.boundariesSource.clear();
            }
        },


        async setShapeLayer() {
            if (this.shapeAPI) {
                this.$store.working = true;
                this.shapeAPI.params = this.shapeAPI.params ?? {};
                this.shapeAPI.params.Zoom = Math.round(this.zoom);
                this.shapeAPI.params.Extent = this.extent.join(",");
                let shapes = await this.api(this.axios.API[this.shapeAPI.method ?? "get"], this.shapeAPI.url, this.shapeAPI.params);
                this.shapeSource.clear();
                if (shapes && shapes.features) {
                    this.shapeSource.addFeatures(this.format.readFeatures(shapes));
                    // this.shapeSource.forEachFeature(feature => {
                    //     feature.setStyle(
                    //         new Style({
                    //             stroke: new Stroke({
                    //                 color: feature.get('color_code') || '#000000', // Default to black if no color_code
                    //                 width: 1,
                    //             }),
                    //             fill: new Fill({
                    //                 color: feature.get('fill_color_code') || 'rgba(255, 255, 255, 0.5)', // Default to semi-transparent white
                    //             }),
                    //         })
                    //     );
                    // });
                }
                this.$store.working = false;
            }
        }
    },
}