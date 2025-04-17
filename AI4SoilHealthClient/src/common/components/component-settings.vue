<template>    
<span>
    <q-btn dense flat icon="settings" @click="getSettings" v-if="isAdmin">
        <q-tooltip>{{ $t("Edit settings") }}</q-tooltip>
    </q-btn>
    <q-dialog v-if="inEdit" :model-value="true">
            <table-row-editor v-if="inEdit" :parent="this" @save="saveRow" @cancel="inEdit = false" :actions="actions" />
    </q-dialog>
</span>
</template>     

<script>
/**
 * Button to invoke editing of a path or components settins.
 * 
 * @component
 * @name ComponentSettings
 * @example
 * <ComponentSettings :name="name" />
 */
import { loadComponent } from '@/common/component-loader';
export default {
    name: "ComponentSettings",
    components: {
        TableRowEditor: loadComponent('table-row-editor'),
    },
    props: {
        path: { type: String, default: null },
    },
    data() {
        return {
            inEdit: false,
            editingRow: {},
            editingRowSaved: {},
            editingRowIndex: 0,
            lookups: {},
            columns: [],
            actions: [{ label: "CRUD", icon:"code", action: this.crud }],
        };
    },
    methods: {
        crud() {
            this.initPopup({
                routeProps: this.editingRow.props, 
                component: "crud",
                maximized: true,
            });
        },  
        cancel() {
            this.inEdit = false;
        },  
        save () {
            this.saveRow();
        },

        /**
         * Saves the changes made to route parameters. Admins only.
         * 
         * @returns {Promise<void>} A promise that resolves when the changes are saved.
         */
        async saveRow() {
            
            this.inEdit = false;

            let saveRow = {};
            this.copyObject(this.editingRow, saveRow);

            this.prepareRow(saveRow, this.columns);

            let ret = await this.put("Table/meta_route", saveRow);
            if (ret != null) {
                this.$store.formChanged = false;
                await this.getRoutes();
                let route = this.$store.routes.find((r) => r.path === this.path);
                if (route.order_no > 0) {
                    this.activateRoute(route);
                }
            }
        },

        /**
         * Retrieves the route parameters and initializes the editingRow object. Admins only.
         */
        async getSettings() {
            this.inEdit = false;
            await this.$nextTick();
            console.log("Getting settings for " + this.path);
            let ret = await this.get("Dev/GetSettings?path=" + this.path);
            this.editingRow = ret[0];
            this.editingRow.props = JSON.parse(this.editingRow.props);
            this.columns = Object.keys(this.editingRow).map((k) => {
                return { name: k, label: k, type: this.getValueType(this.editingRow[k]), required: true };
            });
            this.editingRow.props = JSON.stringify(this.editingRow.props);
            this.inEdit = true;
        },

    }
}
</script>