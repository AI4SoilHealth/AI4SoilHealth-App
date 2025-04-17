/**
 * @desc A mixin object containing methods related to map events
 * @module MapEventssMixin
 */
import { get, toLonLat } from 'ol/proj';
import Geolocation from "ol/Geolocation";
import Feature from "ol/Feature";
import Point from "ol/geom/Point";
import { MapEditingMixin } from './ol-map-editing';

export const MapEventsMixin = {
    mixins: [MapEditingMixin],
    data() {
        return {
        }
    },
    methods: {

        /**
         * Returns tiffLayer and selectedTitle based on the pixel value.
         * @param {*} pixel 
         * @returns {Object} An object containing the selectedTitle and tiffLayer.
         */
        getActiveTiffLayer(pixel) {
            const mapSize = this.map.getSize();

            let selectedTitle = this.selectedTitle;
            let tiffLayer = this.tiffLayer;

            if (this.altSelectedCatalog && pixel[0] > mapSize[0] * this.compare) {
                selectedTitle = this.altSelectedTitle;
                tiffLayer = this.altTiffLayer;
            }
            return { selectedTitle, tiffLayer };
        },

        /**
         * Returns the value at the pixel location.
         * @param {*} pixel
         * @returns {String} The value at the pixel location.
         */
        getValueAtPixel(pixel) {
            let r = this.getActiveTiffLayer(pixel);
            let selectedTitle = r.selectedTitle;
            let tiffLayer = r.tiffLayer;

            let d = tiffLayer.getData(pixel);
            
            let value = null;

            if (d && d[0] != selectedTitle.no_data && d[0] != 0) {
                if (selectedTitle.numerical) {
                    value = this.rescale(d[0], selectedTitle.scale_factor).toFixed(selectedTitle.decimals);
                } else {
                    if (selectedTitle.description) value = selectedTitle.description[d[0]].name;
                }
            }
            return value;
        },

        /**
         * Creates event listeners for various map events.
         */
        createEvents() {

            this.map.on("error", e => {
                this.showError(e);
            });

            this.map.on("rendercomplete", e => {
                this.$store.working = false;
            });

            this.map.on('pointermove', e => {
                
                const pixel = this.map.getEventPixel(e.originalEvent);
                const hit = this.map.hasFeatureAtPixel(pixel);
                this.cursorXY = this.map.getCoordinateFromPixel(pixel);
                this.cursorPixel = pixel;
                this.cursorPosition = toLonLat(this.cursorXY, this.projection);
                
                let value = null, name = null;
                this.infoTip.style.visibility = 'hidden';

                if (this.selectedTitle != null) {          
                    value = this.getValueAtPixel(pixel);
                }

                if (hit) {
                    for (let f of this.map.getFeaturesAtPixel(e.pixel)) {
                        while (f.values_.features) f = f.values_.features[0];
                        name = f.get("name") ?? f.get("Name");
                        if (name == "positionFeature" || name == "accuracyFeature") {
                            name = null;
                            continue;
                        }
                        value = f.get("value") ?? f.get("description") ?? value;
                        break;
                    }
                }

                if (name || value) {
                    this.$refs.map.style.cursor = 'pointer';
                    this.infoTip.style.left = (e.pixel[0] + 10) + 'px';
                    this.infoTip.style.top = (e.pixel[1] - 10) + 'px';
                    let text = (name ?? "") + (value ? " " : "") + (value ?? "");
                    this.infoTip.innerText = text;
                    this.infoTip.style.visibility = 'visible';
                } else {
                    this.infoTip.style.visibility = 'hidden';
                    this.$refs.map.style.cursor = '';   
                }

            });
           
            this.map.on('moveend', (e) => {

                this.zoom = this.map.getView().getZoom();

                if (this.pointLayer && this.pointLayer.getSource()) {
                    this.pointLayer.getSource().changed();
                }
                this.extent = this.map.getView().calculateExtent(this.map.getSize());
                this.center = toLonLat(this.map.getView().getCenter(), this.projection);
                this.rotation = this.map.getView().getRotation();
                if (this.crosshair && this.crosshairOverlay) {
                    this.crosshairOverlay.setPosition(this.map.getView().getCenter());
                }
                this.setShapeLayer();
                this.setBoundariesLayer();
                this.setCustomRestrictedLayer();
            });
            
            this.modify.on('modifystart', (e) => {
                let f = e.features.getArray()[0];
                let c = f.clone();
                c.set("orig_id", f.getId());
                this.editStack.push(c);
            });

            this.modify.on('modifyend', (e) => {
                let f = e.features.getArray()[0];
                this.saveFeature(f);
            }); 

            this.map.on('singleclick', (e) => {
    
                if (this.drawVertexByVertex) {
                    window.dispatchEvent(new CustomEvent('vertexAdded', { detail: { coords: this.map.getCoordinateFromPixel(e.pixel) } }));
                    return;
                }

                for (let f of this.map.getFeaturesAtPixel(e.pixel)) {
                    while (f.values_.features) f = f.values_.features[0];
                    let name = f.get("name") ?? f.get("Name");
                    if (name != "positionFeature" && name != "accuracyFeature") {
                        this.infoTip.style.visibility = 'hidden';
                        this.selectedFeature = f;
                        this.editProps();
                        return;
                    }
                }

                if (this.selectedTitle) {
                    //create feature at clicked location
                    let feature = new Feature({
                        geometry: new Point(this.map.getCoordinateFromPixel(e.pixel)),
                    });
                    feature.set("name", "Point");
                    feature.set("x", this.map.getCoordinateFromPixel(e.pixel)[0]);
                    feature.set("y", this.map.getCoordinateFromPixel(e.pixel)[1]);
                    feature.set ("value", this.getValueAtPixel(e.pixel));
                    this.selectedFeature = feature;
                    this.editProps();
                }
            });
        },	
       
        /**
         * Sets up geolocation functionality.
         */
        createGeolocation() {
            let self = this;

            this.geolocation = new Geolocation({
                trackingOptions: {
                    enableHighAccuracy: true,
                },
                projection: this.projection,
            });
            this.geolocation.setTracking(true);

            this.accuracyFeature = new Feature();
            this.accuracyFeature.set("name", "accuracyFeature");

            this.geolocation.on('change:accuracyGeometry', function () {
                self.accuracyFeature.setGeometry(self.geolocation.getAccuracyGeometry());
            });
            
            let pos = this.$store.position ?? [0,0];
            let pointGeometry = new Point(pos);
            this.positionFeature = new Feature({geometry: pointGeometry});
            this.positionFeature.setStyle(this.positionStyle);
            this.positionFeature.set("name", "positionFeature");

            this.geolocation.on('change:position', function () {
                self.positionFeature.getGeometry().setCoordinates(self.geolocation.getPosition());
                self.$store.position = self.geolocation.getPosition();
            });

            this.geolocation.on('error', function (error) {
                alert(error.message);
            });

       }
    },
}