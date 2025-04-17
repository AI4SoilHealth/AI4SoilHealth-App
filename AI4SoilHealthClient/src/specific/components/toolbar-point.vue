<template>
    <div style="width:100%; height:40px" class="row">
        <q-btn dense flat no-caps @click="addAtGPS" :label="$t('Add point at ')">
            <q-tooltip>Add point at current GPS position</q-tooltip>
            <q-icon  class="q-ml-xs" name="my_location" />
        </q-btn>
        <q-btn dense flat no-caps @click="addAtCenter" :label="$t('Add point at ')" >
            <q-tooltip>Add point at croshair position</q-tooltip>
            <q-icon class="q-ml-xs" name="close" />
        </q-btn>
        <q-btn dense flat no-caps @click="addAtMouse" :label="$t('Add point at ')" >
            <q-tooltip>Add point at mouse cursor position</q-tooltip>
            <q-icon class="q-ml-xs" :name="$icons.arrow_selector_tool" />
        </q-btn>
    </div>
</template>

<script>

/**
 * Toolbar for adding points.
 * 
 * @component
 * @name ToolbarPoint
 * @example
 * <ToolbarPoint />
 */

import { Feature } from "ol";
import { Point, Polygon } from "ol/geom";
import { loadComponent } from '@/common/component-loader';

export default {
    name: "ToolbarPoint",
    props: {
        parent: {},
    },
    data() {
        return {
        };
    },
    methods: {

        async save() {
            //this.parent.saveFeature(this.parent.selectedFeature);
            this.parent.editProps({ tableAPI: "data_point" });
        },

        /**
         * Adds a point as a new observation location.
         *
         * @param {Object} point - The point to be added.
         */
        addPoint(point) {
            let f = new Feature({ geometry: point });
            f.set("editor", "single-row-editor");
            f.set("tableAPI", "data_point");
            this.parent.editableSource.addFeature(f);
            this.parent.map.getView().setCenter(point.getCoordinates());
            this.parent.selectedFeature = f;
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
            // is automatically saved at event
            this.parent.newObject({ type: "Point", editor: "single-row-editor", tableAPI: "data_point" });
        },
    },
    mounted() {
    },
    unmounted() {
    }
}
</script>

<style scoped>
i {
    font-size: x-large;
    margin-right: 5px;
}
</style>
