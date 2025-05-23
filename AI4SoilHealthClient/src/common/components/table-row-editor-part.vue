<template>
    <div>
        <div class="q-mt-sm row" v-for="col in editColumns" :key="col.name">
            <span v-if="col.type == 'boolean' && !col.invisible" style="width:90%">
                <q-checkbox v-model="row[col.name]" dense :label="col.label"
                    :disable="col.disabled"  :rules="col.rules">
                    <template v-slot:label>
                        <label for="my-checkbox" v-html="col.label"></label>
                    </template>
                </q-checkbox>
            </span>
            <span v-else-if="col.type == 'json' && !col.invisible" style="width:90%">
                <label for="my-jsoneditor" v-html="col.label"></label>
                <json-editor v-model="row[col.name]" :rows="15" iconPicker
                    :disable="col.disabled"  :rules="col.rules"/>
            </span>
            <html-editor v-else-if="col.type == 'html' && !col.invisible" style="width:90%"
                v-model="row[col.name]" :height="col.height ?? '300px'" :showIconPicker="false"
                :label="col.label" :disable="col.disabled" ref="inputRefs"  :rules="col.rules" />
            <autocomplete v-else-if="col.lookup && !col.invisible" v-model="row[col.name]"
                :label="col.label" dense style="width:95%"
                emit-value
                map-options :lookup="col.lookup" @update:model-value="$emit('selectionUpdated', col)"
                :disable="col.disabled" ref="inputRefs" :rules="col.rules" :row="row">
                <template v-slot:label>
                    <label for="my-autocomplete" v-html="col.label"></label>
                </template>
            </autocomplete>
            <q-input class="q-pl-sm" v-else-if="!col.invisible" id="my-input"
                v-model="row[col.name]" dense style="width:95%" :label="col.label"
                :disable="col.disabled" :rules="col.rules"
                :type="inputType(col)" ref="inputRefs">
                <template v-slot:label>
                    <label for="my-input" v-html="col.label" style="font-size: smaller;"></label>
                </template>
                <template v-slot:append v-if="col.password">
                    <q-icon :name="col.passwordShown ? 'visibility_off' : 'visibility'" class="cursor-pointer"
                        @click="col.passwordShown = !col.passwordShown"></q-icon>
                </template>
            </q-input>
            <q-btn v-if="col.url" dense flat icon="open_in_new" @click="openURL(row[col.name])" class="q-pa-none q-ma-none" ></q-btn>
        </div>
    </div>
</template>
<script>

/**
 * Generic row editor component
 * 
 * @component
 * @name TableRowEditorPart
 * @example
 * <TableRowEditorPart />
 */
import { loadComponent } from '@/common/component-loader';
import { TableEditMixin } from '../mixins/table-edit';
export default {
    name: "TableRowEditorPart",
    mixins: [TableEditMixin],
    components: {
        jsonEditor: loadComponent('json-editor'),
        autocomplete: loadComponent('autocomplete'),
        htmlEditor: loadComponent('html-editor')
    },
    props: ['row', 'editColumns'],
    emits: ['selectionUpdated'],
    methods: {

        /**
         * Open the URL in a new tab
         * 
         * @param {String} url - URL to open
         */
        openURL(url) {
            window.open(url, '_blank');
        }
    }   
}
</script>
