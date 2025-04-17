<template>
    <div style="width:100%; height:40px" class="row">
        <q-btn dense flat no-caps v-if="!drawVertexByVertex" @click="addAtGPS" :label="$t('Add point at ')">
            <q-tooltip>Add point at current GPS position</q-tooltip>
            <q-icon class="q-ml-xs"  name="my_location" />
        </q-btn>
        <q-btn dense flat no-caps v-if="!drawVertexByVertex" @click="addAtCenter" :label="$t('Add point at ')">
            <q-tooltip>Add point at croshair position</q-tooltip>
            <q-icon class="q-ml-xs" name="close" />
        </q-btn>
        <q-btn dense flat no-caps v-if="!drawVertexByVertex" :label="$t('Add ...')" >
            <q-tooltip>Click to see other possibilities</q-tooltip>
            <q-menu>               
                <q-list>
                    <q-item clickable v-close-popup @click="addAtMouse">
                        <q-item-section>
                            <div>
                                <q-icon class="q-mr-xs" size="sm" :name="$icons.arrow_selector_tool" />
                                {{ $t("Point at mouse cursor position (click there)") }}
                            </div>
                        </q-item-section>
                    </q-item>
                    <q-item clickable v-close-popup @click="addPolygonMouse">
                        <q-item-section>
                            <div>
                                <q-icon class="q-mr-xs" size="sm" name="polyline" />{{ $t("Polygon (click on vertices)") }}
                            </div>
                        </q-item-section>
                    </q-item>
                    <q-item clickable v-close-popup @click="addPolygonCrosshair">
                        <q-item-section>
                            <div>
                                <q-icon class="q-mr-xs" size="sm" name="polyline" />
                                {{ $t("Polygon (vertices at crosshair position)") }}
                            </div>
                        </q-item-section>
                    </q-item>
                </q-list>
            </q-menu>
        </q-btn>
        <q-btn v-if="drawVertexByVertex && !parent.drawVertexByVertex" dense flat icon="close"
            @click="addVertex(parent.map.getView().getCenter())" :label="$t('Add vertex')" no-caps />
        <q-btn v-if="drawVertexByVertex && polygonVertices.length > 3" dense flat icon="stop" color="positive"
            @click="endPolygon" :label="$t('End polygon')" no-caps />
        <q-btn v-if="drawVertexByVertex" dense flat color="negative" icon="delete" @click="cancelPolygon"
            :label="$t('Cancel')" no-caps />
        <q-btn v-if="parent.editStack.length > 0" dense flat icon="undo" @click="undo" :label="$t('Undo')" no-caps />
        <span v-if="!drawVertexByVertex" style="position: absolute; right: 0px; top: 0px">
        </span>
    </div>
</template>

<script>

/**
 * Toolbar for observations (My locations).
 * 
 * @component
 * @name ToolbarObservation
 * @example
 * <ToolbarObservation />
 */

import { Feature } from "ol";
import { Point, Polygon } from "ol/geom";
import { loadComponent } from '@/common/component-loader';

export default {
    name: "ToolbarObservation",
    components: {
        Autocomplete: loadComponent('autocomplete')
    },
    props: {
        parent: {},
    },
    watch: {
        "parent.params": function (val) {
            this.showEditableSource = true;
        },
    },
    data() {
        return {
            polygon: null,
            polygonVertices: [],
            drawVertexByVertex: false,
        };
    },
    methods: {

        /**
         * Undoes the last edit on map.
         */
        async undo() {
            let f = this.parent.editStack.pop();
            let orig_id = f.get("orig_id");
            let g = this.parent.editableSource.getFeatureById(orig_id);
            g.getGeometry().setCoordinates(f.getGeometry().getCoordinates());
            this.parent.saveFeature(g);
        },

        /**
         * Adds a point as a new observation location.
         *
         * @param {Object} point - The point to be added.
         */
        addPoint(point) {
            let f = new Feature({ geometry: point });
            f.set("editor", "simple-observation");
            this.parent.editableSource.addFeature(f);
            this.parent.map.getView().setCenter(point.getCoordinates());
            this.parent.selectedFeature = f;
            this.parent.editProps();
        },

        /**
         * Adds a new observation at the current GPS location.
         */
        addAtGPS() {
            // create point at geolocation position
            try {
                let point = new Point(this.parent.geolocation.getPosition());
                 this.addPoint(point);
            } catch (e) {
                this.showError(this.$t("Geolocation not available"));
            }
        },

        /**
         * Adds a new observation at the center of the map (at crosshair position).
         */
        addAtCenter() {
            // create point at center of map
            let point = new Point(this.parent.map.getView().getCenter());
            this.addPoint(point);
        },

        /**
         * Adds a new observation at the mouse click position.
         */
        addAtMouse() {
            this.parent.newObject({ type: "Point", "editor": "simple-observation" });
        },

        /**
         * Adds a polygon.
         */
        addPolygonMouse() {
            //this.parent.newObject({ type: "Polygon" });
            this.polygonVertices = [];
            this.drawVertexByVertex = true;
            this.parent.drawVertexByVertex = true;
        },

        /**
         * Creates a new polygon.
         */
        addPolygonCrosshair() {
            this.polygonVertices = [];
            this.drawVertexByVertex = true;
        },

        /**
         * Adds a point to the polygon
         */
        addVertex(coords) {
            let point = new Point(coords);
            //this.parent.editableSource.addFeature(new Feature({ geometry: point }));

            if (this.polygonVertices.length == 0) {
                this.polygonVertices.push(point.getCoordinates());
                this.polygonVertices.push(point.getCoordinates());
                let poly = new Polygon([this.polygonVertices]);

                this.polygon = new Feature({
                    geometry: poly,
                });
                this.polygon.setStyle(this.parent.selectedStyleFunction);

                this.parent.editableSource.addFeature(this.polygon);

            } else {
                this.polygonVertices.splice(this.polygonVertices.length - 1, 0, point.getCoordinates());
            }
            this.polygon.getGeometry().setCoordinates([this.polygonVertices]);
        },

        /**
         * Cancels the drawing of the polygon.
         */
        cancelPolygon() {
            this.drawVertexByVertex = false;
            this.parent.drawVertexByVertex = false;
            this.polygonVertices = [];
            this.parent.editableSource.removeFeature(this.polygon);
        },

        /**
         * Ends the drawing process.
         */
        endPolygon() {
            this.polygon.getGeometry().setCoordinates([this.polygonVertices]);
            this.polygon.set("geometry_type_id", 2);
            this.polygon.set("props", []);
            this.polygon.set("editor", "simple-observation");
            this.parent.selectedFeature = this.polygon;
            this.drawVertexByVertex = false;
            this.parent.drawVertexByVertex = false;
            this.parent.editProps();
        },

        /**
         * Deletes the current observation.
         */
        async del() {
            if (await this.confirmDialog(this.$t("Delete observation?"))) {
                this.delete("User/DeleteCustomGeometry/" + this.parent.params.id);
                this.editableSource.clear();
            }
        },
    },
    mounted() {
        window.addEventListener("vertexAdded", (e) => {
            this.addVertex(e.detail.coords);
        });
    },
    unmounted() {
        window.removeEventListener("vertexAdded", this.addVertex);
    }
}
</script>

<style scoped>
i {
    font-size: x-large;
    margin-right: 5px;
}
</style>
