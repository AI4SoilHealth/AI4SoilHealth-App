<template>
    <q-card class="max-width">
        <q-card-section v-if="featureProperties">
            <table>
                <tr v-for="o of featureProperties" :key="o.key">
                    <td v-html="o.key"></td>
                    <td v-html="o.value ?? ''"></td>
                </tr>
            </table>
        </q-card-section>
        <q-card-actions>
            <q-btn v-if="showHistoryVisible" icon="history" flat dense no-caps @click="parent.showHistory" :label="$t('History')">
                <q-tooltip>{{ $t("Show history for this feature") }}</q-tooltip>
            </q-btn>
            <q-btn v-if="showStatisticsVisible" icon="bar_chart" flat dense no-caps @click="parent.showFeatureStats(false)":label="$t('Statistic')">
                <q-tooltip>{{ $t("Show statistics for this feature") }}</q-tooltip>           
            </q-btn>
            <q-btn v-if="has_spectrum" flat dense no-caps icon="show_chart" @click="parent.showSpectrum" :label="$t('Spectrum')">
                <q-tooltip>{{ $t("Show spectral graph for this feature") }}</q-tooltip>  
            </q-btn>
            <q-btn v-if="showStatisticsVisible" icon="compare" flat dense @click="parent.showFeatureStats(true)" :label="$t('Compare')" no-caps>
                <q-tooltip>{{ $t("Compare statistics for this feature with intersecting geometries") }}</q-tooltip>    
            </q-btn>  
        </q-card-actions>
        <a v-if="options && options.api2Link" @click="$emit('children', feature)">{{ options.api2Link }}</a>
    </q-card>
</template>
<script>
/**
 * Popup component for displaying properties of a map feature.
 * 
 * @component
 * @name OlMapPopup
 * @example
 * <OlMapPopup />
 */

//import Overlay from "ol/Overlay"
//import { ClosePopup } from "quasar";

export default {
    name: "OlMapPopup",
    props: ["parentPopup"],
    data() {
        return {
            feature: null,
            featureProperties: [],
            parent: null,
            additionalProps: null,
            options: null,
            has_spectrum: false
        }
    },
    
    computed: {
        showHistoryVisible() {
            return this.parent && this.parent.selectedFeature.getGeometry().getType() == "Point" && this.parent.selectedTitle;
        },
        showStatisticsVisible() {
            return this.parent && this.parent.selectedFeature.getGeometry().getType() != "Point" && this.parent.selectedTitle;
        }
    },

    /**
     * Initializes the popup.
     */
    async mounted() {
        this.initializeComponent(this.parentPopup);
        this.feature = this.parent.selectedFeature;
        let skipProperty = ["color_code", "fill_color_code", "geometry", "geom", "geometry_type_id", "Name", "name", "key"];
        // workaround: feature.getProperties() does not work in v-for
        this.has_spectrum = this.feature.get("has_spectrum");
        let p = this.feature.getProperties();
        // for (let key in this.feature.getProperties()) {
            Object.keys(p).sort().forEach(key => {
                // if key is not in skipProperty
                if (!skipProperty.find(x => x == key)) {
                    let value = this.feature.get(key);
                    if (value instanceof Array) {
                        for (let v of value) {
                            if (v.name && v.name.toLowerCase() == "name") {
                                this.title = v.value;
                            } else if (v.name != null) {
                                let prop = this.$store.catalogs.properties.find(x => x.id == v.property_id);
                                if (prop == null || !prop.indicator_id || prop.numerical) {
                                    this.featureProperties.push({ key: v.name, value: this.formatValue(v.value) });
                                } else {
//                                  this.featureProperties.push({ key: v.name, value: this.$store.catalogs.descriptions.find(x => x.indicator_id == prop.indicator_id && x.value == v.value).label });
                                    this.featureProperties.push({ key: v.name, value: this.$store.catalogs.descriptions[prop.indicator_id][v.value].label });
                                }
                            }
                        }
                    } else if (value != null) {
                        this.featureProperties.push({ key: key, value: this.formatValue(value) });
                    }
                }
            });
        if (this.additionalProps) {
            for (let key in additionalProps) {
                this.featureProperties.push({ key: key, value: additionalProps[key] });
            }
        }
    },
    methods: {
        /**
         * Hides the popup.
         */
        hide() {
            this.closePopup();
        },
    }
}
</script>