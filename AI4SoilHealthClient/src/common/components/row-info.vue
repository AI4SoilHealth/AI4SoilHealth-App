<template>
    <div>
    <q-btn @click="save" dense flat icon="save" />
    <table class="q-ma-md">
        <thead>
            <tr>
                <th scope="col"></th>
                <th scope="col">Field</th>
                <th scope="col">Value</th>
                <th scope="col">Order No</th>
                <th scope="col">Label</th>
                <th scope="col">Default</th>
                <th scope="col">Rules</th>
            </tr>
        </thead>
            <draggable :list="columns" item-key="name" tag="tbody" ghost-class="ghost" handle=".draggable">	
                <template #item="{ element: col, index }" key="name"> 
                    <tr>
                        <td scope="row"><reorder-handles :array="columns" :index="index" :hasDelete="false"/></td>
                        <td>{{ col.name }}</td>
                        <td>{{ v(col.name) }}</td>
                        <td><q-input dense v-model="col.order_no"/></td>
                        <td><q-input dense v-model="col.label"/></td>
                        <td><q-input dense v-model="col.default"/></td>
                        <td><q-input dense v-model="col.customRules"/></td>
                    </tr>
                </template> 
            </draggable>          
    </table>
    </div>
</template>

<script>
/**
 * Row info component
 * 
 * @component
 * @name RowInfo
 * @example
 * <RowInfo />
 */
import draggable from 'vuedraggable';
import { loadComponent } from '@/common/component-loader';
export default {
    name: "RowInfo",
    props: ['parentPopup'],
    components: {
        draggable,
        reorderHandles: loadComponent('reorder-handles')
    },
    data: function () {
        return {
            columns: [],
        }
    },
    mounted() {
        this.initializeComponent(this.parentPopup);
        for (let i = 0; i < this.columns.count; i++) {
            this.columns[i].order_no = i;
        }
    },
    methods: {
        v(colName) {
            if (!this.editingRow) return '';
            if (typeof this.editingRow[colName] == 'object') {
                return JSON.stringify(this.editingRow[colName]);
            } else {
                return this.editingRow[colName];
            }
        },
        async save() {
            let colAtts = {};
            let i = 0;
            for (let col of this.columns) {        
                let o = {}; 
                if (col.default) o.default = col.default;
                if (col.required) o.required = col.required;
                if (col.rules) o.rules = col.rules;
                if (col.customRules) o.customRules = col.customRules;
                o.order_no = ++i;
                o.label = col.label;
                colAtts[col.name] = o;
            }
            await this.put("Dev/SetTableAPIProps/" + this.tableAPIPropsKey, { colAtts: colAtts });
            this.$store.tableAPIProps[this.tableAPIPropsKey] = null;
            await this.parent.reload();
        }
    }
}
</script>

<style scoped>
th,
td {
    text-align: left;
}

th {
    background-color: #f2f2f2;
}
</style>