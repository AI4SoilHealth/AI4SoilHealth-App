<template>
    <autocomplete
        v-model="multiSelectManager.models[optionKey]"
        :bg-color="bgColor"
        :clearable="clearable"
        :dense="dense"
        :disable="disable"
        :filled="filled"
        :label="label"
        :options="options"
        :options-dense="optionsDense"
        :outlined="outlined"
        :searchable="searchable"
        option-label="value"
        @update:model-value="updateValue"
    />
</template>

<script>

import { loadComponent } from '@/common/component-loader';

// const DEBUG = false;
// const DEBUG = true;

export default {
    name: 'MultiSelectElement',
    components: {
        Autocomplete: loadComponent("autocomplete"),
    },
    inject: ['multiSelectManager'],
    props: {
        // Autocomplete props
        disable: { type: Boolean, default: false },
        searchable: { type: Boolean, default: true },
        clearable: { type: Boolean, default: true },
        // emitValue: { type: Boolean, default: false },
        // mapOptions: { type: Boolean, default: false },
        optionsDense: { type: Boolean, default: true },
        outlined: { type: Boolean, default: true },
        // optionLabel: { type: String, default: "label" },
        // optionValue: { type: String, default: "value" },
        filled: { type: Boolean, default: true },
        dense: { type: Boolean, default: true },
        label: { type: String, default: undefined },
        bgColor: { type: String, default: 'white' },
        
        // MultiSelectElement props
        optionKey: { type: String, required: true },
        modelValue: {
            type: [Boolean, String, Number, Array, Object],
            default: null,
        },
    },
    emits: ['update:model-value'],
    computed: {
        options() {
            return this.multiSelectManager.getOptions(this.optionKey)
        },
    },
    mounted() {
        this.multiSelectManager.setModel(this.optionKey, null);
        console.log('MultiSelectElement mounted');
    },
    expose: [],
    methods: {
        updateValue(value) {
            this.multiSelectManager.updateOptionValue(this.optionKey);
            this.$emit('update:model-value', value);
        },
    },
};
</script>

<style scoped>
</style>
