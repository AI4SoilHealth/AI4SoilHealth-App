<template>
    <!-- <focusable-container
        ref="focusableContainer"
        :container-height="containerHeight"
        :container-width="containerWidth"
        :height="height"
        :pannable="pannable"
        :width="width"
        :zoom="zoom"
        :zoomable="zoomable"
    > -->
    <div class="outer-container" :style="outerContainerStyle">
        <div class="inner-container" :style="innerContainerStyle">
            <div class="pan-container" :style="panContainerStyle">
                <div class="zoom-container" :style="zoomContainerStyle">
                    <div
                        class="document-container"
                        :style="documentContainerStyle"
                    >
                        <slot></slot>
                    </div>
                    <q-resize-observer @resize="onContentResize" />
                </div>
            </div>
        </div>
    </div>
    <!-- </focusable-container> -->
</template>

<script>
// import { loadComponent } from '@/common/component-loader';

import {
    DefaultStyleRoundingPrecision,
    validateRoundingPrecision,
    PdfUtilsMixin,
} from '../mixins/pdf.js';

export default {
    name: 'EditingContainer',
    components: {
        // FocusableContainer: loadComponent('focusable-container'),
    },
    mixins: [PdfUtilsMixin],
    props: {
        containerHeight: {
            type: Number,
            required: true,
            validator: (value) => value > 0,
        },
        containerWidth: {
            type: Number,
            required: true,
            validator: (value) => value > 0,
        },
        minimumMargin: {
            type: Number,
            default: 50,
            validator: (value) => value >= 0,
        },
        pageGap: {
            type: Number,
            default: 20,
            validator: (value) => value >= 0,
        },
        numberOfPages: {
            type: Number,
            default: 1,
            validator: (value) => value >= 0,
        },
        pageSize: {
            type: Object,
            default: () => ({ width: 0, height: 0 }),
            validator: (value) =>
                Number.isFinite(value.width) &&
                value.width >= 0 &&
                Number.isFinite(value.height) &&
                value.height >= 0,
        },
        maximumPageBorderWidth: { type: Number, default: 0 },
        // TODO: remove unused code
        // pannable: { type: Boolean },
        // zoomable: { type: Boolean },
        zoom: {
            type: Number,
            default: null,
            validator: (value) => value > 0,
        },
        zoomSteps: {
            type: Array,
            default: () => [
                0.2, 0.27, 0.4, 0.53, 0.6, 0.64, 0.72, 0.8, 0.88, 1, 1.2, 1.4,
                1.6, 2, 2.4, 3.2, 4,
            ],
            validator: (value) =>
                value.every(
                    (step, index) =>
                        typeof step === 'number' &&
                        step > 0 &&
                        (index === 0 || step >= value[index - 1]),
                ),
        },
        zoomSkipStepMaximumDistance: {
            type: Number,
            default: 0.05,
            validator: (value) => value >= 0,
        },
        zoomSkipStepMinimumValue: {
            type: Number,
            default: 0.8,
            validator: (value) => value >= 0,
        },
        increaseZoomNumber: {
            type: Number,
            default: 0,
            validator: (value) => value >= 0,
        },
        decreaseZoomNumber: {
            type: Number,
            default: 0,
            validator: (value) => value >= 0,
        },
        resetZoomNumber: {
            type: Number,
            default: 0,
            validator: (value) => value >= 0,
        },
        styleRoundingPrecision: {
            type: Number,
            default: DefaultStyleRoundingPrecision,
            validator: validateRoundingPrecision,
        },
    },
    emits: ['update:zoom'],
    data() {
        return {
            // panX: 0,
            // panY: 0,
            zoomValue: this.zoom ?? 1,
            observedContentSize: { width: 0, height: 0 },
        };
    },
    computed: {
        outerContainerStyle() {
            return {
                width: this.getCSSPixelValue(this.containerWidth),
                height: this.getCSSPixelValue(this.containerHeight),
            };
        },
        contentSize() {
            const doubleMargin = 2 * this.minimumMargin;
            if (
                this.observedContentSize.width > doubleMargin &&
                this.observedContentSize.height > doubleMargin
            ) {
                return this.observedContentSize;
            }
            const approximateWidth =
                this.pageSize.width +
                2 * this.maximumPageBorderWidth +
                2 * this.minimumMargin;
            const approximateHeight =
                this.numberOfPages *
                    (this.pageSize.height +
                        2 * this.maximumPageBorderWidth +
                        this.pageGap) -
                this.pageGap +
                2 * this.minimumMargin;
            return {
                width: approximateWidth,
                height: approximateHeight,
            };
        },
        innerContainerStyle() {
            const scrollBarWidth = 20;
            // TODO: use scrollBarWidth / window.devicePixelRatio instead
            // so that margin is more precise, also zoom level changes
            // should be monitored with
            // media.addEventListener("change", updatePixelRatio);
            const actualWidth = this.contentSize.width * this.zoomValue;
            const actualHeight = this.contentSize.height * this.zoomValue;
            const horizontalMargin = this.roundTo(
                Math.max(
                    0,
                    (this.containerWidth - scrollBarWidth - actualWidth) / 2,
                ),
                this.styleRoundingPrecision,
            );
            const verticalMargin = this.roundTo(
                Math.max(
                    0,
                    (this.containerHeight - scrollBarWidth - actualHeight) / 2,
                ),
                this.styleRoundingPrecision,
            );
            return {
                margin: `${verticalMargin}px ${horizontalMargin}px`,
                width: this.getCSSPixelValue(
                    this.contentSize.width * this.zoomValue,
                ),
                height: this.getCSSPixelValue(
                    this.contentSize.height * this.zoomValue,
                ),
            };
        },
        panContainerStyle() {
            return {
                left: this.getCSSPixelValue(
                    (this.contentSize.width / 2) * this.zoomValue,
                ),
                top: this.getCSSPixelValue(
                    (this.contentSize.height / 2) * this.zoomValue,
                ),
            };
        },
        zoomContainerStyle() {
            return {
                transform: `scale(${this.zoomValue}) `,
            };
        },

        documentContainerStyle() {
            const margin = this.roundTo(
                this.minimumMargin,
                this.styleRoundingPrecision,
            );
            return {
                margin: `${margin}px ${margin}px`,
                gap: this.getCSSPixelValue(this.pageGap),
            };
        },
    },
    watch: {
        zoom(value) {
            if (value == undefined) {
                return;
            }
            this.zoomValue = value;
        },
        increaseZoomNumber() {
            this.increaseZoom();
        },
        decreaseZoomNumber() {
            this.decreaseZoom();
        },
        resetZoomNumber() {
            this.resetZoom();
        },
        zoomValue(value) {
            this.$emit('update:zoom', value);
        },
    },
    created() {
        if (this.zoom == undefined) {
            this.resetZoom();
        }
    },
    // async mounted() {
    //     await this.waitForRefs(['focusableContainer']);
    // },
    expose: ['getZoom', 'setZoom'],
    methods: {
        // TODO: remove exposed methods from focusable-container

        onContentResize(size) {
            this.observedContentSize.width = size.width;
            this.observedContentSize.height = size.height;
        },

        changeZoomLevel(levelChange) {
            const indexOfClosestSmaller = this.zoomSteps.findLastIndex(
                (step) => step <= this.zoomValue,
            );
            const indexChange =
                levelChange > 0 ||
                this.zoomValue == this.zoomSteps[indexOfClosestSmaller]
                    ? levelChange
                    : levelChange + 1;
            let newZoomIndex = indexOfClosestSmaller + indexChange;
            const initialZoomChange =
                this.zoomSteps[newZoomIndex] - this.zoomValue;
            if (this.zoomSteps[newZoomIndex] > this.zoomSkipStepMinimumValue) {
                if (
                    initialZoomChange > 0 &&
                    initialZoomChange < this.zoomSkipStepMaximumDistance
                ) {
                    newZoomIndex++;
                } else if (
                    initialZoomChange < 0 &&
                    initialZoomChange > -this.zoomSkipStepMaximumDistance
                ) {
                    newZoomIndex--;
                }
            }
            if (newZoomIndex < 0 || newZoomIndex >= this.zoomSteps.length) {
                return;
            }
            if (newZoomIndex < 0 || newZoomIndex >= this.zoomSteps.length) {
                return;
            }
            this.zoomValue = this.zoomSteps[newZoomIndex];
        },
        increaseZoom() {
            this.changeZoomLevel(1);
        },
        decreaseZoom() {
            this.changeZoomLevel(-1);
        },
        getZoom() {
            return this.zoomValue;
        },
        setZoom(zoomValue) {
            this.zoomValue = zoomValue;
        },
        resetZoom() {
            this.zoomValue = Math.min(
                1,
                // (this.containerWidth - 2 * this.minimumMargin) / this.width,
                this.containerWidth / this.contentSize.width,
            );
        },

        //
        // methods from mixins

        getStyleRoundingPrecision() {
            return this.styleRoundingPrecision;
        },
    },
};
</script>

<style scoped>
.outer-container {
    overflow: scroll;
    position: relative;
}

.inner-container {
    position: relative;
    overflow: hidden;
}

.pan-container {
    position: absolute;
    transform: translate(-50%, -50%);
}

.zoom-container {
    display: flex;
    justify-content: center;
    align-items: center;
    transform-origin: center;
}

.document-container {
    display: flex;
    flex-direction: column;
}
</style>
