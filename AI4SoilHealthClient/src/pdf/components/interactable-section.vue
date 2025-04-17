<template>
    <interactable-element
        ref="interactableElement"
        :drag-grid="dragGrid"
        :draggable="draggable"
        :emit-changes="internalEmitEvents"
        :force-inside-container="forceInsideContainer"
        :grid="grid"
        :initial-layout-style="initialLayoutStyle"
        :minimum-height="minimumHeightValue"
        :minimum-width="minimumWidthValue"
        :resizable="resizable"
        :resize-grid="resizeGrid"
        :resize-handler-color="resizeHandlerColor"
        :resize-handler-size="resizeHandlerSize"
        :resize-handlers="resizeHandlers"
        :rounding-precision="roundingPrecision"
        :use-relative-layout="useRelativeLayout"
        @drag-end="checkAndEmit('drag-end')"
        @drag-setup="checkAndEmit('drag-setup')"
        @drag-start="checkAndEmit('drag-start')"
        @drag-update="checkAndEmit('drag-update')"
        @resize-end="checkAndEmit('resize-end', true)"
        @resize-setup="checkAndEmit('resize-setup')"
        @resize-start="
            () => {
                onResizeStart();
                checkAndEmit('resize-start', true);
            }
        "
        @resize-update="
            () => {
                onResizeUpdate();
                checkAndEmit('resize-update');
            }
        "
    >
        <interactable-container
            ref="interactableContainer"
            class="interactable-container"
        >
            <slot />
        </interactable-container>
    </interactable-element>
</template>

<script>
import { loadComponent } from '@/common/component-loader';
const validateMinimumSizeProps = (value) => {
    if (typeof value === 'number') {
        return value >= 0;
    }
    if (!Array.isArray(value)) {
        return false;
    }
    if (value.length !== 2) {
        return false;
    }
    const [size, options] = value;

    return (
        typeof size === 'number' &&
        typeof options === 'object' &&
        size >= 0 &&
        Object.keys(options).includes('containElements') &&
        typeof options.containElements === 'boolean'
    );
};

const emitNames = [
    'drag-setup',
    'drag-start',
    'drag-update',
    'drag-end',
    'resize-setup',
    'resize-start',
    'resize-update',
    'resize-end',
];

export default {
    name: 'InteractableSection',
    components: {
        InteractableElement: loadComponent('interactable-element'),
        InteractableContainer: loadComponent('interactable-container'),
    },
    props: {
        // InteractableElement props
        /* eslint-disable vue/require-default-prop */
        draggable: { type: Boolean },
        emitChanges: { type: [Boolean, Array] },
        /* eslint-disable-next-line vue/no-boolean-default */
        forceInsideContainer: { type: Boolean, default: undefined },
        grid: { type: Array },
        dragGrid: { type: Array },
        resizeGrid: { type: Array },
        initialLayoutStyle: { type: Object },
        resizable: { type: Boolean },
        resizeHandlerColor: { type: String },
        resizeHandlerSize: { type: Number },
        resizeHandlers: { type: Array },
        roundingPrecision: { type: Number },
        useRelativeLayout: { type: Boolean },
        /* eslint-enable vue/require-default-prop */
        // InteractableSection props
        forceElementsInsideAfterResize: { type: Boolean },
        minimumWidth: {
            type: [Number, Array],
            default: 0,
            validator: validateMinimumSizeProps,
        },
        minimumHeight: {
            type: [Number, Array],
            default: 0,
            validator: validateMinimumSizeProps,
        },
    },
    emits: [
        'drag-setup',
        'drag-start',
        'drag-update',
        'drag-end',
        'resize-setup',
        'resize-start',
        'resize-update',
        'resize-end',
    ],
    data() {
        return {
            minimumWidthValue: this.getMinimumSizeValue(this.minimumWidth),
            minimumHeightValue: this.getMinimumSizeValue(this.minimumHeight),
        };
    },
    computed: {
        emitEvents() {
            if (Array.isArray(this.emitChanges)) {
                return this.emitChanges;
            }
            if (this.emitChanges === true) {
                return emitNames;
            } else {
                return [];
            }
        },
        internalEmitEvents() {
            const sectionEmitEvents = ['resize-start', 'resize-update'];
            const elementEmitChanges = Array.from(
                new Set([...this.emitEvents, ...sectionEmitEvents])
            );
            return elementEmitChanges;
        },
    },
    watch: {
        minimumWidth(value) {
            this.minimumWidthValue = this.getMinimumSizeValue(value);
        },
        minimumHeight(value) {
            this.minimumHeightValue = this.getMinimumSizeValue(value);
        },
    },
    expose: [
        'forceElementsInside',
        'styleChanged',
        'getPositionAndSize',
        'setPositionAndSize',
        'resetPositionAndSize',
    ],
    methods: {
        onResizeStart() {
            if (!this.forceElementsInsideAfterResize) {
                return;
            }
            [this.minimumWidthValue, this.minimumHeightValue] =
                this.$refs.interactableContainer.getContentMinimumSize();
            this.forceElementsInside({ saveRelativePosition: true });
        },

        onResizeUpdate() {
            this.forceElementsInside();
        },

        onSizeChange() {
            // console.log('section onSizeChange');
            this.forceElementsInside();
        },

        // helper methods

        getMinimumSizeValue(propValue) {
            if (typeof propValue === 'number') {
                return propValue;
            }
            return propValue[0];
        },

        checkAndEmit(eventName, waitNextTick = false) {
            if (this.emitEvents.includes(eventName)) {
                if (waitNextTick) {
                    this.$nextTick(() => this.$emit(eventName));
                } else {
                    this.$emit(eventName);
                }
            }
        },

        // exposed methods

        forceElementsInside({
            saveRelativePosition = false,
            useSavedRelativePosition = true,
        } = {}) {
            this.$refs.interactableContainer.forceElementsInside({
                saveRelativePosition,
                useSavedRelativePosition,
            });
        },

        async styleChanged(forceRefresh = false) {
            await this.$refs.interactableElement.styleChanged(forceRefresh);
            await this.$refs.interactableContainer.styleChanged(forceRefresh);
        },

        getPositionAndSize() {
            return this.$refs.interactableElement.getPositionAndSize();
        },

        setPositionAndSize(sizeAndPosition) {
            this.$refs.interactableElement.setPositionAndSize(sizeAndPosition);
        },

        async resetPositionAndSize() {
            await this.$refs.interactableElement.resetPositionAndSize();
        },
    },
};
</script>

<style scoped>
.interactable-container {
    width: 100%;
    height: 100%;
}
</style>
