<template>
    <div>
        <slot />
        <div
            v-if="clearable"
            :class="{
                [clearButtonContainerClass]: clearButtonContainerClass !== '',
            }"
        >
            <q-btn
                :class="{
                    [clearButtonClass]: clearButtonClass !== '',
                }"
                :dense="clearButtonDense"
                :flat="clearButtonFlat"
                icon="restart_alt"
                @click="clearAllSelections"
            >
                <q-tooltip>
                    {{ $t('Clear all selections') }}
                </q-tooltip>
            </q-btn>
        </div>
    </div>
</template>

<script>
// const DEBUG = false;
// const DEBUG = true;

export default {
    name: 'MultiSelectContainer',
    provide() {
        return {
            multiSelectManager: this.multiSelectManager,
        };
    },
    props: {
        options: {
            type: Array,
            default: () => [],
            validator: (value) =>
                value.every((option) => typeof option === 'object'),
        },
        optionValue: {
            type: String,
            default: null,
        },
        // differentKeys: {
        //     type: Boolean,
        //     default: false,
        // },
        resetDependencies: {
            type: Array,
            default: () => [],
            validator: (value) =>
                value.every((item) => typeof item === 'string') ||
                value.every(
                    (item) =>
                        Array.isArray(item) ||
                        item.every((subItem) => typeof subItem === 'string')
                ),
        },
        clearable: {
            type: Boolean,
            default: false,
        },
        clearButtonContainerClass: {
            type: String,
            default: '',
        },
        clearButtonClass: {
            type: String,
            default: '',
        },
        clearButtonDense: {
            type: Boolean,
            default: true,
        },
        clearButtonFlat: {
            type: Boolean,
            default: true,
        },
        modelValue: {
            type: [Boolean, String, Number, Array, Object],
            default: null,
        },
    },
    emits: ['update:model-value'],
    data() {
        const self = this;
        const multiSelectManager = {
            optionsData: {},
            models: {},
            getOptions(optionKey) {
                if (!(optionKey in this.optionsData)) {
                    this.setOptions(self.options, optionKey);
                }
                return this.optionsData[optionKey];
            },
            setModel(optionKey, value) {
                console.log('setModel: ', optionKey, value);
                this.models[optionKey] = value;
            },
            updateOptionValue(optionKey) {
                self.resetDependenciesMap
                    .get(optionKey)
                    ?.forEach(
                        (resetDependency) =>
                            (this.models[resetDependency] = null)
                    );
                for (const optionKey in this.models) {
                    const otherSelectedOptionKeyValueEntries = Object.entries(
                        this.models
                    ).filter(
                        ([otherOptionKey, otherOptionValue]) =>
                            otherOptionKey !== optionKey &&
                            otherOptionValue !== null
                    );
                    const filteredOptions = self.options.filter((option) =>
                        otherSelectedOptionKeyValueEntries.every(
                            ([otherOptionKey, otherOptionValue]) =>
                                option[otherOptionKey] === otherOptionValue.value
                        )
                    );
                    this.setOptions(filteredOptions, optionKey);
                }
                this.checkAndUpdateOptionsValue();
            },
            // setSelections(selectedOptions) {
            //     for (const optionKey in selectedOptions) {
            //         if (optionKey in this.models) {
            //             this.models[optionKey] = selectedOptions[optionKey];
            //         }
            //     }
            // },
            resetSelections() {
                for (const optionKey in this.models) {
                    this.models[optionKey] = null;
                }
                for (const optionKey in this.optionsData) {
                    this.setOptions(self.options, optionKey);
                }
            },
            checkAndUpdateOptionsValue() {
                for (const optionKey in this.models) {
                    if (this.models[optionKey] === null) {
                        self.updateValue(null);
                        return;
                    }
                }
                const possibleSelectedOptions = self.options.filter((option) =>
                    Object.entries(this.models).every(
                        ([optionKey, optionValue]) =>
                            option[optionKey] === optionValue.value
                    )
                );
                const selectedOptions = possibleSelectedOptions?.[0] ?? null;
                if (selectedOptions === null) {
                    return;
                }
                self.updateValue(selectedOptions);
            },
            setOptions(allOptions, optionKey) {
                this.optionsData[optionKey] = Array.from(
                    new Set(allOptions.map((option) => option[optionKey]))
                ).map(option => ({ value: option }));
            }
        };
        return {
            multiSelectManager: multiSelectManager,
        };
    },
    computed: {
        // optionKeys() {
        //     if (!this.differentKeys) {
        //         return Object.keys(this.options[0]);
        //     }
        //     const optionKeys = new Set();
        //     this.options.forEach((option) => {
        //         Object.keys(option).forEach((key) => optionKeys.add(key));
        //     });
        //     return Array.from(optionKeys);
        // },
        // optionValueName() {
        //     if (this.optionValue !== null) {
        //         return this.optionValue;
        //     }
        //     return this.optionKeys?.[0] ?? '';
        // },
        resetDependenciesMap() {
            const resetDependenciesMap = new Map();
            const multiple = this.resetDependencies.every((item) =>
                Array.isArray(item)
            );
            const resetDependenciesArrays = multiple
                ? this.resetDependencies
                : [this.resetDependencies];
            for (const resetDependency of resetDependenciesArrays) {
                for (const [index, optionKey] of resetDependency
                    .slice(0, -1)
                    .entries()) {
                    if (!resetDependenciesMap.has(optionKey)) {
                        resetDependenciesMap.set(optionKey, []);
                    }
                    resetDependenciesMap
                        .get(optionKey)
                        .push(...resetDependency.slice(index + 1));
                }
            }
            return resetDependenciesMap;
        },
    },
    watch: {
        options() {
            this.multiSelectManager.optionsData = {};
        },
    },
    mounted() {
        console.log('MultiSelectContainer mounted');
    },
    expose: [],
    methods: {
        clearAllSelections() {
            this.multiSelectManager.resetSelections();
            this.updateValue(null);
        },
        updateValue(value) {
            this.$emit('update:model-value', value);
        },
    },
};
</script>
