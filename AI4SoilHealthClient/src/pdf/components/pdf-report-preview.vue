<template>
    <div>
        <!-- <div class="editing-toolbar">
            <div class="left-item row">
                <div class="editing-toolbar-group row">
                    <q-btn
                        color="primary"
                        dense
                        flat
                        icon="download"
                        @click="downloadReport"
                    >
                        <q-tooltip>{{ $t('Download') }}</q-tooltip>
                    </q-btn>
                </div>
                <div class="editing-toolbar-group row">
                    <q-btn
                        color="primary"
                        dense
                        flat
                        icon="print"
                        @click="printReport"
                    >
                        <q-tooltip>{{ $t('Print') }}</q-tooltip>
                    </q-btn>
                </div>
            </div>
            <q-resize-observer @resize="onEditingToolbarResize" />
        </div> -->
        <q-card ref="editingCard">
            <iframe
                frameborder="0"
                :height="containerHeight"
                :src="reportDocumentUrl"
                :width="containerWidth"
            ></iframe>
        </q-card>
    </div>
</template>

<script>
// import { loadComponent } from '@/common/component-loader';

import {
    PdfReportMixin,
    validatePdfReportState,
    DefaultStyleRoundingPrecision,
    DefaultInternalRoundingPrecision,
    validateRoundingPrecision,
    PdfUtilsMixin,
} from '../mixins/pdf.js';

// const DEBUG = false;
const DEBUG = true;

const propertyValidators = {
    state: validatePdfReportState,
    styleRoundingPrecision: validateRoundingPrecision,
    internalRoundingPrecision: validateRoundingPrecision,
};

export default {
    name: 'PdfReportPreview',
    // components: {
    //     EditingContainer: loadComponent('editing-container'),
    //     PageContainer: loadComponent('page-container'),
    //     InteractableElement: loadComponent('interactable-element'),
    // },
    mixins: [PdfReportMixin, PdfUtilsMixin],
    props: {
        parentPopup: {
            type: Object,
            default: null,
        },
        propState: {
            type: Object,
            default: null,
            validator: propertyValidators.state,
        },
        propStyleRoundingPrecision: {
            type: Number,
            default: DefaultStyleRoundingPrecision,
            validator: propertyValidators.styleRoundingPrecision,
        },
        propInternalRoundingPrecision: {
            type: Number,
            default: DefaultInternalRoundingPrecision,
            validator: propertyValidators.internalRoundingPrecision,
        },
    },
    emits: ['close'],
    data() {
        return {
            state: this.propState,
            styleRoundingPrecision: this.propStyleRoundingPrecision,
            internalRoundingPrecision: this.propInternalRoundingPrecision,
            // TODO: remove this
            // update: maybe it is needed
            // recalculateLayoutNumber: 0,
            // cached
            // sectionLabelLayoutDataAndStyle: {},
            headerHeight: 0,
            reportDocumentUrl: '',
        };
    },
    computed: {
        // containerWidth() {
        //     return this.$q.screen.width - 2 * this.horizontalPadding;
        // },
        // previewData() {
        //     return this.getPreviewData();
        // },
    },
    async mounted() {
        this.initializeComponent(this.parentPopup);
        for (const property of Object.keys(
            this.$store.newPopups[this.parentPopup.name].props,
        )) {
            this.validateProp(property);
        }
        const editingCardContainer =
            this.$refs.editingCard.$el.parentElement.parentElement
                .parentElement;
        this.headerHeight = editingCardContainer.querySelector(
            '.q-card__section.row',
        ).offsetHeight;
        this.reportDocumentUrl = await this.getReportDocumentUrl();
    },
    methods: {
        validateProp(propertyName) {
            if (!(propertyName in propertyValidators)) {
                return;
            }
            if (propertyValidators[propertyName](this[propertyName])) {
                return;
            }
            if (DEBUG) {
                console.error(
                    'Invalid prop: validator check failed ' +
                        `for prop "${propertyName}"`,
                );
            }
        },
        // onEditingToolbarResize(size) {
        //     this.editingToolbarHeight = size.height;
        // },
        // getSectionLabelStyle(sectionId) {
        //     const layoutData = this.state.sections[sectionId].layout;
        //     const oldLayoutData =
        //         this.sectionLabelLayoutDataAndStyle?.[sectionId]?.layoutData;
        //     if (!this.isDataChanged(layoutData, oldLayoutData)) {
        //         return this.sectionLabelLayoutDataAndStyle[sectionId].style;
        //     }
        //     const style = {
        //         top: `${this.state.sections[sectionId].layout.top}px`,
        //         left: `${this.state.sections[sectionId].layout.left}px`,
        //         width: `${this.state.sections[sectionId].layout.width}px`,
        //         height: `${this.state.sections[sectionId].layout.height}px`,
        //     };
        //     this.sectionLabelLayoutDataAndStyle[sectionId] = {
        //         layoutData,
        //         style,
        //     };
        //     return style;
        // },

        //
        // methods from mixins

        getHeaderHeight() {
            return this.headerHeight;
        },

        getPdfReportState() {
            return this.state;
        },

        getStyleRoundingPrecision() {
            return this.styleRoundingPrecision;
        },

        getInternalRoundingPrecision() {
            return this.internalRoundingPrecision;
        },

        
    },
};
</script>

<style scoped>
.editing-toolbar {
    width: 100%;
    z-index: 100;
}

.editing-toolbar-group {
    padding: 0 10px;
}

.zoom-percentage-input {
    width: 50px;
}

.editing-container {
    width: 100%;
    height: 100%;
}

.section {
    position: absolute;
    border-style: dashed;
    border-color: rgb(0 0 0 / 10%);
    border-width: 1px;
}

/* .section-label-container {
    position: absolute;
}

.section-label {
    position: absolute;
    right: 0;
    padding: 4px 6px;
    color: rgba(128 128 128 / 10%);
    font-size: 12px;
} */
</style>
