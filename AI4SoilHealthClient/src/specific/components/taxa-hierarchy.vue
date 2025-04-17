<template>
    <Header title="Taxa browser"/>
    <q-tree dense ref="tree" :nodes="tree" node-key="id" no-connectors @lazy-load="lazyLoad" default-expand-all>
        <template v-slot:default-header="props">
            <div>{{ props.node.latin_name }} ({{props.node.level}} - {{ props.node.n_children }} {{ props.node.n_children == 1 ? "child" : "children" }})</div>
        </template>
    </q-tree>
</template>
<script>
/**
 * Taxa hierarchy component
 * @component components/taxa-hierarchy
 * @description Taxa hierarchy component
 * @example
 *  <TaxaHierarchy />
 */
export default {
    name: "TaxaHierarchy",
    data() {
        return {
            tree: [],
        };
    },
    async mounted() {
        let ret = await this.get("Table/GetTable", { dbFunction: "data.get_taxa_children", json: true, pars: '{"id" : 0}' });
        this.setLazy(ret);
        this.tree = ret;
    },

    methods: {

        setLazy(nodes) {
            for (let i = 0; i < nodes.length; i++) {
                if (nodes[i].n_children > 0) {
                    nodes[i].children = [];
                    nodes[i].lazy = true;
                }
            }
        },

        async lazyLoad({ node, key, done, fail }) {
            console.log("lazyLoad", node, key);
            let children = await this.get("Table/GetTable", { dbFunction: "data.get_taxa_children", json: true, pars: '{"id" : ' + key + '}' });
            console.log("lazyLoad", children);
            this.setLazy(children);
            done(children);
        },
    },
};
</script>
