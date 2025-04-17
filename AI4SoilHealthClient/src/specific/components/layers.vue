<template>
    <q-card v-if="loaded">
        <q-card-section>
            <div class="row">
                <autocomplete class="q-pb-sm q-pr-sm baseLayerDropdown" :options="parent.baseLayers" v-model="parent.selectedBaseLayer" :label="$t('Base layer')"
                style="width:200px;" :searchable="false" :clearable="false" />   
            
                <q-select class="q-ma-none srids" :options="$store.catalogs.srids" v-model="parent.srid" :label="$t('Projection')" dense optionsDense />
            </div>
            
            <datacube-selector class="q-pb-sm" :parent="parent" :alt="false"/> 
            <datacube-selector v-if="parent.selectedTitle" class="q-pb-sm" :parent="parent" :alt="true"/> 

            <autocomplete class="q-pb-sm" :options="dataSources" v-model="parent.selectedDataSource" :label="$t('Data source')"
            @update:model-value="selectedDataSourceUpdated" :clearable="false" style="width:400px;" :multiple="true" use-chips/>
            
            <autocomplete class="q-pb-sm" v-if="sites.length > 0" :options="sites" :searchable="false" v-model="parent.selectedSite"
            :label="$t('Site')" style="width:400px;" />

            <div class="row">
                <autocomplete class="q-pb-sm"  v-if="parent.selectedDataSource?.length > 0" :options="indicators" v-model="parent.selectedIndicator" @update:model-value="parent.selectedIndicator.depth=parent.selectedIndicator.depths[0]" :label="$t('Indicator')" />
                <autocomplete class="q-pb-sm" v-if="parent.selectedIndicator" :options="parent.selectedIndicator.depths" v-model="parent.selectedIndicator.depth" :label="$t('Depth')"/>
            </div>

            <div  v-if="parent.selectedIndicator" class="row">
                <q-input v-model="parent.valueFrom" :label="$t('From')" style="width:60px;" dense />
                <q-input v-model="parent.valueTo" :label="$t('To')" style="width:60px;" dense />
                <q-btn flat no-caps :label="$t('Show')" icon="visibility" @click="parent.getIndicatorValues" dense />
                <q-btn flat no-caps :label="$t('Frequencies')" icon="bar_chart" @click="parent.showFrequencies" dense/>
                <q-checkbox class="q-pa-none q-ml-sm" v-model="parent.showPoints" :label="$t('Points')" left-label dense />
            </div>

            <autocomplete class="q-pb-sm" :options="nutsLevels" v-model="parent.nutsLevel" :label="$t('NUTS level')" :searchable="false" />

            <div class="row">
                <q-checkbox v-model="parent.trackPosition" :label="$t('Track position')" left-label dense class="q-mr-md" />
                <q-checkbox v-model="parent.showEditableSource" :label="$t('Custom geometries')" left-label dense class="q-mr-md" />    
                <q-checkbox v-model="parent.info" :label="$t('Info')" left-label dense class="q-mr-md" />
            </div>
            
        </q-card-section>
    </q-card>
</template>

<script>
import { loadComponent } from '@/common/component-loader';
import { ref } from 'vue';
export default {
    name: 'layers',
    props: ['parentPopup', 'layersProps'],
    components: {
        DatacubeSelector: loadComponent('datacube-selector'),
        Autocomplete: loadComponent('autocomplete'),
    },
    data() {
        return {
            loaded: false,
            parent: null,
            dataSources: [],
            indicators: [],
            max: 0,
            metadata: null,
            nutsLevels: [
                { value: 0, label: "Countries" },
                { value: 1, label: "Major regions" },
                { value: 2, label: "Basic regions" },
                { value: 3, label: "Small regions" },
            ],
            sites: []
        };
    },
    mounted() {
        this.initializeComponent(this.parentPopup);
        this.indicators = this.$store.catalogs.indicators;
        this.dataSources = this.$store.catalogs.dataSources;
        this.selectedDataSourceUpdated();
        this.loaded = true;
    },
    methods: {
        async selectedDataSourceUpdated() {
            this.parent.selectedSite = null;
            if (this.parent.selectedDataSource.length == 0) {   
                this.sites = [];
                this.indicators = [];
                this.parent.selectedDataSourceIds = [];
            } else {
                this.parent.selectedDataSourceIds = this.parent.selectedDataSource.map(x => x.value);
                let sites = await this.get("User/GetSitesForDataSource", { dataSourceId: this.parent.selectedDataSourceIds.join(",") } );
                if (sites == "") {
                    this.sites = [];
                } else {
                    this.sites = sites;
                }
                let indicators = await this.get("User/GetIndicatorsForDataSource", { dataSourceId: this.parent.selectedDataSourceIds.join(",") } );
                if (indicators == "") {
                    this.indicators = [];
                } else {
                    this.indicators = indicators;
                }
            };
        }
    }
}
</script>