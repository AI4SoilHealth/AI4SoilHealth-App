<template>
    <div>
        <Header v-if="!parentPopup" :name="$route.name" :title="title ?? $t($route.name)" :backButton="backButton" />
        
        <Toolbar ref="toolbar" v-if="toolbar" v-model="toolbarOpen" v-model:toolbarHeight="toolbarHeight" width="100%" :toolbarCloseable="toolbarCloseable"
            @close="toolbarOpen = false">
            <component :is="toolbarComponent" :parent="this" />
        </Toolbar>

        <q-btn v-if="toolbar" style="position:absolute; top:40px; right:5px; z-index: 1000;" @click="toolbarOpen = true"
            padding="xs" icon="settings" text-color="black" size="sm" />

        <div id="map-container" class="map-container q-pa-none q-ma-none" :style="mapContainerStyle" tabIndex="0">

            <div class="map q-pa-none q-ma-none" ref="map" />

            <q-slider v-if="altSelectedCatalog" z-index=2000 v-model="compare" :min="0" :max="1" :step="0.001" dense style="position: absolute; bottom: 4px; left: 0px; width: 100%;" @update:model-value="render()"/>

            <i v-if="crosshair" id="crosshair" class="crosshair material-icons">close</i>
    
            <OlMapLegend v-if="selectedCatalog || selectedIndicator" :options="legend" :optionsTiff="tiffLegend" :parent= "this" :selectedTitle="selectedTitle" :selectedCatalog="selectedCatalog" />

            <OlMapLegend v-if="altSelectedCatalog" :optionsTiff="altTiffLegend" :parent= "this" :rightLegend="true" :selectedTitle="altSelectedTitle" :selectedCatalog="altSelectedCatalog" />

            <div ref="infoTip" class="infoTip"/>
            <div id="info" v-if="info">{{ `zoom ${zoom.toFixed(1)}, center
                ${center[0].toFixed(3)},
                ${center[1].toFixed(3)}, pixel: ${valueAtPixel}, lon/lat: ${cursorPosition[0].toFixed(3) } ${ cursorPosition[1].toFixed(3) },
                xy: ${ cursorXY[0].toFixed(0) } ${ cursorXY[1].toFixed(0) }, c: ${ ($store.compass ?? 0).toFixed(0) } o: ${ opacity } th: ${ toolbarHeight }` }}
            </div>
        </div>
        
        <q-btn-group flat class="btnGroup">

            <q-btn round flat dense icon="content_copy" @click="exportMap(false)" text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("Copy to clipboard") }}</q-tooltip>
            </q-btn>
            <q-btn round flat dense icon="download" @click="exportMap(true)" text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("Download as image") }}</q-tooltip>
            </q-btn>
            <q-btn v-if="showEditableSource || shapeAPI" round flat dense icon="image_aspect_ratio" @click="animateToExtent(null)" text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("Fit to features") }}</q-tooltip>
            </q-btn>
            <q-btn v-if="rotation != 0" round flat dense icon="north" @click="map.getView().setRotation(0)"
                text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("North orientation") }}</q-tooltip>
            </q-btn>
            <q-btn v-if="trackPosition" round flat dense :icon="$icons.my_location"
                @click="map.getView().setCenter(geolocation.getPosition())" text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("Center at GPS") }}</q-tooltip>
            </q-btn>  
            <q-btn round flat dense icon="layers"
                @click="showLayers()" text-color="black">
                <q-tooltip anchor="center left" self="center right">{{ $t("Layers") }}</q-tooltip>
            </q-btn>    
            <q-slider v-if="selectedCatalog || (additionalTiff && additionalTiff.maxZoom >= this.zoom)"  class="semi-transparent q-pl-sm" v-model="opacity" :min="0.0" :max="1." :step="0.05" dense label :label-value="$t('Opacity: ' + opacity)" reverse vertical switch-label-side/>          
        </q-btn-group>
    </div>
</template>

<script>
/**
 * OpenLayers map component.
 * 
 * @component
 * @name OlMap
 * @example
 * <OlMap />
 
 */

// #region imports 
import "ol/ol.css";
import { Map, View, Feature, Overlay } from "ol";

import GeoJSON from 'ol/format/GeoJSON';
import { OSM, Vector as VectorSource,  BingMaps } from "ol/source";
import { Vector as VectorLayer } from "ol/layer";
import WebGLTile from 'ol/layer/WebGLTile.js';
import WebGLVectorLayer from "ol/layer/WebGLVector.js";
import Tile from 'ol/layer/Tile.js';
import { defaults as defaultInteractions, DragRotateAndZoom, Modify } from "ol/interaction";
//import { defaults as defaultControls } from 'ol/control/defaults';
import { fromLonLat } from "ol/proj";
import { get as getProjection, useGeographic } from 'ol/proj';
import proj4 from 'proj4';
import { register } from 'ol/proj/proj4.js';
import { MapMixin } from '../mixins/ol-map.js';
import { MapEditingMixin } from '../mixins/ol-map-editing.js';
import { MapStylesMixin } from '../mixins/ol-map-styles.js';
import { MapEventsMixin } from '../mixins/ol-map-events.js';
import { MapLayersMixin } from '../mixins/ol-map-layers.js';
import { loadComponent } from '@/common/component-loader';
import { GeoTIFF as GeoTIFFSource } from 'ol/source';
import {getRenderPixel} from 'ol/render.js';

// #endregion

export default {
    name: 'OlMap',
    mixins: [MapMixin, MapEditingMixin, MapStylesMixin, MapEventsMixin, MapLayersMixin],
    props: ["parentPopup"],
    components: {
        OlMapPopup: loadComponent('ol-map-popup'),
        OlMapLegend: loadComponent('ol-map-legend'),
        Toolbar: loadComponent('toolbar'),
        ContextMenu: loadComponent('context-menu'),
        Autocomplete: loadComponent('autocomplete'),
        DatacubeSelector: loadComponent('datacube-selector'),
    },
    beforeRouteEnter(to, from, next) {
        next(vm => { vm.init(to.name); });
    },
    beforeRouteUpdate(to, from, next) {
        this.routeKey++;
        this.init(to.name);
        next();
    },
    beforeRouteLeave(to, from, next) {
        if (this.activeAction != null) {
            this.endObject();
        }
        this.geolocation.setTracking(false);
        this.saveStorable(this, from.name);
        next();
    },
    computed: {
        toolbarComponent: function () {
            if (!this.toolbar) return null;
            return loadComponent(this.toolbar);
            //return defineAsyncComponent(() => import(`../../${this.toolbar}.vue`));
        },

        mapContainerStyle : function () {
            return {
                top: ((this.parentPopup ? 0 : 40) + (this.toolbarHeight ?? 0)) + 'px',
                height: ((this.parentPopup ? 28 : 0) + this.$q.screen.height - 80 - (this.toolbarHeight ?? 0)) + 'px'
            }
        }
    },
    watch: {
        valueAtPixel: function (val) {
            this.valueAtPixelStyle = {
                position: "absolute",
                top: (this.cursorPixel[1] - 30) + "px",
                left: this.cursorPixel[0] + "px",
                backgroundColor: "white",
                padding: "3px",
                borderRadius: "5px",
            }
        },

        selectedSite: function (val) {
            this.setShapeLayer();
        },
        selectedBaseLayer: function (val) {
            this.setBaseLayer();
        },
        showEditableSource: function (val) {
            this.editableLayer.setVisible(val);
        },
        nutsLevel: function (val) {
            this.setBoundariesLayer();
        },
        showPoints: function (val) {
            this.setPointLayer();
        },
        srid: function (val) {
            this.setSrid(val)
        },
        trackPosition: function (val) {
            this.geolocationLayer.setVisible(val);
            this.geolocation.setTracking(val);
        },
        opacity: function (val) {
            this.tiffLayer.setOpacity(val);
            if (this.altTiffLayer) this.altTiffLayer.setOpacity(val);
            if (this.additionalTiffLayer) this.additionalTiffLayer.setOpacity(val);
        },
        mapTilerOpacity: function (val) {
            this.mapTilerLayer.setOpacity(val);
        },
        selectedIndicator: async function (val) {
            this.valueFrom = null;
            this.valueTo = null;
        },
        selectedDataSource: function (val) {
            this.selectedIndicator = null;
            this.setPointLayer();
            this.setBoundariesLayer();
        },
        selectedCatalog: function (val) {
            this.setTileLayer(this.selectedTitle, this.selectedCatalog, this.tiffLayer, this.tiffLegend, false);
        },
        altSelectedCatalog: function (val) {
            this.setTileLayer(this.altSelectedTitle, this.altSelectedCatalog, this.altTiffLayer, this.altTiffLegend, true);
        },

    },
    data: function () {
        return {
            toolbarHeight: 0,
            compare: 0.5,
            altSelectedCatalog: null,
            altSelectedTitle: null,
            altTiffLayer: null,
            tiffLayer: null,
            routeKey: 0,
            storable: ["toolbarOpen", "srid", "center", "zoom", "rotation", "opacity", "mapTilerOpacity", "trackPosition", "showEditableSource", "selectedBaseLayer", "info"],
            dialog: false,
            srid: null,
            toolbar: null,
            toolbarOpen: false,
            toolbarCloseable: true,
            center: [12, 51],
            cursorPosition: [12, 51],
            cursorXY: [0, 0],
            cursorPixel: [0, 0],
            pointFeatures: [],
            zoom: 5,

            map: null,
            view: null,
            vectorSource: null,
            overlay: null,
            projectionForSave: null,
            projection: getProjection("EPSG:3857"),
            extent: null,
            format: new GeoJSON(),
            trackPosition: false,

            rotation: 0,

            type: null,
            geom: null,
            source: null,
            opacity: 0.5,
            mapTilerOpacity: 0.5,
            styleCache: {},
            nutsLevel: null,
            showPoints: true,
            selectedIndicator: null,
            selectedTitle: null,
            sources: [],
            selectedCatalog: null,
            legend: [],
            tiffLegend: [],
            altTiffLegend: [],

            activeAction: null,
            showEditableSource: false,
            select: null,
            modify: null,
            snap: null,
            draw: null,
            selectedFeature: null,
            valueAtPixel: null,
            pixelRatio: 1,
            moveTimeout: null,
            selectedBaseLayer: { value: 1, label: "OSM" },
            //selectedDataSource: { value: 1, label: "LUCAS 2018" },
            selectedDataSource: [], // [{ value: 1, label: "LUCAS 2018" }],
            selectedDataSourceIds: [],
            selectedSite: null,
            crosshairOverlay: null,
            crosshair: false,

            params: null,
            baseLayers: [
                { value: 1, label: "OSM" },
                { value: 2, label: "Bing sat" }
            ],

            pointSource: null,
            pointLayer: null,

            editableLayer: null,
            editableSource: null,

            boundariesSource: null,
            boundariesLayer: null,

            shapeSource: null,
            shapeLayer: null,
            shapeAPI: null,
            geolocation: null,
            accuracyFeature: null,
            positionFeature: null,
            geolocationSource: null,
            geolocationLayer: null,

            editStack: [],
            drawVertexByVertex: false,

            infoTip: null,
            info: false,
            title: null,

            backButton: false,
            valueFrom: null,
            valueTo: null,

            layersProps: {
                baseLayers: this.baseLayers,
                selectedBaseLayer: this.selectedBaseLayer,
            },

            additionalTiff: null,
            additionalTiffSource: null,     
            additionalTiffLayer: null,

            pointStyle : {
                "shape-points":
                [
                    'match', 
                    ['get', 'data_source_order_no'],
                    0, 16, 1, 3, 0
                ],
                "shape-radius" : [
                        'interpolate', ['linear'], ['zoom'],
                        5, 2,
                        10, 5,
                        15, 10,
                        20, 15
                    ],
                //"shape-fill-color": ['interpolate', ['linear'], ['get', 'value'], 0, '#000000'],
                "shape-fill-color": ['get', 'color_code'],
            },
            
            boundaryStyle : {
                'stroke-color': ['get', 'color_code'], //['*', ['get', 'color_code'], '#FF0000'],
                'stroke-width': 1,
                //'stroke-offset': 0,
                'fill-color': ['get', 'fill_color_code'],          
            },
        };
    },

    mounted() {
        if (this.parentPopup) this.init(this.parentPopup.path);
    },

    methods: {
        /**
         * Renders the map.
         */
        render() {
            this.map.render();
            if (this.infoTip.style.visibility == 'visible' && this.selectedTitle) {
                let value = this.getValueAtPixel(this.cursorPixel   );
                this.infoTip.innerHTML = value;
            }
        },


        /**
         * Clips the rendering of a layer based on the specified comparison ratio and direction.
         * 
         * @param {Object} event - The rendering event object containing the WebGL context.
         * @param {number} compare - The ratio (0 to 1) to determine the clipping position.
         * @param {boolean} right - If true, clips the right side of the layer; otherwise, clips the left side.
         */
        clipLayer(event, compare, right) {
            const gl = event.context;
            gl.enable(gl.SCISSOR_TEST);

            const mapSize = this.map.getSize();

            const bottomLeft = getRenderPixel(event, [0, mapSize[1]]);
            const topRight = getRenderPixel(event, [mapSize[0], 0]);

            const mapWidth = topRight[0] - bottomLeft[0];
            const width = Math.round(mapWidth * compare);
            const height = topRight[1] - bottomLeft[1];

            if (right) {
                gl.scissor(bottomLeft[0] + width, bottomLeft[1], mapWidth - width, height);
            } else {
                gl.scissor(bottomLeft[0], bottomLeft[1], width, height);
            }

            gl.clear(gl.COLOR_BUFFER_BIT); // Optionally clear the scissor region
        },

        /**
         * Exports the map as an image.
         * 
         * @param {boolean} download - Indicates whether the image should be downloaded.
         */
        async exportMap(download) {
            await this.export(download, 'map-container', 'map.png');
            return;
        },

        /**
         * Handles the click event on a popup.
         * 
         * @param {Object} feature - The feature object associated with the clicked popup.
         */
        async popupClicked(feature) {
            this.$refs.popup.hide();
            let osm_id = feature.get('osm_id');
            let result = await this.get(this.api2, { OsmId: osm_id });
            this.vectorSource.addFeatures(this.format.readFeatures(result));
        },


        /**
         * Initializes the component.
         * 
         * @param {string} routeName - The name of the route.
         */
        async init(routeName) {

            proj4.defs("EPSG:4258", "+proj=longlat +ellps=GRS80 +no_defs +type=crs");
            proj4.defs("EPSG:3035", "+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs +type=crs");
            register(proj4);
            let self = this;
            await this.$nextTick();

            this.toolbarHeight = 0;
            this.toolbarOpen = false;
            this.toolbarCloseable = true;
            this.toolbar = null;

            if (this.$refs.toolbar && this.toolbar) {
                console.log("invoke calcToolbar");
                this.$refs.toolbar.calcToolbarHeight();
            }
            this.infoTip = this.$refs.infoTip;  //document.getElementById('infoTip');
            this.selectedTitle = null;
            this.selectedCatalog = null;
            this.altSelectedCatalog = null;
            this.selectedIndicator = null;
            this.selectedTitle = null;
            this.selectedSite = null;
            this.crosshair = false;
            this.toolbarCloseable = true;
            this.editStack = [];
            this.params = null;
            this.backButton = false;
            this.toolbar = null;
            this.tiffSource = null;
            this.additionalTiffSource = null;
            this.mapTilerSource = false;
            this.customFeaturesAPI = null;
            this.zoomToEditableSource = false;
            this.customRestrictedLayer = null;
            this.customRestrictedAPI = null;
            this.additionalTiffLayer = null;
            this.additionalTiff = null;
            this.selectedTitleIndicatorId = null;

            if (!this.$store.catalogs.sourceTitles) {
                this.$store.catalogs.sourceTitles = await this.get("DataCube/GetTitles");
            }

            this.projectionForSave = getProjection("EPSG:4326");
            this.createGeolocation();

            this.loadStorable(this, routeName);

            this.initializeComponent(this.parentPopup);

            if (!this.trackPosition) this.geolocation.setTracking(false);
            // if this.srid is not an object, make it one
            this.srid = this.srid ?? 3035;
            this.projection = this.projection ?? "EPSG:" + this.srid;

            this.projection = "EPSG:" + this.srid;

            this.pointSource = new VectorSource({ format: this.format });
            this.editableSource = new VectorSource({ format: this.format });
            this.boundariesSource = new VectorSource({ format: this.format });
            this.shapeSource = new VectorSource({ format: this.format });

            this.geolocationSource = new VectorSource({
                format: this.format,
                projection: this.projection,
                features: [this.accuracyFeature, this.positionFeature]
            });

            //this.baseLayer = new WebGLTile({
                // properties: { visible: true, zIndex: 0, source: null, cacheSize: 1024 },
            this.baseLayer = new Tile({
                properties: { visible: true, zIndex: 0, source: null, cacheSize: 1024 },
            });

            this.baseLayer.on("precompose", e => {
                this.pixelRatio = e.frameState.pixelRatio;
            });

            this.tiffLayer = new WebGLTile({
                properties: {
                    type: "GeoTIFF",
                },
                visible: false,
                opacity: this.opacity,
                zIndex: 1,
                imageSmoothing: false
            });

            this.altTiffLayer = new WebGLTile({
                properties: {
                    type: "GeoTIFF",
                },
                visible: false,
                opacity: this.opacity,
                zIndex: 1,
                //imageSmoothing: false
            });

            this.tiffLayer.on('prerender', (event) => {
                if (!this.altSelectedCatalog) return;
                this.clipLayer(event, this.compare, false);
            });

            this.altTiffLayer.on('prerender', (event) => {
                this.clipLayer(event, this.compare, true);
            });

            this.tiffLayer.on('postrender', function (event) {
                const gl = event.context;
                gl.disable(gl.SCISSOR_TEST);
            });

            this.altTiffLayer.on('postrender', function (event) {
                const gl = event.context;
                gl.disable(gl.SCISSOR_TEST);
            });


            this.mapTilerLayer = new WebGLTile({
                // properties: {
                //     type: "MapTiler",
                // },
                visible: false, opacity: this.mapTilerOpacity, zIndex: 2, source: null,
            });

            this.editableLayer = new VectorLayer({
                source: this.editableSource,
                zIndex: 100,
                style: feature => {
                    if (feature.getGeometry().getType() === 'Point') {
                        return this.getObservationPointStyle(feature, this.zoom);
                    } else {
                        return this.shapeStyle;
                    }
                }
            });

            this.shapeLayer = new WebGLVectorLayer({
                source: this.shapeSource,
                zIndex: 3,
                style: this.boundaryStyle         
            });

            this.boundariesLayer = new WebGLVectorLayer({
                source: this.boundariesSource,
                zIndex: 4,
                style: this.boundaryStyle   
                // style: feature => {
                //     return this.getBoundaryStyle(feature);
                // }
            });

            this.pointLayer = new WebGLVectorLayer({
                zIndex: 5,
                source: this.pointSource,
                opacity: 1,
                style: this.pointStyle,
            });

            if (this.additionalTiff) {

                this.additionalTiffSource = new GeoTIFFSource({
                    normalize: false,
                    sources: [{ url: this.additionalTiff.url }],
                    projection: getProjection("EPSG:" + this.srid),
                    interpolate: false,
                });

                this.additionalTiffLayer = new WebGLTile({
                    properties: {
                        type: "GeoTIFF",
                    },
                    opacity: this.opacity,
                    zIndex: 6,
                    minZoom: this.additionalTiff.minZoom ?? 0,
                    maxZoom: this.additionalTiff.maxZoom ?? 30,
                    source: this.additionalTiffSource,
                    style: {
                        color: JSON.parse(this.additionalTiff.color_map ?? '[[  "interpolate",  [   "linear"  ],  [   "band",   1  ],  0,  [   255,   255,   255,  0  ],  255,  [   0,   0,   0,   1 ] ]]')
                    }
                });
            }

            this.customRestrictedSource = new VectorSource({
                    format: this.format,
            });


            this.customRestrictedLayer = new WebGLVectorLayer({
                source: this.customRestrictedSource,
                zIndex: 7,
                minZoom: this.customRestrictedAPI?.minZoom ?? 0,
                maxZoom: this.customRestrictedAPI?.maxZoom ?? 30,
                style: this.boundaryStyle
            });

            this.geolocationLayer = new VectorLayer({
                source: this.geolocationSource,
                zIndex: 5000,
            });

            this.geolocationLayer.set('interactive', false);

            if (this.map) this.map.dispose();

            this.map = new Map({
                controls: [],
                interactions: defaultInteractions().extend([new DragRotateAndZoom()]),
                target: this.$refs.map,
                layers: [],
                view: new View({
                    projection: this.projection,
                    center: fromLonLat(this.center, this.projection),
                    zoom: this.zoom,
                }),
                // controls: defaultControls({ rotateOptions: { autoHide: false } }),
            });
            this.map.addLayer(this.baseLayer);

            this.map.addLayer(this.tiffLayer);
            this.map.addLayer(this.altTiffLayer);

            this.map.addLayer(this.mapTilerLayer);
            this.map.addLayer(this.boundariesLayer);
            this.map.addLayer(this.shapeLayer);
            this.map.addLayer(this.editableLayer);
            this.map.addLayer(this.pointLayer);
            this.map.addLayer(this.geolocationLayer);

            if (this.additionalTiff) this.map.addLayer(this.additionalTiffLayer);

            this.map.addLayer(this.customRestrictedLayer);

            this.editableLayer.setVisible(this.showEditableSource);
await this.$nextTick();
            // this.select = new Select({
            //     condition: singleClick,
            //     toggleCondition: shiftKeyOnly,
            //     hitTolerance: 5,
            //     style: this.selectedStyleFunction,
            // });

            // this.map.addInteraction(this.select);

            this.modify = new Modify({
                source: this.editableSource,
            });
            this.map.addInteraction(this.modify);

            this.createEvents();
            this.setBaseLayer();
            //this.setTileLayer();
            this.setMapTilerLayer();

            if (this.selectedTitleIndicatorId) {
                this.selectedTitle = this.$store.catalogs.sourceTitles.find(x => x.indicator_id == this.selectedTitleIndicatorId);
                this.selectedTitle.description = this.$store.catalogs.descriptions[this.selectedTitleIndicatorId];
                let sources = await this.get("DataCube/GetShdcFiles", {
                    titleId: this.selectedTitle.value,
                    depth: this.selectedTitleDepth,
                    confidence:  this.selectedTitleConfidence
                });
                this.selectedCatalog = sources[0];
            }

            if (this.customFeaturesAPI) {
                await this.readCustomFeatures();
            }

            this.map.updateSize();
            this.extent = this.map.getView().calculateExtent(this.map.getSize());

            if (this.shapeAPI) {
                await this.setShapeLayer();
                //this.animateToExtent(this.shapeSource);
            }

            if (this.showEditableSource) {
                this.trackPosition = true;
            }

            if (this.selectedDataSource || this.selectedIndicator) {
                this.getIndicatorValues();
            }

                            
        },

        showLayers() {
            this.initPopup( { component: "layers", title : this.$t("Map layers and features") });
        },
    }
}
</script>

<style scoped>
.map-container {
    position: absolute;
    width: 100%;
}

.map {
    width: 100%;
    height: 100%;
    background-color: white !important;
}

#info {
    position:absolute;
    top: 10px;
    left: 0px;
    padding: 5px;
    z-index: 1000;
}

#toolbar {
    padding: 0;
    margin: 0;
}

.btnGroup {
    position: fixed;
    top: 40%;
    right: 5px;
    padding: 5px;
    z-index: 1000;
    flex-direction: column;
}

.crosshair {
    font-size: 30px;
    font-weight: 50;
    color: blue;
    position: absolute;
    top: calc(50% - 15px);
    left: calc(50% - 15px);
    pointer-events: none;
}

.infoTip {
    position: absolute;
    display: inline-block;
    width: auto;
    z-index: 100;
    background-color: #fff;
    color: #000;
    text-align: center;
    border-radius: 4px;
    padding: 5px;
    transform: translate(-60%, -80%);
    /* Center the tip horizontally */
    visibility: hidden;
    pointer-events: none;
}
</style>