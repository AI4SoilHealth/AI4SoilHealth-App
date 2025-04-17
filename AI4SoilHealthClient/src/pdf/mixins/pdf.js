/**
 * @desc A mixin object containing methods that are used both by
 * pdf-report-editor and pdf-report-preview,
 * and also other functionalities that are used by multiple components.
 * @module PdfReportMixin
 */

import { colors } from 'quasar';

// import html2pdf from 'html2pdf.js';
import { jsPDF } from 'jspdf';
// TODO: check if jsPDF is the best option,
// it has minified sized of 350 KB,
// also check if it is loaded only when needed

export const MarginValuesInCm = {
    // top right bottom left
    Narrow: [0.5, 0.5, 0.5, 0.5],
    Moderate: [1, 0.75, 1, 0.75],
    Normal: [1, 1, 1, 1],
    Wide: [1, 2, 1, 2],
};

// TODO: remove duplicated definitions
const DefaultFillColor = '#01010100';

export const DefaultPdfReportState = {
    zoom: 1,
    pageSize: {
        width: 794,
        height: 1123,
    },
    pageOrientation: 'portrait',
    marginsInCm: MarginValuesInCm.Normal,
    areMarginsShown: false,
    sections: {
        documentHeader: {
            text: 'Document Header',
            layout: {
                top: [0, { useRelativeUnits: true }],
                width: [1, { useRelativeUnits: true }],
                height: [0.1, { useRelativeUnits: true }],
            },
            elements: [],
            styleData: { fillColor: DefaultFillColor, fillColorRules: [] },
        },
        pageHeader: {
            text: 'Page Header',
            layout: {
                top: [0.1, { useRelativeUnits: true }],
                width: [1, { useRelativeUnits: true }],
                height: [0.1, { useRelativeUnits: true }],
            },
            elements: [],
            styleData: { fillColor: DefaultFillColor, fillColorRules: [] },
        },

        details: {
            text: 'Details',
            layout: {
                top: [0.2, { useRelativeUnits: true }],
                width: [1, { useRelativeUnits: true }],
                height: [0.1, { useRelativeUnits: true }],
            },
            elements: [],
            styleData: { fillColor: DefaultFillColor, fillColorRules: [] },
        },
        pageFooter: {
            text: 'Page Footer',
            layout: {
                top: [0.8, { useRelativeUnits: true }],
                width: [1, { useRelativeUnits: true }],
                height: [0.1, { useRelativeUnits: true }],
            },
            elements: [],
            styleData: { fillColor: DefaultFillColor, fillColorRules: [] },
        },
        documentFooter: {
            text: 'Document Footer',
            layout: {
                top: [0.9, { useRelativeUnits: true }],
                width: [1, { useRelativeUnits: true }],
                height: [0.1, { useRelativeUnits: true }],
            },
            elements: [],
            styleData: { fillColor: DefaultFillColor, fillColorRules: [] },
        },
    },
    maxElementId: 0,
    elements: {},
};

export const validatePdfReportState = (value) =>
    // state is an object that has all the keys of DefaultPdfReportState
    typeof value === 'object' &&
    Object.keys(DefaultPdfReportState).every((key) => key in value) &&
    // sections validation
    Object.entries(value.sections).every(
        ([sectionKey, sectionValue]) =>
            sectionKey in DefaultPdfReportState.sections &&
            typeof sectionValue === 'object' &&
            Object.entries(DefaultPdfReportState.sections[sectionKey]).every(
                ([itemKey, itemValue]) =>
                    itemKey in sectionValue &&
                    typeof itemValue === typeof sectionValue[itemKey],
            ),
    ) &&
    // marginsInCm validation
    Array.isArray(value.marginsInCm) &&
    value.marginsInCm.length === 4 &&
    value.marginsInCm.every((size) => Number.isFinite(size) && size >= 0) &&
    // pageSize validation
    typeof value.pageSize === 'object' &&
    Object.keys(value.pageSize).includes('width') &&
    Number.isFinite(value.pageSize.width) &&
    value.pageSize.width > 0 &&
    Object.keys(value.pageSize).includes('height') &&
    Number.isFinite(value.pageSize.height) &&
    value.pageSize.height > 0;

// Low style rounding precision can cause elements to flicker
// if relative layout is used. Recommended value is at least 2.
export const DefaultStyleRoundingPrecision = 4;

// The internal rounding precision is used to
// round position and size values of the element, main purpose
// is to round width, height, top and left values
// to avoid small imperceptible changes in the element position
// because each such change the element's style.
export const DefaultInternalRoundingPrecision =
    DefaultStyleRoundingPrecision + 2;

export const validateRoundingPrecision = (value) =>
    Number.isInteger(value) && value >= 0;

// const DEBUG = false;
const DEBUG = true;

export const PdfReportMixin = {
    data() {
        const a4PageSizeInCm = [21, 29.7];
        const a4PageSizeInPx = [794, 1123];
        // const cmToPx = 37.79527559055118;
        const cmToPx =
            (a4PageSizeInPx[0] / a4PageSizeInCm[0] +
                a4PageSizeInPx[0] / a4PageSizeInCm[0]) /
            2;
        return {
            // TODO: maybe use as props instead of hardcoding
            preferredVerticalPadding: 50, // it needs to be at least 24
            preferredHorizontalPadding: 100,
            minimumHorizontalPadding: 50,
            // horizontalPadding: 200,
            // editingToolbarHeight: 40,
            editingToolbarSize: { width: 0, height: 0 },
            cmToPx: cmToPx,
            marginValuesInCm: MarginValuesInCm,
            zoomPercentageInput: '',
            increaseZoomNumber: 0,
            decreaseZoomNumber: 0,
            resetZoomNumber: 0,
            // cached
            elementsTextsStyleDataAndStyles: {},
            elementsStyleDataAndStyles: {},
            sectionsStyleDataAndStyles: {},

            elementTextStyleNameConverter: {
                textColor: 'color',
            },
            elementTextStyleConverter: {
                fontSize: (value) => this.getCSSPixelValue(value),
                fontFamily: (value) => value,
                fontWeight: (value) => (value === 'bold' ? 'bold' : 'normal'),
                fontStyle: (value) =>
                    value === 'italic' ? 'italic' : 'normal',
                textDecoration: (value) =>
                    value === 'underline' ? 'underline' : 'none',
                textAlign: (value) => value,
                textColor: (value) => value,
            },
            elementStyleNameConverter: {
                fillColor: 'backgroundColor',
            },
            elementStyleConverter: {
                fillColor: (value) => value,
            },
        };
    },
    computed: {
        horizontalPadding() {
            if (this.$q.screen.width < 800) {
                return 0;
            }
            if (this.$q.screen.width < 1024) {
                return this.minimumHorizontalPadding;
            }
            if (this.$q.screen.width <= 1440) {
                return (
                    this.minimumHorizontalPadding +
                    ((this.preferredHorizontalPadding -
                        this.minimumHorizontalPadding) *
                        (this.$q.screen.width - 1024)) /
                        (1440 - 1024)
                );
            }
            return (
                this.preferredHorizontalPadding +
                (this.$q.screen.width - 1440) / 2
            );
            // minimumHorizontalPadding;
            // const maximumHorizontalPadding =
            //     (this.$q.screen.width - this.editingToolbarSize.width) / 2;
            // return Math.min(
            //     this.preferredHorizontalPadding,
            //     maximumHorizontalPadding,
            // );
        },

        verticalPadding() {
            if (this.$q.screen.width < 800) {
                return 0;
            }
            return this.preferredVerticalPadding;
        },
        containerHeight() {
            const precision = this.getInternalRoundingPrecision();
            const headerHeight = this.getHeaderHeight();
            const availableHeight =
                this.$q.screen.height -
                headerHeight -
                this.editingToolbarSize.height;
            if (this.parentPopup) {
                return this.roundTo(
                    availableHeight - 2 * this.verticalPadding,
                    precision,
                );
            }
            const top = this.getTop();
            return this.roundTo(availableHeight - top, precision);
        },
        containerWidth() {
            const precision = this.getInternalRoundingPrecision();
            if (this.parentPopup) {
                return this.roundTo(
                    this.$q.screen.width - 2 * this.horizontalPadding,
                    precision,
                );
            }
            return this.roundTo(
                this.$q.screen.width -
                    (this.$store.drawer ? this.$store.drawerWidth : 0),
                precision,
            );
        },
        marginsInPx() {
            const state = this.getPdfReportState();
            const marginsInCm = state?.marginsInCm ?? [0, 0, 0, 0];
            const precision = this.getInternalRoundingPrecision();
            return marginsInCm.map((margin) =>
                this.roundTo(margin * this.cmToPx, precision),
            );
        },
        isStateSet() {
            const state = this.getPdfReportState();
            // TODO: move this to editor component
            return state !== null && this.editingState !== null;
        },
        pageContainerSize() {
            const state = this.getPdfReportState();
            if (state === null) {
                // TODO: check if null or undefined should be returned
                return null;
            }
            const precision = this.getInternalRoundingPrecision();
            return {
                width: this.roundTo(
                    state.pageSize.width -
                        this.marginsInPx[1] -
                        this.marginsInPx[3],
                    precision,
                ),
                height: this.roundTo(
                    state.pageSize.height -
                        this.marginsInPx[0] -
                        this.marginsInPx[2],
                    precision,
                ),
            };
        },
    },
    methods: {
        onEditingToolbarResize(size) {
            this.editingToolbarSize = size;
        },
        decreaseZoomLevel() {
            this.decreaseZoomNumber++;
        },
        resetZoomPercentageInput() {
            const state = this.getPdfReportState();
            this.zoomPercentageInput = `${Math.round(state.zoom * 100)}%`;
        },
        setCustomZoom() {
            const state = this.getPdfReportState();
            const value = this.zoomPercentageInput;
            const numericPart = value.endsWith('%')
                ? value.slice(0, -1)
                : value;
            const zoom = Math.round(Number(numericPart)) / 100;
            if (Number.isNaN(zoom)) {
                this.resetZoomPercentageInput();
                return;
            }
            // TODO: check if this is necessary or can be simplified
            // if (this.zoom === zoom) {
            if (state.zoom === zoom) {
                this.resetZoomPercentageInput();
                return;
            }
            state.zoom = zoom;
        },
        increaseZoomLevel() {
            this.increaseZoomNumber++;
        },
        isDataChanged(newStyleData, oldStyleData) {
            if (oldStyleData === undefined) {
                return true;
            }
            return !this.deepEqualObjects(oldStyleData, newStyleData);
        },
        getSectionStyle(sectionId) {
            const state = this.getPdfReportState();
            const nameConverter = {
                fillColor: 'backgroundColor',
            };
            const converter = {
                fillColor: (value) => value,
            };
            const styleDataEntries = Object.entries(
                state.sections[sectionId].styleData,
            ).filter(([property, _value]) => property in converter);
            const oldStyleDataEntries =
                this.sectionsStyleDataAndStyles?.[sectionId]?.styleDataEntries;
            if (!this.isDataChanged(styleDataEntries, oldStyleDataEntries)) {
                return this.sectionsStyleDataAndStyles[sectionId].style;
            }
            const style = Object.fromEntries(
                styleDataEntries.map(([property, value]) => [
                    nameConverter[property] ?? property,
                    converter[property](value),
                ]),
            );
            this.sectionsStyleDataAndStyles[sectionId] = {
                styleDataEntries,
                style,
            };
            return style;
        },
        getElementStyle(elementId) {
            const state = this.getPdfReportState();
            const styleDataEntries = Object.entries(
                state.elements[elementId].styleData,
            ).filter(
                ([property, _value]) => property in this.elementStyleConverter,
            );
            const oldStyleDataEntries =
                this.elementsStyleDataAndStyles?.[elementId]?.styleDataEntries;
            if (!this.isDataChanged(styleDataEntries, oldStyleDataEntries)) {
                return this.elementsStyleDataAndStyles[elementId].style;
            }
            const style = Object.fromEntries(
                styleDataEntries.map(([property, value]) => [
                    this.elementStyleNameConverter[property] ?? property,
                    this.elementStyleConverter[property](value),
                ]),
            );
            this.elementsStyleDataAndStyles[elementId] = {
                styleDataEntries,
                style,
            };
            return style;
        },
        getElementTextStyle(elementId) {
            const state = this.getPdfReportState();

            const styleDataEntries = Object.entries(
                state.elements[elementId].styleData,
            ).filter(
                ([property, _value]) =>
                    property in this.elementTextStyleConverter,
            );
            const oldStyleDataEntries =
                this.elementsTextsStyleDataAndStyles?.[elementId]
                    ?.styleDataEntries;
            if (!this.isDataChanged(styleDataEntries, oldStyleDataEntries)) {
                return this.elementsTextsStyleDataAndStyles[elementId].style;
            }
            const style = Object.fromEntries(
                styleDataEntries.map(([property, value]) => [
                    this.elementTextStyleNameConverter[property] ?? property,
                    this.elementTextStyleConverter[property](value),
                ]),
            );
            this.elementsTextsStyleDataAndStyles[elementId] = {
                styleDataEntries,
                style,
            };
            return style;
        },

        async blobToBase64(blob) {
            return new Promise((resolve, _reject) => {
                const reader = new FileReader();
                reader.addEventListener('loadend', () =>
                    resolve(reader.result),
                );
                reader.readAsDataURL(blob);
            });
        },

        // async getImageData(imageUrl) {
        //     // TODO: fix getting image data
        //     // return null;
        //     const response = await this.axios.API.get(imageUrl, {
        //         responseType: 'blob',
        //     });
        //     if (response && response.data) {
        //         const blob = new Blob([response.data], {
        //             type: response.headers['content-type'],
        //         });
        //         const base64Data = await this.blobToBase64(blob);
        //         return base64Data;
        //     }
        //     return null;
        // },

        getLoadedImage(imageUrl) {
            return new Promise((resolve, reject) => {
                const image = new Image();
                // let crossOrigin = 'Anonymous'
                // if (crossOrigin) {
                //   img.crossOrigin = crossOrigin
                // }
                image.crossOrigin = 'Anonymous';
                image.addEventListener('load', (_event) => {
                    resolve(image);
                });
                image.addEventListener('error', (event) => {
                    reject(event);
                });
                image.src = imageUrl;
            });
        },

        async getLoadedImageIfAccessible(imageUrl) {
            try {
                const image = await this.getLoadedImage(imageUrl);
                return image;
            } catch {
                return null;
            }
        },

        getBlobFromCanvas(canvas, type, quality) {
            return new Promise((resolve) => {
                canvas.toBlob(resolve, type, quality);
            });
        },

        async getImageData(
            imageUrl,
            { size, type = 'image/png', quality = 1 } = {},
        ) {
            // TODO: check if size should be null or undefined as default
            const image = await this.getLoadedImageIfAccessible(imageUrl);
            if (image === null) {
                return null;
            }
            if (size === undefined) {
                size = { width: image.width, height: image.height };
            }
            const canvas = document.createElement('canvas');
            canvas.width = size.width;
            canvas.height = size.height;
            const context = canvas.getContext('2d');
            context.drawImage(image, 0, 0, size.width, size.height);
            const blob = await this.getBlobFromCanvas(canvas, type, quality);
            const base64Data = await this.blobToBase64(blob);
            return base64Data;
        },

        getLayoutWithOffset(layout, offset) {
            return {
                left: (layout?.left ?? 0) + (offset?.left ?? 0),
                top: (layout?.top ?? 0) + (offset?.top ?? 0),
                width: layout.width,
                height: layout.height,
            };
        },

        getPreviewElements(sectionId, { elementsTexts, pageNumber } = {}) {
            const state = this.getPdfReportState();
            return state.sections[sectionId].elements.map(
                (elementId, elementIndex) => {
                    const elementType = state.elements[elementId].type;
                    const elementData = {
                        id: elementId,
                        type: elementType,
                    };
                    switch (elementType) {
                        case 'text': {
                            elementData.text = `${
                                elementsTexts === undefined
                                    ? state.elements[elementId].text
                                    : (elementsTexts[elementIndex] ?? '')
                            }`;
                            // TODO: improve html detection
                            elementData.html =
                                elementData.text.includes('<sub>');
                            break;
                        }
                        case 'page_number': {
                            elementData.text = `${pageNumber ?? ''}`;
                            break;
                        }
                        case 'image': {
                            elementData.imageUrl =
                                state.elements[elementId].imageUrl;
                            break;
                        }
                        default: {
                            elementData.text = '';
                        }
                    }
                    return elementData;
                },
            );
        },

        getPreviewData() {
            const state = this.getPdfReportState();
            const previewData = {
                sections: state.sections,
                elements: state.elements,
                pages: [],
                pageSize: state.pageSize,
                marginsInPx: this.marginsInPx,
            };
            previewData.pages.push([]);
            //
            // add document header data
            if ('documentHeader' in state.sections) {
                previewData.pages[0].push({
                    sectionId: 'documentHeader',
                    sectionLayout: state.sections.documentHeader.layout,
                    elements: this.getPreviewElements('documentHeader'),
                    rowIndex: 1,
                });
            }
            // add page data
            if (state.sections?.details?.elements?.length > 0) {
                const detailsRowsTexts = state.sections.details
                    ?.multiRowsTexts ?? [
                    state.sections.details?.elements.map(
                        (elementId) => state.elements[elementId].text,
                    ),
                ];
                let currentDetailsRowNumber = 0;
                const documentHeaderTop =
                    state.sections?.documentHeader?.layout?.top ?? 0;
                const pageHeaderTop =
                    state.sections?.pageHeader?.layout?.top ??
                    documentHeaderTop;
                const documentAndPageHeaderHeightOffset =
                    pageHeaderTop - documentHeaderTop;
                // detailsRowsTexts.splice(10, detailsRowsTexts.length - 10);
                while (currentDetailsRowNumber < detailsRowsTexts.length) {
                    let pageNumber = previewData.pages.length;
                    const headerTopOffset =
                        pageNumber > 1 ? -documentAndPageHeaderHeightOffset : 0;
                    //
                    // add page header
                    if ('pageHeader' in state.sections) {
                        previewData.pages.at(-1).push({
                            sectionId: 'pageHeader',
                            sectionLayout: this.getLayoutWithOffset(
                                state.sections.pageHeader.layout,
                                { top: headerTopOffset },
                            ),
                            elements: this.getPreviewElements('pageHeader', {
                                pageNumber,
                            }),
                            rowIndex: pageNumber,
                        });
                    }
                    //
                    // add details
                    let documentFooterTop =
                        state.sections?.documentFooter?.layout?.top;
                    if (documentFooterTop === undefined) {
                        documentFooterTop = this.pageContainerSize.height;
                    }
                    const detailsAndDocumentFooterHeightOffset =
                        documentFooterTop - state.sections.details.layout.top;
                    const maximumAvailableHeight =
                        detailsAndDocumentFooterHeightOffset - headerTopOffset;
                    const detailsHeight = state.sections.details.layout.height;
                    let detailsNumberOfRows = Math.floor(
                        maximumAvailableHeight / detailsHeight,
                    );
                    let isLastPage =
                        currentDetailsRowNumber + detailsNumberOfRows >=
                        detailsRowsTexts.length;
                    let nextFooterTop =
                        state.sections?.pageFooter?.layout?.top ??
                        state.sections?.documentFooter?.layout?.top;
                    if (nextFooterTop === undefined) {
                        nextFooterTop = this.pageContainerSize.height;
                    }
                    const detailsAndNextFooterHeightOffset =
                        nextFooterTop - state.sections.details.layout.top;
                    const minimumAvailableHeight =
                        detailsAndNextFooterHeightOffset - headerTopOffset;
                    const maximumDetailsRowsOnLastPage = Math.floor(
                        minimumAvailableHeight / detailsHeight,
                    );
                    while (
                        isLastPage &&
                        detailsNumberOfRows > maximumDetailsRowsOnLastPage
                    ) {
                        detailsNumberOfRows--;
                        isLastPage =
                            currentDetailsRowNumber + detailsNumberOfRows >=
                            detailsRowsTexts.length;
                    }
                    // TODO: use for range loop instead of slice
                    // update: maybe not
                    const sectionRowsTextsInPage = detailsRowsTexts.slice(
                        currentDetailsRowNumber,
                        currentDetailsRowNumber + detailsNumberOfRows,
                    );
                    for (const [
                        rowElementsIndex,
                        rowElementsTexts,
                    ] of sectionRowsTextsInPage.entries()) {
                        previewData.pages.at(-1).push({
                            sectionId: 'details',
                            sectionLayout: this.getLayoutWithOffset(
                                state.sections.details.layout,
                                {
                                    top:
                                        headerTopOffset +
                                        rowElementsIndex * detailsHeight,
                                },
                            ),
                            elements: this.getPreviewElements('details', {
                                elementsTexts: rowElementsTexts,
                            }),
                            rowIndex:
                                currentDetailsRowNumber + rowElementsIndex,
                        });
                    }
                    currentDetailsRowNumber += detailsNumberOfRows;
                    //
                    // add page footer
                    if ('pageFooter' in state.sections) {
                        const pageAndDocumentFooterHeightOffset =
                            'documentFooter' in state.sections
                                ? state.sections.documentFooter.layout.top -
                                  state.sections.pageFooter.layout.top
                                : 0;
                        const footerTopOffset = isLastPage
                            ? 0
                            : pageAndDocumentFooterHeightOffset;
                        previewData.pages.at(-1).push({
                            sectionId: 'pageFooter',
                            sectionLayout: this.getLayoutWithOffset(
                                state.sections.pageFooter.layout,
                                { top: footerTopOffset },
                            ),
                            elements: this.getPreviewElements('pageFooter', {
                                pageNumber,
                            }),
                            rowIndex: pageNumber,
                        });
                    }
                    if (!isLastPage) {
                        previewData.pages.push([]);
                    }
                }
            } else {
                //
                // add page header
                if ('pageHeader' in state.sections) {
                    previewData.pages.at(-1).push({
                        sectionId: 'pageHeader',
                        sectionLayout: this.getLayoutWithOffset(
                            state.sections.pageHeader.layout,
                        ),
                        elements: this.getPreviewElements('pageHeader', {}),
                        rowIndex: previewData.pages.length,
                    });
                }
                //
                // add page footer
                if ('pageFooter' in state.sections) {
                    previewData.pages.at(-1).push({
                        sectionId: 'pageFooter',
                        sectionLayout: this.getLayoutWithOffset(
                            state.sections.pageFooter.layout,
                        ),
                        elements: this.getPreviewElements('pageFooter', {}),
                        rowIndex: previewData.pages.length,
                    });
                }
            }
            // add document footer
            if ('documentFooter' in state.sections) {
                previewData.pages.at(-1).push({
                    sectionId: 'documentFooter',
                    sectionLayout: state.sections.documentFooter.layout,
                    elements: this.getPreviewElements('documentFooter'),
                    rowIndex: 1,
                });
            }
            return previewData;
        },

        addColoredElementToDocumentReport(
            setColorCallback,
            addElementCallback,
            reportDocument,
            opacity = 1,
        ) {
            if (opacity === 0) {
                return;
            }
            if (opacity < 100) {
                reportDocument.saveGraphicsState();
                reportDocument.setGState(
                    new reportDocument.GState({ opacity: opacity / 100 }),
                );
            }
            setColorCallback();
            addElementCallback();
            if (opacity < 100) {
                reportDocument.restoreGraphicsState();
            }
        },

        getConditionalColor(colorRules, arguments_ = {}, defaultColor) {
            let color = defaultColor;
            if (colorRules.length > 0) {
                const colorRuleFunctions = colorRules.map(
                    (colorRule) =>
                        new Function(
                            ...Object.keys(arguments_),
                            `return ${colorRule.function}`,
                        ),
                );
                const applyRuleIndex = colorRuleFunctions.findIndex(
                    (ruleFunction) =>
                        ruleFunction(...Object.values(arguments_)),
                );
                if (applyRuleIndex !== -1) {
                    color = colorRules[applyRuleIndex].color;
                }
            }
            return color;
        },

        getTextElementFillColor(elementData, elementStyleData, rowIndex) {
            return this.getConditionalColor(
                elementStyleData.fillColorRules,
                { value: elementData.text, rowIndex },
                elementStyleData.fillColor,
            );
        },

        getTextElementTextColor(elementData, elementStyleData, rowIndex) {
            return this.getConditionalColor(
                elementStyleData.textColorRules,
                { value: elementData.text, rowIndex },
                elementStyleData.textColor,
            );
        },

        getHtmlTextContainer(
            elementData,
            elementAbsoluteLayout,
            elementStyleData,
        ) {
            const elementColorData = {
                fillColor: this.getTextElementFillColor(
                    elementData,
                    elementStyleData,
                ),
                textColor: this.getTextElementTextColor(
                    elementData,
                    elementStyleData,
                ),
            };
            const styleDataEntries = Object.entries(elementStyleData).filter(
                ([property, _value]) => property in this.elementStyleConverter,
            );
            const fillColorEntry = styleDataEntries.find(
                ([property, _value]) => property === 'fillColor',
            );
            if (fillColorEntry !== undefined) {
                fillColorEntry[1] = elementColorData.fillColor;
            }
            const containerStyle = Object.fromEntries(
                styleDataEntries.map(([property, value]) => [
                    this.elementStyleNameConverter[property] ?? property,
                    this.elementStyleConverter[property](value),
                ]),
            );
            containerStyle.width = this.getCSSPixelValue(
                elementAbsoluteLayout.width,
            );
            containerStyle.height = this.getCSSPixelValue(
                elementAbsoluteLayout.height,
            );
            const textStyleDataEntries = Object.entries(
                elementStyleData,
            ).filter(
                ([property, _value]) =>
                    property in this.elementTextStyleConverter,
            );
            const textColorEntry = textStyleDataEntries.find(
                ([property, _value]) => property === 'textColor',
            );
            if (textColorEntry !== undefined) {
                textColorEntry[1] = elementColorData.textColor;
            }
            const textStyle = Object.fromEntries(
                textStyleDataEntries.map(([property, value]) => [
                    this.elementTextStyleNameConverter[property] ?? property,
                    this.elementTextStyleConverter[property](value),
                ]),
            );
            const textContainer = document.createElement('div');
            const textElement = document.createElement('div');
            textElement.innerHTML = elementData.text;
            textContainer.append(textElement);
            Object.assign(textContainer.style, containerStyle);
            Object.assign(textElement.style, textStyle);
            return textContainer;
        },

        async addHtmlTextElementDataToDocumentReport(
            elementData,
            previewData,
            reportDocument,
            sectionLayoutWithMargins,
        ) {
            console.log(elementData);
            // if ((this._out ?? 0) > 3) {
            //     return;
            // }
            // this._out = (this._out ?? 0) + 1;
            // break;
            const element = previewData.elements[elementData.id];
            const elementStyleData = element.styleData;
            const absoluteLayout = this.getLayoutWithOffset(
                element.layout,
                sectionLayoutWithMargins,
            );
            const textContainer = this.getHtmlTextContainer(
                elementData,
                absoluteLayout,
                elementStyleData,
            );
            const currentPageNumber =
                reportDocument.getCurrentPageInfo().pageNumber;
            await reportDocument.html(textContainer, {
                x: absoluteLayout.left,
                y: absoluteLayout.top,
            });
            reportDocument.setPage(currentPageNumber);
        },

        addTextElementRectangleToDocumentReport(
            elementData,
            elementStyleData,
            absoluteLayout,
            reportDocument,
            rowIndex,
        ) {
            const fillColor = this.getTextElementFillColor(
                elementData,
                elementStyleData,
                rowIndex,
            );
            const fillColorRgba = colors.textToRgb(fillColor);
            this.addColoredElementToDocumentReport(
                () =>
                    reportDocument.setFillColor(
                        fillColorRgba.r,
                        fillColorRgba.g,
                        fillColorRgba.b,
                    ),
                () =>
                    reportDocument.rect(
                        absoluteLayout.left,
                        absoluteLayout.top,
                        absoluteLayout.width,
                        absoluteLayout.height,
                        'F',
                    ),
                reportDocument,
                fillColorRgba.a,
            );
        },

        getTextElementOptionsForDocumentReport(
            elementData,
            elementStyleData,
            reportDocument,
            fontList,
            defaultFontFamily,
            rowIndex,
        ) {
            const fontFamily =
                elementStyleData.fontFamily in fontList
                    ? elementStyleData.fontFamily
                    : defaultFontFamily;
            const fontStyleBold =
                elementStyleData.fontWeight === 'bold' ? 'bold' : '';
            const fontStyleItalic =
                elementStyleData.fontStyle === 'italic' ? 'italic' : '';
            let fontStyle = fontStyleBold + fontStyleItalic;
            if (fontStyle === '') {
                fontStyle = fontList[fontFamily][0];
            }
            const fontData = {
                family: fontFamily,
                style: fontStyle,
                size: elementStyleData.fontSize,
            };
            const textColor = this.getTextElementTextColor(
                elementData,
                elementStyleData,
                rowIndex,
            );
            const textColorRgba = colors.textToRgb(textColor);
            return [fontData, textColorRgba];
        },

        addColoredTextToDocumentReport(
            textLines,
            textPosition,
            textColorRgba,
            textAlign,
            reportDocument,
            { textBaseline = 'middle' } = {},
        ) {
            this.addColoredElementToDocumentReport(
                () =>
                    reportDocument.setTextColor(
                        textColorRgba.r,
                        textColorRgba.g,
                        textColorRgba.b,
                    ),
                () =>
                    reportDocument.text(
                        textLines,
                        textPosition.left,
                        textPosition.top,
                        {
                            align: textAlign,
                            baseline: textBaseline,
                        },
                    ),
                reportDocument,
                textColorRgba.a,
            );
        },

        async addTextElementDataToDocumentReport(
            elementData,
            previewData,
            reportDocument,
            sectionLayoutWithMargins,
            fontList,
            defaultFontFamily,
            rowIndex,
        ) {
            const element = previewData.elements[elementData.id];
            const elementStyleData = element.styleData;
            const absoluteLayout = this.getLayoutWithOffset(
                element.layout,
                sectionLayoutWithMargins,
            );
            this.addTextElementRectangleToDocumentReport(
                elementData,
                elementStyleData,
                absoluteLayout,
                reportDocument,
                rowIndex,
            );
            // add text
            const [fontData, textColorRgba] =
                this.getTextElementOptionsForDocumentReport(
                    elementData,
                    elementStyleData,
                    reportDocument,
                    fontList,
                    defaultFontFamily,
                    rowIndex,
                );
            reportDocument.setFont(fontData.family, fontData.style);
            reportDocument.setFontSize(fontData.size);
            const textAlign = ['left', 'center', 'right', 'justify'].includes(
                elementStyleData.textAlign,
            )
                ? elementStyleData.textAlign
                : 'left';
            const textPosition = {
                top: absoluteLayout.top + absoluteLayout.height / 2,
            };
            // eslint-disable-next-line unicorn/prefer-switch
            if (textAlign === 'left') {
                textPosition.left = absoluteLayout.left;
            } else if (textAlign === 'center') {
                textPosition.left =
                    absoluteLayout.left + absoluteLayout.width / 2;
            } else if (textAlign === 'right') {
                textPosition.left = absoluteLayout.left + absoluteLayout.width;
            }
            const textWrap = elementStyleData.textWrap;
            const textLines = [];
            if (textWrap === 'overflow') {
                textLines.push(elementData.text);
            } else {
                const splittedText = reportDocument.splitTextToSize(
                    elementData.text,
                    absoluteLayout.width,
                );
                if (textWrap === 'wrap') {
                    // TODO: fix when text is wrapped that text box is expanded in height
                    textLines.push(...splittedText);
                } else if (textWrap === 'clip') {
                    textLines.push(splittedText[0]);
                }
            }
            this.addColoredTextToDocumentReport(
                textLines,
                textPosition,
                textColorRgba,
                textAlign,
                reportDocument,
            );
        },

        addSpecialTextElementDataToDocumentReport(
            specialTextParts,
            elementData,
            previewData,
            reportDocument,
            sectionLayoutWithMargins,
            fontList,
            defaultFontFamily,
            rowIndex,
        ) {
            const element = previewData.elements[elementData.id];
            const elementStyleData = element.styleData;
            const absoluteLayout = this.getLayoutWithOffset(
                element.layout,
                sectionLayoutWithMargins,
            );
            this.addTextElementRectangleToDocumentReport(
                elementData,
                elementStyleData,
                absoluteLayout,
                reportDocument,
                rowIndex,
            );
            // add text
            const [fontData, textColorRgba] =
                this.getTextElementOptionsForDocumentReport(
                    elementData,
                    elementStyleData,
                    reportDocument,
                    fontList,
                    defaultFontFamily,
                    rowIndex,
                );
            const textAlign = ['left', 'center', 'right', 'justify'].includes(
                elementStyleData.textAlign,
            )
                ? elementStyleData.textAlign
                : 'left';
            reportDocument.setFont(fontData.family, fontData.style);
            reportDocument.setFontSize(fontData.size);

            const textWrap = elementStyleData.textWrap;
            const partsByLines = [[]];
            let lineTextWidth = 0;
            let lineIndex = 0;
            if (textWrap === 'overflow') {
                partsByLines.push([specialTextParts]);
            } else {
                for (const part of specialTextParts) {
                    if (part.type === 'text') {
                        lineTextWidth += reportDocument.getTextDimensions(
                            part.text,
                        ).w;
                    } else if (part.type === 'sub' || part.type === 'sup') {
                        reportDocument.setFontSize(fontData.size / 2);
                        lineTextWidth += reportDocument.getTextDimensions(
                            part.text,
                        ).w;
                        reportDocument.setFontSize(fontData.size);
                    }
                    if (lineTextWidth < absoluteLayout.width) {
                        partsByLines[lineIndex].push(part);
                    } else {
                        if (textWrap === 'wrap') {
                            partsByLines.push([part]);
                            lineTextWidth = 0;
                            lineIndex++;
                        } else if (textWrap === 'clip') {
                            break;
                        }
                    }
                }
            }
            let lineHeight = reportDocument.getTextDimensions('Sample Text').h;
            for (const [lineIndex, lineParts] of partsByLines.entries()) {
                let textWidth = 0;
                for (const part of lineParts) {
                    if (part.type === 'text') {
                        textWidth += reportDocument.getTextDimensions(
                            part.text,
                        ).w;
                    } else if (part.type === 'sub' || part.type === 'sup') {
                        reportDocument.setFontSize(fontData.size / 2);
                        textWidth += reportDocument.getTextDimensions(
                            part.text,
                        ).w;
                        reportDocument.setFontSize(fontData.size);
                    }
                }
                const textPosition = {
                    top:
                        absoluteLayout.top +
                        absoluteLayout.height / 2 +
                        lineIndex * lineHeight,
                };
                // eslint-disable-next-line unicorn/prefer-switch
                if (textAlign === 'left') {
                    textPosition.left = absoluteLayout.left;
                } else if (textAlign === 'center') {
                    textPosition.left =
                        absoluteLayout.left +
                        (absoluteLayout.width - textWidth) / 2;
                } else if (textAlign === 'right') {
                    textPosition.left =
                        absoluteLayout.left + absoluteLayout.width - textWidth;
                }
                for (const part of lineParts) {
                    if (part.type === 'text') {
                        this.addColoredTextToDocumentReport(
                            part.text,
                            textPosition,
                            textColorRgba,
                            'left',
                            reportDocument,
                        );
                        const textDimensions = reportDocument.getTextDimensions(
                            part.text,
                        );
                        textPosition.left += textDimensions.w;
                    } else if (part.type === 'sub' || part.type === 'sup') {
                        // TODO: do not hardcode font size ratio
                        const scriptToNormalSizeRatio = 3 / 4;
                        const mainTextTop = textPosition.top;
                        if (part.type === 'sub') {
                            textPosition.top +=
                                lineHeight / 2 -
                                (lineHeight * scriptToNormalSizeRatio) / 3;
                        } else if (part.type === 'sup') {
                            textPosition.top -=
                                lineHeight / 2 -
                                (lineHeight * scriptToNormalSizeRatio) / 3;
                        }
                        reportDocument.setFontSize(
                            fontData.size * scriptToNormalSizeRatio,
                        );
                        this.addColoredTextToDocumentReport(
                            part.text,
                            textPosition,
                            textColorRgba,
                            'left',
                            reportDocument,
                            { textBaseline: 'middle' },
                        );
                        textPosition.top = mainTextTop;
                        const textDimensions = reportDocument.getTextDimensions(
                            part.text,
                        );
                        textPosition.left += textDimensions.w;
                        reportDocument.setFontSize(fontData.size);
                    }
                }
            }
        },

        async addImageElementDataToDocumentReport(
            elementData,
            previewData,
            reportDocument,
            sectionLayoutWithMargins,
            { imageQuality, imageSizeMultiplier } = {},
        ) {
            const element = previewData.elements[elementData.id];
            const absoluteLayout = this.getLayoutWithOffset(
                element.layout,
                sectionLayoutWithMargins,
            );
            const imageSize = {
                width: absoluteLayout.width * imageSizeMultiplier,
                height: absoluteLayout.height * imageSizeMultiplier,
            };
            const imageType = ((imageUrl) => {
                if (imageUrl.endsWith('.jpg') || imageUrl.endsWith('.jpeg')) {
                    return 'image/jpeg';
                } else if (imageUrl.endsWith('.png')) {
                    return 'image/png';
                }
            })(elementData.imageUrl);
            const imageData = await this.getImageData(elementData.imageUrl, {
                size: imageSize,
                type: imageType,
                quality: imageQuality,
            });
            if (imageData !== null) {
                reportDocument.addImage(
                    imageData,
                    absoluteLayout.left,
                    absoluteLayout.top,
                    absoluteLayout.width,
                    absoluteLayout.height,
                );
            }
        },

        getSectionFillColor(sectionData, sectionStyleData) {
            return this.getConditionalColor(
                sectionStyleData.fillColorRules,
                { rowIndex: sectionData.rowIndex },
                sectionStyleData.fillColor,
            );
        },

        async addSectionDataToDocumentReport(
            sectionData,
            // { sectionLayout, elements },
            previewData,
            reportDocument,
        ) {
            // add rectangle
            const section = previewData.sections[sectionData.sectionId];
            const sectionStyleData = section.styleData;
            const fillColor = this.getSectionFillColor(
                sectionData,
                sectionStyleData,
            );
            const fillColorRgba = colors.textToRgb(fillColor);
            const sectionLayoutWithMargins = this.getLayoutWithOffset(
                sectionData.sectionLayout,
                {
                    left: previewData.marginsInPx[3],
                    top: previewData.marginsInPx[0],
                },
            );
            this.addColoredElementToDocumentReport(
                () =>
                    reportDocument.setFillColor(
                        fillColorRgba.r,
                        fillColorRgba.g,
                        fillColorRgba.b,
                    ),
                () =>
                    reportDocument.rect(
                        sectionLayoutWithMargins.left,
                        sectionLayoutWithMargins.top,
                        sectionLayoutWithMargins.width,
                        sectionLayoutWithMargins.height,
                        'F',
                    ),
                reportDocument,
                fillColorRgba.a,
            );
        },

        async addPageDataToDocumentReport(
            page,
            previewData,
            reportDocument,
            { imageQuality, imageSizeMultiplier } = {},
        ) {
            reportDocument.addPage();

            await Promise.all(
                page.map((sectionData) =>
                    this.addSectionDataToDocumentReport(
                        sectionData,
                        previewData,
                        reportDocument,
                    ),
                ),
            );

            // const temporaryPageDocument = new jsPDF({
            //     unit: 'pt',
            //     format: [
            //         previewData.pageSize.width,
            //         previewData.pageSize.height,
            //     ],
            //     compress: true,
            // });
            const fontList = reportDocument.getFontList();
            const defaultFontFamily =
                'helvetica' in fontList ? 'helvetica' : undefined;
            for (const sectionData of page) {
                const sectionLayoutWithMargins = this.getLayoutWithOffset(
                    sectionData.sectionLayout,
                    {
                        left: previewData.marginsInPx[3],
                        top: previewData.marginsInPx[0],
                    },
                );
                for (const elementData of sectionData.elements) {
                    if (elementData.type === 'text' && elementData.html) {
                        const parser = new DOMParser();
                        const elementHtml = parser.parseFromString(
                            elementData.text,
                            'text/html',
                        );
                        const isSimpleHtml = [
                            ...elementHtml.body.children,
                        ].every(
                            (child) =>
                                ['sub', 'sup'].includes(
                                    child.tagName.toLowerCase(),
                                ) && child.children.length === 0,
                        );
                        if (isSimpleHtml) {
                            const specialTextParts = [
                                ...elementHtml.body.childNodes,
                            ].map((child) => ({
                                text: child.textContent,
                                type:
                                    child.nodeType === window.Node.TEXT_NODE
                                        ? 'text'
                                        : child.tagName.toLowerCase(),
                            }));
                            this.addSpecialTextElementDataToDocumentReport(
                                specialTextParts,
                                elementData,
                                previewData,
                                reportDocument,
                                sectionLayoutWithMargins,
                                fontList,
                                defaultFontFamily,
                                sectionData.rowIndex,
                            );
                        } else {
                            let temporaryPageDocument;
                            await this.addHtmlTextElementDataToDocumentReport(
                                elementData,
                                previewData,
                                temporaryPageDocument,
                                sectionLayoutWithMargins,
                            );
                        }
                    }
                }
            }

            // const currentPageNumber =
            //     reportDocument.getCurrentPageInfo().pageNumber;
            // reportDocument.internal.pages[currentPageNumber] = [
            //     ...temporaryPageDocument.internal.pages[1],
            // ];

            for (const sectionData of page) {
                const sectionLayoutWithMargins = this.getLayoutWithOffset(
                    sectionData.sectionLayout,
                    {
                        left: previewData.marginsInPx[3],
                        top: previewData.marginsInPx[0],
                    },
                );
                // TODO: move await out of the loop
                for (const elementData of sectionData.elements) {
                    if (
                        (elementData.type === 'text' && !elementData.html) ||
                        elementData.type === 'page_number'
                    ) {
                        await this.addTextElementDataToDocumentReport(
                            elementData,
                            previewData,
                            reportDocument,
                            sectionLayoutWithMargins,
                            fontList,
                            defaultFontFamily,
                            sectionData.rowIndex,
                        );
                    } else if (elementData.type === 'image') {
                        await this.addImageElementDataToDocumentReport(
                            elementData,
                            previewData,
                            reportDocument,
                            sectionLayoutWithMargins,
                            { imageQuality, imageSizeMultiplier },
                        );
                    }
                }
            }
        },

        async createReport({
            imageQuality = 0.9,
            imageSizeMultiplier = 5,
        } = {}) {
            const previewData = this.getPreviewData();
            // TODO: consider keep the last report in memory
            // const content1 = '<div>S<sub>2</sub></div>'
            // const content2 = '<div>S<sub>2</sub></div>'
            // const doc = new jsPDF("p", "pt", "a4");
            // doc.html(content1, {
            //     callback: function (pdf) {
            //         pdf.addPage("a4", "l");
            //         pdf.html(content1, {
            //         callback: function (pdf2) {
            //             pdf2.addPage("a4", "l");
            //             pdf2.html(content2, {
            //             callback: function (pdf3) {
            //                 pdf3.save("multipage.pdf")
            //             },
            //             x: 0,
            //             y: 2 * previewData.pageSize.height,
            //             });
            //         },
            //         x: 0,
            //         y: previewData.pageSize.height,
            //         });
            //     },
            //     x: 0,
            //     y: 0,
            //     });

            // return doc;

            const reportDocument = new jsPDF({
                orientation:
                    previewData.pageSize.width < previewData.pageSize.height
                        ? 'portrait'
                        : 'landscape',
                unit: 'pt',
                format: [
                    previewData.pageSize.width,
                    previewData.pageSize.height,
                ],
                compress: true,
            });
            reportDocument.deletePage(1);
            // TODO: move await out of the loop
            for (const page of previewData.pages) {
                await this.addPageDataToDocumentReport(
                    page,
                    previewData,
                    reportDocument,
                    { imageQuality, imageSizeMultiplier },
                );
            }
            return reportDocument;
        },

        async downloadReport() {
            const reportDocument = await this.createReport();
            reportDocument.save('report.pdf');
        },

        async getReportDocumentUrl() {
            const reportDocument = await this.createReport();
            return reportDocument.output('bloburl');
        },

        async printReport() {
            const reportDocumentUrl = await this.getReportDocumentUrl();
            // TODO: check if printing is working in firefox
            // TODO: us frame id to prevent duplication and issues,
            // remove any used printFrame from the DOM
            const printFrame = document.createElement('iframe');
            printFrame.style.visibility = 'hidden';
            printFrame.style.height = '0';
            printFrame.style.width = '0';
            printFrame.style.position = 'absolute';
            printFrame.style.border = '0';
            // printFrame.setAttribute(
            //     'style',
            //     'visibility: hidden; height: 0; width: 0; position: absolute; border: 0',
            // );
            document.documentElement.querySelector('body').append(printFrame);
            printFrame.addEventListener('load', (_event) => {
                printFrame.focus();
                // setTimeout(() => {
                //     printFrame.contentWindow.print();
                // }, 1000);
                printFrame.contentWindow.print();
            });
            // printFrame.setAttribute('src', reportDocumentUrl);
            printFrame.src = reportDocumentUrl;
        },

        getTop() {
            if (DEBUG) {
                console.warn('The method getTop is not implemented.');
            }
        },

        getHeaderHeight() {
            if (DEBUG) {
                console.warn('The method getHeaderHeight is not implemented.');
            }
        },

        getPdfReportState() {
            if (DEBUG) {
                console.warn(
                    'The method getPdfReportState is not implemented.',
                );
            }
        },

        getInternalRoundingPrecision() {
            if (DEBUG) {
                console.warn(
                    'The method getInternalRoundingPrecision is not implemented.',
                );
            }
        },
    },
};

export const PdfUtilsMixin = {
    methods: {
        // TODO: better organization for getCSSPixelValue
        // it is used in PdfReportMixin
        getCSSPixelValue(value) {
            const precision = this.getStyleRoundingPrecision();
            if (value === null) {
                return '';
            }
            return `${this.roundTo(value, precision)}px`;
        },

        getStyleRoundingPrecision() {
            if (DEBUG) {
                console.warn(
                    'The method getStyleRoundingPrecision is not implemented.',
                );
            }
        },
    },
};
