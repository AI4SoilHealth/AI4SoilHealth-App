<template>
    <q-dialog v-model="$store.newPopups[name].show" @update:model-value="umv" :persistent="Boolean($store.newPopups[name]?.props?.persistent)" 
        :maximized="$store.newPopups[name].props.maximized || (!$store.isWide && !$store.newPopups[name].props.doNotMaximize)"
        @keydown.f9="translate">
        <q-card flat class="max-width">
            <q-card-section dense class="max-width row items-center text-bold q-pa-sm background">
                <span v-html="title"></span>	
                <q-circular-progress v-if="$store.working" size="24px" indeterminate color="primary" class="nomy q-pl-sm"
                :thickness="0.3" />
                <q-space />
                <q-btn v-for="button in buttons" :key="button.label" dense flat round
                    :label="button.label" :icon="button.icon" no-caps @click="button.action">
                    <q-tooltip>{{ button.tooltip }}</q-tooltip>
                </q-btn>
                <component-settings v-if="isAdmin && path" :path="path" />
                <help-button v-if="help" :name="$t(help)" :titleToShow="titleToShow" />
                <q-btn dense size="sm" flat round icon="close" @click="closeDialog" />
            </q-card-section>
            <q-card-section class="max-width q-pa-none">
                <component v-if="component" :is="component" :parentPopup="this" ref="component" @close="closeDialog" />
            </q-card-section>
        </q-card>
    </q-dialog>
</template>

<script>
/**
 * Popup component for displaying various components.
 * 
 * @component
 * @name Popup
 * @example
 * <Popup />
 */

import { markRaw } from 'vue';
import { loadComponent } from '@/common/component-loader';
import eventBus from '@/common/event-bus';

export default {
    name: 'Popup',
    components: {
        ComponentSettings: loadComponent('component-settings'),
    },
    props: {
        name: {
            type: String,
            default: 'default'
        },
    },
    data() {
        return {
            loadedComponent: null,
            component: null,
            title: null,
            help: null,
            titleToShow: null,
            buttons: null,
            path: null
        }
    },
    async mounted() {
        await this.$nextTick();
        console.log('Popup mounted', this.name, this.$store.newPopups[this.name]);
        if (!this.$store.newPopups[this.name]) return;
        this.component = markRaw(loadComponent(this.$store.newPopups[this.name].component));
        this.title = this.$store.newPopups[this.name].props.title;
        this.help = this.$store.newPopups[this.name].props.help != undefined ? this.$store.newPopups[this.name].props.help : this.$store.newPopups[this.name].component;
        this.help = this.$store.newPopups[this.name].props.help ?? this.title ?? this.$store.newPopups[this.name].component;
        this.titleToShow = this.$store.newPopups[this.name].props.titleToShow != undefined ? this.$t(this.$store.newPopups[this.name].props.titleToShow) : this.$t(this.help);
        this.buttons = this.$store.newPopups[this.name].props.buttons;
        this.path = this.$store.newPopups[this.name].props.path;
        console.log('Popup mounted 1', this.name, this.buttons);
    },
    
    methods: {

        async closeDialog(event) {
            if (!this.$store.newPopups[this.name].canCloseIfFormChanged) {
                if (await this.checkFormChanged()) return;
            } 
            this.$store.formChanged = false;    
            this.$store.newPopups[this.name].show = false;          
            this.$store.popupLevel--;
            eventBus.emit('popupClosed', this.name);
        },

        umv(value) {
            if (!value) this.$store.popupLevel--;
            eventBus.emit('popupClosed', this.name);
        },
    }
}
</script>