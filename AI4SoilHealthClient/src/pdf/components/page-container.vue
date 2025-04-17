<template>
    <div class="border-container" :style="borderContainerStyle">
        <div class="margin-container" :style="marginContainerStyle">
            <slot></slot>
        </div>
    </div>
</template>

<script>
import {
    DefaultStyleRoundingPrecision,
    validateRoundingPrecision,
    PdfUtilsMixin,
} from '../mixins/pdf.js';

export default {
    name: 'PageContainer',
    mixins: [PdfUtilsMixin],
    props: {
        height: { type: Number, default: 10, validator: (value) => value >= 0 },
        width: { type: Number, default: 10, validator: (value) => value >= 0 },
        pageBorderWidth: { type: Number, default: 0 },
        pageBorderStyle: { type: String, default: 'solid' },
        pageBorderColor: { type: String, default: 'black' },
        pageColor: { type: String, default: 'white' },
        pageMargins: {
            type: Array,
            default: () => [0, 0, 0, 0],
            validator: (value) =>
                value.length === 4 &&
                value.every((margin) => Number.isFinite(margin) && margin >= 0),
        },
        pageMarginsStyle: { type: String, default: 'solid' },
        pageMarginsColor: { type: String, default: 'transparent' },
        styleRoundingPrecision: {
            type: Number,
            default: DefaultStyleRoundingPrecision,
            validator: validateRoundingPrecision,
        },
    },
    data() {
        return {};
    },
    computed: {
        borderContainerStyle() {
            return {
                width: this.getCSSPixelValue(this.width),
                height: this.getCSSPixelValue(this.height),
                borderWidth: this.getCSSPixelValue(this.pageBorderWidth),
                borderStyle: this.pageBorderStyle,
                borderColor: this.pageBorderColor,
                backgroundColor: this.pageColor,
            };
        },
        marginContainerStyle() {
            return {
                borderBottomWidth: this.getCSSPixelValue(this.pageMargins[2]),
                borderLeftWidth: this.getCSSPixelValue(this.pageMargins[3]),
                borderRightWidth: this.getCSSPixelValue(this.pageMargins[1]),
                borderTopWidth: this.getCSSPixelValue(this.pageMargins[0]),
                borderStyle: this.pageMarginsStyle,
                borderColor: this.pageMarginsColor,
            };
        },
    },
    watch: {},
    methods: {
        //
        // methods from mixins

        getStyleRoundingPrecision() {
            return this.styleRoundingPrecision;
        },
    },
};
</script>

<style scoped>
.border-container {
    box-sizing: content-box;
}

.margin-container {
    position: relative;
    width: 100%;
    height: 100%;
    border-style: solid;
    border-color: transparent;
    box-sizing: border-box;
}
</style>
