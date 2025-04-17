<template>
    <q-btn size="sm" dense flat icon="drag_handle" no-caps class="draggable">
        <q-tooltip>{{ $t("Reorder by dragging") }}</q-tooltip>
    </q-btn>
    <q-btn size="sm" :disable="index == 0" dense flat icon="arrow_upward" no-caps @click="moveItemUp(index)">
        <q-tooltip>{{ $t("Move up") }}</q-tooltip>
    </q-btn>
    <q-btn size="sm" :disable="index == array.length - 1" dense flat icon="arrow_downward" no-caps
        @click="moveItemDown(index)">
        <q-tooltip>{{ $t("Move down") }}</q-tooltip>
    </q-btn>
    <q-btn v-if="hasDelete" size="sm" dense flat icon="delete" no-caps @click="removeItem(index)" color="negative">
        <q-tooltip>{{ $t("Delete") }}</q-tooltip>
    </q-btn>
</template>
<script>
export default {
    name: 'ReorderHandles',
    props: {
        array: { type: Array, default: [] },
        index: { type: Number, default: 0 },
        hasDelete: { type: Boolean, default: true }
    },
    methods: {
        moveItemUp(index) {
            if (index > 0) {
                const item = this.array.splice(index, 1)[0];
                this.array.splice(index - 1, 0, item);
            }
        },
        moveItemDown(index) {
            if (index < this.array.length - 1) {
                const item = this.array.splice(index, 1)[0];
                this.array.splice(index + 1, 0, item);
            }
        },
        removeItem(index) {
            this.array.splice(index, 1);
        },
    }
}
</script>