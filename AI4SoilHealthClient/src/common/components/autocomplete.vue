<template>
    <q-select v-model="value" ref="select" class="q-ma-none q-pa-none" :options="localOptions"
        :option-label="localOptionLabel" :option-value="localOptionValue" :clearable="clearable" :emit-value="emitValue"
        :map-options="mapOptions" :dense="dense" :options-dense="optionsDense" :outlined="outlined"
        :filled="filled" :label="label" :bg-color="bgColor" options-html display-value-html @popup-show="startEditing"
        :disable="disable" :rules="rules" :square="square" style="min-width: 100px;" @focus="handleFocus(0)" :multiple="multiple" 
        @blur="handleBlur(0)" @keydown="keyDownSel" @update:model-value="update" no-option="No results found">
        <template v-slot:after>
            <q-btn v-if="lookup && lookup.refTable" class="q-pa-none q-ma-none" size="sm" flat dense icon="edit"
                @click="editLookup" @focus="handleFocus(2)" @blur="handleBlur(2)" />
        </template>
        <template v-slot:before-options v-if="searchable">
            <q-icon name="search" />
            <!-- <input dense ref="inputFilter" id="x" v-model="filter" style="outline: none; border:0"
                @focus="handleFocus(1)" @blur="handleBlur(1)" @keydown="keyDown" @input="setFilteredOptions"/> -->
            <input dense ref="inputFilter" id="x" v-model="filter" style="outline: none; border:0"
                @focus="handleFocus(1)" @blur="handleBlur(1)" @keydown="keyDown" @update:model-value="setFilteredOptions"/>
        </template>
        <q-btn v-if="lookup && lookup.refTable && isAdmin" class="q-pa-none q-ma-none" size="sm" flat dense icon="code"
                @click.stop="editProc"  />
    </q-select>

</template>

<script>
/**
 * Autocomplete component for selecting options from a dropdown list.
 *
 * @component Autocomplete
 *
 */
import eventBus from '@/common/event-bus';
export default {
    name: "Autocomplete",
    props: {
        disable: { type: Boolean, default: false },
        searchable: { type: Boolean, default: true },
        clearable: { type: Boolean, default: true },
        emitValue: { type: Boolean, default: false },
        mapOptions: { type: Boolean, default: false },
        optionsDense: { type: Boolean, default: true },
        outlined: { type: Boolean, default: true },
        options: { type: Array, default: () => [] },
        optionLabel: { type: String, default: "label" },
        optionValue: { type: String, default: "value" },
        filled: { type: Boolean, default: true },
        dense: { type: Boolean, default: true },
        label: { type: String, default: undefined },
        bgColor: { type: String },
        modelValue: { type: [String, Number, Array, Object, Boolean], default: null },
        rules: { type: Array, default: () => [] },
        square: { type: Boolean, default: true },
        lookup: { type: Object, default: () => null },
        multiple: { type: Boolean, default: false },
        row: { type: Object, default: null },
        fromTable: { type: Boolean, default: false }
    },
    emits: ["update:modelValue", "blur"],
    data() {
        return {
            filter: "",
            focusedChildren: [false, false, false],
            localOptionLabel: this.optionLabel,
            localOptionValue: this.optionValue,
            lastSearchValue: null,
            filteredOptions: [], 
            editLookupPopup: null,
            loaded: false
        }
    },
    computed: {
        localOptions() {
            if (this.filter > "") {
                return this.filteredOptions;
            } else if (this.lookup) {
                return this.lookup.options;
            } else {
                return this.options;
            }
        },  

        value: {
            get() {
                return this.modelValue
            },
            set(value) {
                //this.$emit('update:modelValue', value)
            }
        },

        /**
         * Returns the filtered options based on the input value.
         * @returns {Array} The filtered options.
         */
    },

    methods: {

        async setFilteredOptions() {

            // load from server if minChars is set
            if (this.lookup) {
                if (this.lookup.minChars) {
                    if (this.filter.length == this.lookup.minChars && this.filter != this.lastSearchValue) {
                        await this.loadLookup(this.lookup, false, { searchValue: this.filter });
                        if (this.lookup.options.length == 0) {
                            this.setEmptyOptions();
                        } 
                        this.localOptionLabel = this.lookup.optionLabel;
                        this.localOptionValue = this.lookup.optionValue;
                        this.lastSearchValue = this.filter;
                    }
                }
            }

            let options = [];
            if (this.lookup) {
                options = this.lookup.options;
            } else {
                options = this.options;
            }

            if (this.filter == "") {
                this.filteredOptions = options;
                return;
            }

            let s = this.filter.toLowerCase();
            if (s.indexOf('%') == 0) s = s.substring(1);

            let f = options.filter(option => option[this.localOptionLabel].toLowerCase().includes(s));

            if (f.length == 0) {
                this.filteredOptions = options;
            } else {
                this.filteredOptions = f;
            }
        },

        /**
         * Sets the empty options.
         */
        setEmptyOptions() {
            this.value = null;
            this.lookup.options = [{ value: null, label: 'Type at least ' + this.lookup.minChars + ' characters to search...' }];
            this.lookup.optionLabel = 'label';
            this.lookup.optionValue = 'value';
        },

        /**
         * Starts the editing process.
         */
        async startEditing() {
            if (this.searchable) {
                this.$nextTick(() => {
                    this.filter = "";
                    this.$refs.inputFilter.focus();
                });
            }
        },

        /**
         * Focuses the input field.
         */
        focus() {
            this.$refs.select.focus();
            this.$refs.select.showPopup();
            this.$refs.select.setOptionIndex(-1);
        },

        /**
         * Handles the focus event - sets the focusedChildren array.
         * @param key 
         */
        handleFocus(key) {
            if (key == 0) {
                this.$refs.select.focus();
            }
            this.focusedChildren[key] = true;
        },

        /**
         * Handles the blur event - sets the focusedChildren array.
         * @param key 
         */
        handleBlur(key) {
            this.focusedChildren[key] = false;
            if (this.focusedChildren.every(e => !e)) {
                this.$refs.select.hidePopup();
                this.$emit('blur');
            }
        },

        /**
         * Opens the edit lookup popup.
         */
        async editLookup() {
            this.$refs.select.hidePopup();
            this.editLookupPopup = await this.initPopup({component : 'table', tableAPI : this.lookup.refTable });
            await this.$nextTick();
        },

        /**
         * Handles the key down event (arrow down - shows the popup and focuses the first option).
         * @param {Event} e - The key down event.
         */
        keyDown(e) {
            console.log("KeyDown", e.key);
            if (e.key == 'ArrowDown') {
                this.$refs.select.showPopup();
                this.$refs.select.setOptionIndex(0);
            } 
        },

        /**
         * Handles the key down event (arrowUp - sets focust to input if necessary).
         * @param {Event} e - The key down event.
         */
        keyDownSel(e) {
            console.log("KeyDownSel", e.key);
            if (e.key == 'ArrowUp') {
                if (this.$refs.select.getOptionIndex() == 0) {
                    this.$refs.inputFilter.focus();
                }
            }
        },
        update(newValue) {
            if (this.row && this.lookup) {
                let selectedOption = this.filteredOptions.find(o => o[this.localOptionValue] == newValue);
                if (this.fromTable) {
                    this.row[this.lookup.labelColumnReference] = selectedOption[this.lookup.optionLabel];
                } else {
                    this.row[this.lookup.labelColumnName] = selectedOption[this.lookup.optionLabel];
                }
                if (this.lookup.dependentValue) {
                    if (this.fromTable) {
                        this.row[this.lookup.dependentValueReference] = selectedOption[this.lookup.dependentValue];
                        this.row[this.lookup.dependentLabelReference] = selectedOption[this.lookup.dependentLabel];
                    } else {
                        this.row[this.lookup.dependentValue] = selectedOption[this.lookup.dependentValue];
                        this.row[this.lookup.dependentLabel] = selectedOption[this.lookup.dependentLabel];
                    }
                }
            }
            this.$emit('update:modelValue', newValue);
        },

        editProc() {
            this.initPopup({component : 'crud', refTable : this.lookup.refTable, popup : 'editLookup', maximized : true});
        }
    },

    /**
     * Mounted lifecycle method - initializes the component and creates an event listener.
     */
    async mounted() {
        eventBus.on('popupClosed', async (payload) => {
            if (payload == this.editLookupPopup) {
                if (this.lookup && (!this.lookup.minChars || this.filter.length >= this.lookup.minChars)) {
                    await this.loadLookup(this.lookup);
                    this.setFilteredOptions();
                }
            }
        });
        await this.$nextTick();
        if (this.lookup) {
            if (!this.lookup.loaded && (this.lookup.minChars??0) == 0) {
                await this.loadLookup(this.lookup);
            } else if (this.lookup.minChars) {
                this.value = null;
                this.setEmptyOptions();
            }
            this.localOptionLabel = this.lookup.optionLabel;
            this.localOptionValue = this.lookup.optionValue;
        } else {
            this.localOptionLabel = this.optionLabel;
            this.localOptionValue = this.optionValue;
        }
        this.setFilteredOptions();
        this.loaded = true;
    },

    /**
     * Before unmount lifecycle method - removes the event listener.
     */
    beforeUnmount() {
        eventBus.off('popupClosed');
    },
}
</script>
<style scoped>
input:focus {
    outline: none;
}
</style>
