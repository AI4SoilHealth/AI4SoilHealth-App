<template>
    <div class="outer-container" :style="outerContainerStyle">
        <div class="inner-container" :style="innerContainerStyle">
            <div class="pan-container" :style="panContainerStyle">
                <div class="zoom-container" :style="zoomContainerStyle">
                    <slot />
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'FocusableContainer',
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
        height: { type: Number, default: 10, validator: (value) => value >= 0 },
        width: { type: Number, default: 10, validator: (value) => value >= 0 },
        minimumMargin: {
            type: Number,
            default: 50,
            validator: (value) => value >= 0,
        },
        pannable: { type: Boolean },
        zoomable: { type: Boolean },
        zoom: {
            type: Number,
            default: null,
            validator: (value) => value > 0,
        },
        zoomSteps: {
            type: Array,
            default: () => [
                0.2, 0.27, 0.4, 0.53, 0.6, 0.64, 0.72, 0.8, 0.88, 1.0, 1.2, 1.4,
                1.6, 2.0, 2.4, 3.2, 4.0,
            ],
            validator: (value) =>
                value.every(
                    (step, i) =>
                        typeof step === 'number' &&
                        step > 0 &&
                        (i === 0 || step >= value[i - 1])
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
    },

    data() {
        return {
            panX: 0,
            panY: 0,
            zoomValue: this.zoom,
        };
    },
    computed: {
        outerContainerStyle() {
            return {
                width: `${this.containerWidth}px`,
                height: `${this.containerHeight}px`,
            };
        },
        innerContainerStyle() {
            const scrollBarWidth = 20;
            // TODO: use scrollBarWidth / window.devicePixelRatio instead
            // so that margin is more precise, also zoom level changes
            // should be monitored with
            // media.addEventListener("change", updatePixelRatio);
            const actualWidth = this.width * this.zoomValue;
            const actualHeight = this.height * this.zoomValue;
            const horizontalMargin = Math.max(
                this.minimumMargin,
                (this.containerWidth - scrollBarWidth - actualWidth) / 2
            );
            const verticalMargin = Math.max(
                this.minimumMargin,
                (this.containerHeight - scrollBarWidth - actualHeight) / 2
            );
            return {
                margin: `${verticalMargin}px ${horizontalMargin}px`,
                width: `${this.width * this.zoomValue}px`,
                height: `${this.height * this.zoomValue}px`,
            };
        },
        panContainerStyle() {
            return {
                left: `${(this.width / 2) * this.zoomValue}px`,
                top: `${(this.height / 2) * this.zoomValue}px`,
            };
        },
        zoomContainerStyle() {
            return {
                width: `${this.width}px`,
                height: `${this.height}px`,
                transform: `scale(${this.zoomable ? this.zoomValue : 1}) `,
            };
        },
    },
    watch: {
        width() {
            this.panX = this.width / 2;
        },
        height() {
            this.panY = this.height / 2;
        },
        zoom(value) {
            if (value == null) {
                return;
            }
            this.zoomValue = value;
        },
    },
    created() {
        if (this.zoom == null) {
            this.resetZoom();
        }
    },
    expose: ['increaseZoom', 'decreaseZoom', 'getZoom', 'setZoom', 'resetZoom'],
    methods: {
        changeZoomLevel(levelChange) {
            const indexOfClosestSmaller = this.zoomSteps.findLastIndex(
                (step) => step <= this.zoomValue
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
                (this.containerWidth - 2 * this.minimumMargin) / this.width
            );
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
    transform-origin: center;
}
</style>
