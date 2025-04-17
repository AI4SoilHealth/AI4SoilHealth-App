<template>
    <div>
        <input v-if="editing" v-model="editValue" ref="editInput"/>
        <span v-else @click="startEditing" v-html="shownValue"/>
    </div>
</template>

<script>
/**
 * Represents the HTML input component. Not used in the current version of the application.
 * 
 * @component
 * @name HtmlInput
 * @example
 * <HtmlInput />
 */
export default {
    data() {
        return {
            editing: false,
            editValue: "",
        };
    },
    props: {
        modelValue: { type: String, default: "" },
        shownValue: { type: String, default: "" },
    },
    methods: {
        /**
         * Starts the editing mode for the HTML input component.
         */
        startEditing() {
            this.editing = true;
            this.editValue = "";  //this.shownValue;
            this.$nextTick(() => {
                this.$refs.editInput.focus();
                this.$refs.editInput.select();                
            });
        },
        /**
         * Stops the editing mode and saves the changes made to the input field.
         */
        stopEditing() {
            this.editing = false;
            this.$emit("input", this.editValue); // Emit the edited value to the parent
        },
    },
};
</script>
<style scoped>
span {
    z-index: 99999;
}
input:focus {
    outline: none;
}
</style>
