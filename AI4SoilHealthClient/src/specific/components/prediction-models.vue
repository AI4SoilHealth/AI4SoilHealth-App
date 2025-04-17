<template>
    <Header ref="header" :back-button="backButton" :name="$route.name" :title="title ?? $t($route.name)" />
    <div :style="containerStyle">
        <div class="prediction-models-toolbar">
            <multi-select-container v-model="selectedAiModel"
                class="left-item row __clear_button__specifier_for_unscoped_styles__"
                clear-button-container-class="row clear-button" clearable :options="aiModels.options">
                <multi-select-element dense :label="aiModels.methods.label" option-key="method"
                    :style="{ width: aiModels.methods.width ?? '100px' }" />
                <multi-select-element dense :label="aiModels.indicators.label" option-key="indicator_display_name"
                    :style="{ width: aiModels.indicators.width ?? '100px' }" />
            </multi-select-container>
        </div>
        <Table ref="table" :detailTable="true" :options="tableOptions" />
    </div>
</template>

<script>

import { loadComponent } from '@/common/component-loader';

// const DEBUG = false;
const DEBUG = true;

const propValidators = {
    title(value) {
        return typeof value === 'string';
    },
    backButton(value) {
        return typeof value === 'boolean';
    },
    rows(value) {
        if (!Array.isArray(value)) {
            return false;
        }
        return value.every((item) => typeof item === 'object');
    },
    top(value) {
        if (typeof value !== 'number') {
            return false;
        }
        return Number.isFinite(value);
    },
    contextValues(value) {
        if (!Array.isArray(value)) {
            return false;
        }
        return value.every((item) => typeof item === 'object');
    },
};

export default {
    name: 'PredictionModels',
    components: {
        MultiSelectContainer: loadComponent('multi-select-container'),
        MultiSelectElement: loadComponent('multi-select-element'),
        // eslint-disable-next-line vue/no-reserved-component-names
        Table: loadComponent('table'),
    },
    props: {
        propTitle: {
            type: String,
            default: null,
            validator: propValidators.title,
        },
        propBackButton: {
            type: Boolean,
            default: false,
            validator: propValidators.backButton,
        },
        propRows: {
            type: Array,
            default: () => [],
            validator: propValidators.rows,
        },
        propTop: {
            type: Number,
            default: 0,
            validator: propValidators.top,
        },
    },
    data() {
        return {
            title: this.propTitle,
            backButton: this.propBackButton,
            top: this.propTop,
            headerHeight: 0,
            // TODO: Add table in database for prediction models
            // and use contextValues as prop to load options
            contextValues: {},
            contextValuesLocal: {},
            aiModels: {
                // TODO: adjust name of lookup table
                lookup: 'zzgll.data_ai_models_l',
                methods: {
                    label: 'AI model',
                    width: '250px',
                },
                indicators: {
                    label: 'Indicator',
                    width: '200px',
                },
                options: [],
            },
            selectedAiModel: null,
            // tableProperties: {
            //     details: null,
            //     standardEditing: false,
            // }
            rows: this.propRows,
            tableOptions: null,
        };
    },
    computed: {
        containerHeight() {
            return this.$q.screen.height - this.top - this.headerHeight;
        },
        containerStyle() {
            return {
                height: `${this.containerHeight}px`,
            };
        },
    },
    watch: {
        '$route.query.timestamp': {
            handler(val) {
                this.init();
            },
            immediate: true
        },
        async selectedAiModel(value) {
            console.log('selectedAiModel', value);
            if (value) {
                const spectralValues = [];
                const rowIndexesToSpectralValuesIndexes = [];
                for (const row of this.rows) {
                    if (row.values !== null) {
                        spectralValues.push(Array.from(row.values));
                    }
                    rowIndexesToSpectralValuesIndexes.push(
                        spectralValues.length - 1
                    );
                }
                const predictions = await this.post(
                    'Predictions/GetPredictedValues/' + value.id,
                    { features: spectralValues }
                );
                await this.setPredictions(
                    predictions,
                    value,
                    rowIndexesToSpectralValuesIndexes
                );
                console.log('a');
            } else {
                await this.resetPredictions();
            }
        },
    },
    mounted() {
        console.log('PredictionModels mounted');
        this.headerHeight = this.$refs.header.$el.offsetHeight;
    },
    methods: {
        validateProp(propName) {
            if (!(propName in propValidators)) {
                return;
            }
            if (propValidators[propName](this[propName])) {
                return;
            }
            if (DEBUG) {
                console.error(
                    `Invalid prop: validator check failed for prop "${propName}"`
                );
            }
            return;
        },
        async init() {          
            this.initializeComponent(this.parentPopup);
            Object.keys(this.$store.props[this.$route.name]).forEach((prop) => {
                this.validateProp(prop);
            });
            this.copyObject({ data: this.rows, parent: true }, this.tableOptions, true);
            // this.aiModels.options = await this.get(
            //     'Table/GetParamLookup/' + this.aiModels.lookup
            // );
            this.aiModels.options = await this.get(
                'Predictions/GetModelsForDataSource/',
                { dataSourceId: 0 }
            );
            console.log('PredictionModels init');
        },
        async setPredictions(
            predictions,
            aiModel,
            rowIndexesToSpectralValuesIndexes
        ) {
            const featuresColumnName = aiModel.indicator_name;
            const featuresColumnIndex = Object.entries(this.rows[0]).findIndex(
                ([key, _value]) => key === featuresColumnName
            );
            const predictionsColumnName = `predicted_${featuresColumnName}`;
            const predictionsColumnIndex = featuresColumnIndex > 0 ? featuresColumnIndex + 1 : featuresColumnIndex;
            this.$refs.table.data = this.rows.map((row, index) => {
                if (row.values === null) {
                    return row;
                }
                const rowEntries = Object.entries(row);
                rowEntries.splice(predictionsColumnIndex, 0, [
                    predictionsColumnName,
                    predictions[rowIndexesToSpectralValuesIndexes[index]],
                ]);
                return Object.fromEntries(rowEntries);
            });
            for (let columnName of [featuresColumnName, predictionsColumnName]) {
                if (!(columnName in this.$refs.table.colAtts)) {
                    this.$refs.table.colAtts[columnName] = {};
                }
                this.copyObject(
                    { order_no: 0 },
                    this.$refs.table.colAtts[columnName],
                    true,
                );
            }
            await this.$refs.table.reload();
        },
        async resetPredictions() {
            this.$refs.table.data = this.rows;
            await this.$refs.table.reload();
        },
    },
};
</script>

<style scoped>
.prediction-models-toolbar {
    width: 100%;
    z-index: 100;
}
</style>

<style>
.__clear_button__specifier_for_unscoped_styles__ .clear-button {
    padding: 0px 10px;
}
</style>
