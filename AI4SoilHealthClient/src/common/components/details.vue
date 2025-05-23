<template>
    <Header v-if="!parentPopup" :title="title" :backButton="!parentPopup && $store.level > 1" />
    <q-card class="max-width" v-if="localOptions">
        <q-card-section v-if="details && details.length > 1" class="row items-center q-py-xs">
            <q-btn dense flat no-caps
                :class="{ active: detail.name != localOptions.name, bold: detail.name == localOptions.name }"
                v-for="detail in details" :label="detail.name" :key="detail.name" @click="openDetail(detail, true)" />
            <q-space />
        </q-card-section>
        <q-card-section class="q-pa-none">
            <Table ref="detailTable" :detailTable="true" :options="localOptions" @close="close"/>
        </q-card-section>
    </q-card>
</template>

<script>

/**
 * Component to display detail of master-detail relationship
 * 
 * @component
 * @name TableDetails
 * @example
 * <TableDetails />
 */

import { loadComponent } from '@/common/component-loader';
import { TableUtilsMixin } from "@/common/mixins/table-utils.js"
import { TableMixin } from "@/common/mixins/table.js"

export default {
    name: 'TableDetails',
    mixins: [TableUtilsMixin, TableMixin],
    components: {
        Table: loadComponent('table'),
    },
    props: ['parentPopup'],

    data: () => {
        return {
            localOptions: null,
            details: null,
            title: 'Details',
            asForm: false,
            loaded: false
        }
    },
    /**
     * Mounted lifecycle method
     */
    async mounted() {
        this.initializeComponent(this.parentPopup);
        this.openDetail(this.details[0], false); // open the first detail by default
    },
    methods: {
        /**
         * Opens the detail.
         * 
         * @param {Object} detail - The detail to be opened.
         */
        async openDetail(detail, isLoaded) {
            this.loaded = isLoaded;
            detail.masterKey = this.masterKey;
            detail.masterValue = this.masterValue;
            this.localOptions = detail;
            this.title = this.localOptions.title;
            if (this.parentPopup) {
                this.parentPopup.title = this.localOptions.title;
                if (this.localOptions.help) this.parentPopup.help = this.localOptions.help;
                if (this.localOptions.titleToShow) this.parentPopup.titleToShow = this.localOptions.titleToShow;
            }   
            if (this.loaded) {
                await this.$nextTick();
                await this.$refs.detailTable.init();
            }
        },

        close() {
            this.closePopup();
        }
    }
}
</script>
<style scoped>
.active {
    text-decoration: underline;
}

.bold {
    font-weight: bold;
}
</style>