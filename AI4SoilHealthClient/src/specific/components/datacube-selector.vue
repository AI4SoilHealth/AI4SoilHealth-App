<template>
    <div class="row">
        <autocomplete class="q-pr-sm" :options="sourceTitles" v-model="parent[selectedTitle]" @update:model-value="selectedTitleUpdated(true)" style="min-width: 200px;"
            :label="titleLabel" />
        <autocomplete class="q-pr-sm" v-if="parent[selectedTitle] && depths.length > 0" :options="depths" v-model="parent[selectedDepth]" :clearable="false"
            :searchable="false" @update:model-value="selectedDepthUpdated" :label="$t('Depth')" />
        <autocomplete class="q-pr-sm" v-if="parent[selectedTitle] && confidences.length > 0" :options="confidences" v-model="parent[selectedConfidence]" :clearable="false"
            :searchable="false" @update:model-value="selectedDepthUpdated" :label="$t('Confidence')" />
        <q-btn size="sm" padding="xs" flat v-if="parent[selectedCatalog] && parent[selectedCatalog].value < this.max"
            icon="arrow_back" @click="back"/>
        <autocomplete class="q-pr-sm" v-if="parent[selectedTitle]" :label="$t('Time span')" :options="sources" v-model="parent[selectedCatalog]" :searchable="false" :clearable="false" />
        <q-btn size="sm" padding="xs" flat v-if="parent[selectedCatalog] && parent[selectedCatalog].value > 1"
            icon="arrow_forward" @click="forward" />
    </div>
</template>

<script lang='js'>
/**
 * Toolbar for various advance operation on the map.
 * 
 * @component
 * @name ToolbarDatacube
 * @example
 * <ToolbarDatacube />
 */

import { loadComponent } from '@/common/component-loader';

export default {
    name: "ToolbarDatacube",
    components: {
        Autocomplete: loadComponent('autocomplete'),
    },
    props: ['parent', 'alt'],
    data() {
        return {
            confidences: [],
            depths: [],
            sourceTitles: [],
            max: 0,
            selectedTitle: null,
            selectedCatalog: null,
            selectedDepth: null,
            selectedConfidence: null,
            titleLabel: null,
            sources: []
        }
    },

    /**
     * Lifecycle hook: Called after the component has been created.
     * Sets the options to the data.
     * Sets the dataSources to the store's dataSources.
     */
    async mounted() {
        this.sourceTitles = this.$store.catalogs.sourceTitles;
        if (this.alt) {
            this.selectedTitle = "altSelectedTitle";
            this.selectedCatalog = "altSelectedCatalog";
            this.selectedDepth = "altSelectedDepth";
            this.selectedConfidence = "altSelectedConfidence";
            this.titleLabel = this.$t("Other datacube layer");
        } else {
            this.selectedTitle = "selectedTitle";
            this.selectedCatalog = "selectedCatalog";
            this.selectedDepth = "selectedDepth";
            this.selectedConfidence = "selectedConfidence";
            this.titleLabel = this.$t("Datacube layer");
        }

        if (this.parent[this.selectedTitle]) {
            this.selectedTitleUpdated(false);
        }       
    },

    methods: {

        /**
         * Moves to the previous catalog.
         */
        back() {
            this.parent[this.selectedCatalog] = this.sources[this.parent[this.selectedCatalog].value];
        },

        /**
         * Moves to the next catalog.
         */
        forward() {
            this.parent[this.selectedCatalog] = this.sources[this.parent[this.selectedCatalog].value - 2];
        },

        /**
         * Updates the selected title.
         */
        async selectedTitleUpdated(init) {
            if (!this.parent[this.selectedTitle]) {
                this.depths = [];
                this.confidences = [];
                this.parent[this.selectedCatalog]= null;
                this.parent[this.selectedDepth] = null;
                this.parent[this.selectedConfidence] = null;   
            } else {
                console.log("selectedTitleUpdated", this.parent[this.selectedTitle]);
                this.parent[this.selectedTitle].description = this.$store.catalogs.descriptions[this.parent[this.selectedTitle].indicator_id];
                console.log("selectedTitleUpdated", this.parent[this.selectedTitle].description);
                if (this.parent[this.selectedTitle].depth_list == null) {
                    this.depths = [];
                    this.parent[this.selectedDepth] = null;
                } else {
                    this.depths = this.parent[this.selectedTitle].depth_list.split(",").map(x => { return { value: x.trim(), label: x.trim() } });
                    if (this.depths?.length > 0) {
                        if (this.parent[this.selectedDepth] == null || init) {
                            this.parent[this.selectedDepth] = this.depths[0];
                        }
                    } else {
                        this.parent[this.selectedDepth] = null;
                    }   
                }
                
                if (this.parent[this.selectedTitle].confidence_list) {
                    this.confidences = this.parent[this.selectedTitle].confidence_list.split(",").map(x => { return { value: x.trim(), label: x.trim() } });
                } else {
                    this.confidences = [];
                }

                if (this.confidences.length > 0) {
                    if (this.parent[this.selectedConfidence] == null || init) {
                        this.parent[this.selectedConfidence] = this.confidences[0];
                    }
                } else {
                    this.parent[this.selectedConfidence] = null;
                }
                
            }
            this.selectedDepthUpdated(init);
        },

        /**
         * Updates the selected depth.
         */
        async selectedDepthUpdated(init) {
            if (this.parent[this.selectedTitle]== null) return;
            this.sources = await this.get("DataCube/GetShdcFiles", {
                titleId: this.parent[this.selectedTitle].value,
                depth: this.parent[this.selectedDepth] ? this.parent[this.selectedDepth].value : null,
                confidence:  this.parent[this.selectedConfidence] ? this.parent[this.selectedConfidence].value : null
            });
            this.max = this.sources.length;
            if (init) {
                this.parent[this.selectedCatalog] = this.sources[0];
            }
            this.parent[this.selectedTitle].sources = this.sources;
        },

    }

}

</script>
<style scoped>
.q-slider {
    width: 150px;
    margin-left: 20px;
    margin-top: 10px;
}
.semi-transparent {
    opacity: 0.5;
}
.tb {
    background-color: transparent;
}
</style>