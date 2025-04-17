<template>
    <div>    <div v-for="p in procedures">
        <q-btn dense flat icon="save" @click="save(p)"/>
        <q-btn class="copy" dense flat size="sm" icon="content_copy" @click="copyToClipboard(p.function_definition)" />
        <q-input type="textarea" v-model="p.function_definition" :rows="20" style="font-family: Consolas"/>
    </div>
    </div>
</template>

<script>
export default {
    name: "Crud",
    props: ["parentPopup"],
    data() {
        return {
            appendix : ['c', 'r', 'u', 'd'],
            procedures: [],
        };
    },
    async mounted() {
        this.initializeComponent(this.parentPopup);
        if (this.refTable) {
            this.procedures = await this.get("Dev/GetDbProcedures", { schemaName: "zzgll", procName: this.refTable + "_l" });
        } else {
            let routeProps = JSON.parse(this.routeProps);
            if (routeProps.tableAPI) {
                for (let a of this.appendix) {
                    let p = await this.get("Dev/GetDbProcedures", { schemaName: "zzglc", procName: routeProps.tableAPI + "_" + a });
                    this.procedures = this.procedures.concat(p);
                }
            } else if (routeProps.dbFunction) {
                let p = await this.get("Dev/GetDbProcedures", { schemaName: "zzgll", procName: routeProps.dbFunction });
                this.procedures = this.procedures.concat(p);
            }
        }
    },
    methods: {
        async save(p) {
           await this.put("Dev/UpdateDbProcedure", { definition: p.function_definition });
        }
    }
};
</script>