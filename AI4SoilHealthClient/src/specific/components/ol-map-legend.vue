<template>
    <div id="legend" ref="legend" :style="{ left: !rightLegend ? '10px' : null, right: rightLegend ? '10px' : null }">

        <div v-if="selectedTitle">
            <div style="font-size:12px">
                <div v-html="selectedTitle.label"/>
                <div v-if="selectedCatalog.confidence || selectedCatalog.depth" v-html="(selectedCatalog.confidence??'') + ' ' + (selectedCatalog.depth??'')"/>
                <div class="row">
                    <div v-html="selectedCatalog.label"/>
                    <q-space />
                    <q-checkbox v-if="selectedTitle.description" v-model="selectedTitle.allActive" @click="setActive" dense size="sm" />
                </div>
            </div>  
            <div v-for="option in optionsTiff" :key="option.text" class="row items-center">
                <span class="colorBox" :style="{ backgroundColor: option.color_code }"></span>
                <span style="font-size:11px">{{ option.text }}</span>
                <q-space />
                <q-checkbox v-if="selectedTitle.description" v-model="option.active" @click="updateColorMap" dense size="sm" />
            </div>
        </div>

        <hr v-if="selectedTitle && parent.selectedIndicator"/>

        <div v-if="parent.selectedIndicator && !rightLegend">
            <div class="row">
                <div v-html="parent.selectedIndicator.label" style="font-size:12px"></div>&nbsp;
                <div v-if="parent.selectedIndicator.unit" v-html="'['+parent.selectedIndicator.unit+']'" style="font-size:12px"></div>&nbsp;
                <div v-html="parent.selectedIndicator.depth.name" style="font-size:12px"></div>
            </div>
            <div v-for="(ds, index) of parent.selectedDataSource" :key="ds" class="row items-center" style="font-size:12px">
                <q-icon :name="icons[index % 3]" size="xs"></q-icon><span>&nbsp;{{ ds.label }}</span>
            </div>  
            <div v-for="option in options" :key="option.text" class="row items-center">
                <span class="colorBox" :style="{ backgroundColor: option.color_code }"></span>
                <span style="font-size:11px">{{ option.text }}</span>
            </div>
            <div style="font-size:11px">Points: {{ parent.pointFeatures?.length }}</div>
        </div>

    </div>
</template>

<script lang="js">
/**
 * Legend component for the map.
 * 
 * @component
 * @name OlMapLegend
 * @example
 * <OlMapLegend />
 */

export default {
    name: "OlMapLegend",
    props: {
        optionsTiff: Array,
        options: Array,
        valueAtPixel: String,
        noData: Number,
        parent: Object, 
        rightLegend: Boolean,
        selectedTitle: Object,
        selectedCatalog: Object
    },
    data() {
        return {
            valueAtPixelStyle: null,
            icons: [this.$icons.circle, "change_history", this.$icons.square]
        }
    },
    watch: {
        options: {
            handler(val) {
                this.unit = this.parent.selectedIndicator ? this.parent.selectedIndicator.unit : null;
                this.count = this.parent.pointFeatures ? this.parent.pointFeatures.length : null;
            },
            deep : true
        }
        /**
         * Retrieves the value at the specified pixel coordinates and shows in near the appropriate legend item.
         *
         * @param {number} val - The pixel value to retrieve.
         * @returns {any} The value at the specified pixel coordinates.
         */
        // valueAtPixel: function (val) {
        //     let v = +val;
        //     for (let i = 0; i < this.options.length; i++) {
        //         if (this.options[i].from <= v && v <= this.options[i].to) {
        //             // calculate legend position, height and width
        //             this.valueAtPixelStyle = {
        //                 position: "absolute",
        //                 bottom: (10 - 4 + (this.options.length - i - 1) * (this.$refs.legend.clientHeight / this.options.length)) + "px",
        //                 left: (10 + 8 + this.$refs.legend.clientWidth) + "px",
        //                 backgroundColor: "white",
        //                 padding: "3px",
        //                 borderRadius: "5px",
        //             }
        //             break;
        //         }
        //     }
        // },
    },
    methods: {
        /**
         * Updates the color map of the selected layer.
         */
        updateColorMap() {
            this.parent.updateColorMap(this.selectedTitle, this.optionsTiff);
        },

        setActive() {
            this.optionsTiff.forEach(option => option.active = this.selectedTitle.allActive);
            this.parent.updateColorMap(this.selectedTitle, this.optionsTiff);
        }


    }
}
</script>

<style scoped>
#legend {
    position: absolute;
    bottom: 20px;
    padding: 3px 3px 3px 3px;
    background-color: rgba(255,255,255, 0.3);
    border: 1px solid #ccc;
    border-radius: 5px;
    max-height: 600px;
    overflow: hidden;
    overflow-y: scroll;    
}

#legend::-webkit-scrollbar {
    display: none; /* Hide the scrollbar for Chrome, Safari, and Opera */
}

.colorBox {
    width: 15px;
    height: 15px;
    display: inline-block;
    margin-right: 10px;
}
</style>