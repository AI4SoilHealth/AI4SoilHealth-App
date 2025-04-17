<template>
    <table-row-editor v-if="loaded" :parent="this" :multiRow="false" :rows="rows" @cancel="cancel"/>
</template>
<script>

/**
 * Generic single row editor component
 * 
 * @component
 * @name SingleRowEditor
 * @example
 * <SingleRowEditor :parent="this" />
 */
import { loadComponent } from '@/common/component-loader';
import { TableUtilsMixin } from '../mixins/table-utils';

export default {

    name: "SingleRowEditor",
    mixins: [TableUtilsMixin],
    components: {
        tableRowEditor : loadComponent('table-row-editor'),
    },

    props: ['parentPopup'],
    data() {
        return {
            editingRow: {},
            editingRowSaved: {},
            rows: [],
            columns: [],
            loaded: false,
        };
    },

    created() {
    },

    /**
     * Initialize the component
     */
    async mounted() {
        this.initializeComponent(this.parentPopup);

        await this.injectTableAPIProps(this.tableAPI);

        this.tableAPIKey = this.id;
        this.singleRow = true;
        await this.reload();
        if (this.rows.length > 0) {
            this.editingRow = this.rowToObject(this.rows[0], this.columns);
        } else {
            this.editingRow = this.createEmptyRow(this.columns);
        }
        this.copyObject(this.editingRow, this.editingRowSaved);
        await this.loadLookups(this.columns);
        this.loaded = true;  
    },

    methods: {  

        /**
         * Save the current row 
         * @returns {Promise<void>}
         */

        async saveRow() {
            let ret = await this.put("Table/" + this.tableAPI, this.editingRow);
            if (ret != null) {

                if (this.id == null && ret != null) {
                    this.id == ret.id;
                }

                if (this.specificEditorSave) {
                    this.specificEditorSave();
                    return;
                }

                this.cancel();
            }
        },

        /**
         * Cancel the edit operation and revert to the original values
         */
        cancel() {
            if (this.id == null && this.parent && this.parent.editableSource) {
                this.parent.editableSource.removeFeature(this.parent.selectedFeature);
                this.parent.selectedFeature = null;
            }
            this.closePopup();
            this.$store.formChanged = false;
        },
    }
};
</script>
