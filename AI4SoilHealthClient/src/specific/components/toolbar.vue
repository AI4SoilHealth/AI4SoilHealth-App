<template>
    <dialog ref="dialog" class="dialog" :style="style">
        <q-card flat square ref="card">
            <q-card-section flat square class="q-pa-none q-ma-none">
                <q-btn v-if="toolbarCloseable" flat style="position:absolute; top:5px; right:5px; z-index: 9999;"
                    @click="close" padding="xs" icon="close" size="sm" />
                <div class="q-pa-none q-ma-none">
                    <slot />
                </div>
            </q-card-section>
        </q-card>
    </dialog>
</template>
<script>

/**
 * Dialog component for displaying various components.
 * 
 * @component
 * @name Dialog
 * @example
 * <Dialog />
 */

export default {
    name: "Dialog",
    props: {
        modelValue: Boolean,
        align: String,
        width: String,
        toolbarCloseable: { type: Boolean, default: true }
    },
    data() {
        return {
            value: false,
            toolbarHeight: 0
        };
    },
    computed: {
        style() {
            return {
                left: this.$store.realDrawerWidth,//(this.$q.screen.width - this.width.replace('px', '')) + "px",
                minWidth: this.$store.screenWidth + "px",
            };
        },
    },
    watch: {
        "$q.screen.width"() {
            this.calcToolbarHeight();
        },
        modelValue(val) {
            this.value = val;
            this.calcToolbarHeight();
        },
        value(val) {
            val ? this.$refs.dialog.show() : this.$refs.dialog.close();
            this.$emit("update:modelValue", val);
        }
    },
    methods: {
        calcToolbarHeight() {
            this.toolbarHeight = this.value ? this.$refs.card.$el.offsetHeight : 0;
            this.$emit("update:toolbarHeight", this.toolbarHeight);
            console.log("calcToolbar", this.toolbarHeight);
        },
        close() {
            this.$emit("update:modelValue", false);
            this.$refs.dialog.close();
            this.$emit("close");
        },
    },
    async mounted() {

        if (!this.toolbarCloseable) {
            this.value = true;
            this.$emit("update:modelValue", true);
        }

         this.modelValue || !this.toolbarCloseable ? this.$refs.dialog.show() : this.$refs.dialog.close();
        await this.$nextTick();
        console.log("mounted", this.toolbarHeight, this.value, this.$refs.card.$el.offsetHeight);
        setTimeout(() => {
            this.calcToolbarHeight();
        }, 100);
    },
}
</script>
<style scoped>
.dialog {
    position: absolute;
    border: 0;
    padding: 3px;
    margin: 0;
    z-index: 99999;
    /* transition: height 0.5s ease-in-out; */
    top: 40px;
    width: 100%;
}
</style>