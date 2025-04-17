<template>
    <div class="row items-center" :style="{ maxWidth: ($store.screenWidth - 35) + 'px' }">
            <div v-for="source in selectedTitles" class="row">
                <autocomplete class="q-pr-sm q-pb-xs":options="sourceTitles" v-model="source.title" @update:model-value="selectedTitleUpdated(source)"
            style="width:500px; " :label="$t('Catalog')"/>
                <q-input v-model="source.expression" :label="$t('Expression')" style="width:100px;" dense />
                <q-btn flat color="negative" icon="delete" @click="selectedTitles.splice(selectedTitles.indexOf(source), 1)" dense><q-tooltip>{{ $t('Delete row') }}</q-tooltip></q-btn>
            </div>
        <q-btn flat icon="add" @click="selectedTitles.push({title: null, expression: ''})" dense><q-tooltip>{{ $t('Add row') }}</q-tooltip></q-btn>
        <autocomplete :lookup="colorLookup" v-model="color" :label="$t('Color')" style="width:200px"/>
        <q-btn class="q-px-lg" flat icon="visibility" @click="view" dense :label="$t('Show')" no-caps><q-tooltip>{{ $t('Show areas satistying all expressions') }}</q-tooltip></q-btn>
        <span>{{ $t("Opacity") }}</span>
        <q-slider v-model="parent.mapTilerOpacity" label :min="0.0" :max="1." :step="0.05" dense class="q-py-none" />
    </div>
    <div class="row">
        <voice-input class="col auto" v-model="voiceInput" @submit="voiceInputSubmitted" />
    </div>
</template>

<script>
/**
 * Toolbar for maptiler.
 * 
 * @component
 * @name ToolbarDatacubeExpressions
 * @example
 * <ToolbarDatacubeExpressions />
 */

import { loadComponent } from '@/common/component-loader';
import { sourcesFromTileGrid } from 'ol/source';

export default {
    name: "ToolbarDatacubeExpressions",
    components: {
        Autocomplete: loadComponent('autocomplete'),
        VoiceInput: loadComponent("voice-input"),
    },
    props: {
        parent: {}
    },
    data() {
        return {
            sourceTitles: [],
            selectedTitles: [{title: null, expression: ""}],
            color: null,
            voiceInput: "",
            color: 1,
            colorLookup: { refTable : 'general_color' }
        }
    },

    /**
     * Lifecycle hook: Called after the component has been created.
     */
    async mounted() {
        this.sourceTitles = await this.get("Table/GetLookup/data_vw_asset");
        await this.$nextTick();
        await this.loadLookup(this.colorLookup);
        this.color = this.colorLookup.options.find((c) => c.id == 1);
    },

    methods: {

        selectedTitleUpdated(source) {
            source.url = source.title.url;
            source.scale_factor = source.title.scale_factor;
            source.no_data = source.title.no_data;
        },

        scaleNumbersInExpression(expression, scaleFactor) {
            let numbers = expression.match(/\d+(\.\d+)?/g);
            if (numbers) {
                numbers.forEach((number) => {
                    expression = expression.replace(number, number * scaleFactor);
                });
            }
            return expression;
        },

        createMapTilerSource(layers, fromToolbar, color) {
            let urls = {}, expressions = [], index = 0;
            layers.forEach((source) => {
                if (source.expression) {
                    let expression = this.scaleNumbersInExpression(source.expression, source.scale_factor);
                    if (fromToolbar) {
                        urls['b' + index] = source.url;
                        expressions.push("b" + index + "!=" + source.no_data + ", b" + index + expression);
                        index++;
                    } else {
                        urls[source.layer] = source.url;
                        expressions.push(expression);
                    }
                }
            });

            this.parent.mapTilerSource = "https://tilequery.ceres.multione.hr/{z}/{x}/{y}?layers=" + JSON.stringify(urls)
                + "&color=" + color.color_name + "&expression=" + expressions.join(", ");;
            console.log(this.parent.mapTilerSource);
            this.parent.setMapTilerLayer();
        },

        view() {
            if (this.voiceInput > "") {
                this.voiceInputSubmitted();
            } else {

                this.createMapTilerSource(this.selectedTitles, true, this.color);
            }
        },

        async voiceInputSubmitted() {
            let res = await this.post("Chat/SendMessage", { model_id: "26", prompt: this.voiceInput });
            if (res) {
                res = JSON.parse(res.response);
                this.createMapTilerSource(res.layers, false, res.color ?? this.color);
            }
        }
    }
}

</script>
<style scoped>
.q-slider {
    width: 150px;
    margin-left: 20px;
    margin-top: 10px;
}
</style>