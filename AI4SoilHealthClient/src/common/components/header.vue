<template>
    <div :class="{ 'header-container': true, 'background': !$q.dark.isActive }" :style="headerStyle">
        <div class="text-subtitle1 text-bold q-pa-xs q-ma-none left">
            <q-btn dense v-if="backButton" flat icon="arrow_back" @click="goBack">
                <q-tooltip>{{ $t("Back") }}</q-tooltip>
            </q-btn>
            <span v-html="titleToShow"></span>
            <q-circular-progress v-if="$store.working" size="24px" indeterminate color="primary" class="nomy q-pl-sm"
                :thickness="0.3" />
        </div>
        
        <div class="q-pa-xs q-ma-none right row">
            <q-btn v-if="showDownloadButton" dense flat icon="download" @click="$emit('download')">
                <q-tooltip>{{ $t("Download") }}</q-tooltip>
            </q-btn>
            <component-settings v-if="isAdmin" :path="$route.path" />
            <help-button v-if="showHelpButton" :options="null" :name="nameForHelp" :titleToShow="titleForHelp" />
            <q-btn v-if="showCloseButton" dense flat icon="close" @click="$emit('close')" >
                <q-tooltip>{{ $t("Close") }}</q-tooltip>
            </q-btn>
        </div>
    </div>
</template>
<script>
/**
 * Represents the header component for the application.
 * 
 * @component
 * @name Header
 * @example
 * <Header />
 */
import { loadComponent } from '@/common/component-loader';

export default {
    name: "Header",
    components: {

        ComponentSettings: loadComponent('component-settings'),
    },
    props: {
        name: { type: String, default: null },
        title: { type: String, default: null },
        backButton: { type: Boolean, default: false },
        showHelpButton: { type: Boolean, default: true },
        showCloseButton: { type: Boolean, default: false },
        showDownloadButton: { type: Boolean, default: false },
        help: { type: String, default: null },
        fullWidth: { type: Boolean, default: false },
    },
    emits: ['download', 'close'],
    data() {
        return {
        };
    },
    computed: {
        titleForHelp() {
            return this.help ?? this.titleToShow;
        },
        titleToShow() {
            return this.title ?? this.$route.meta.title ?? this.$route.name;
        },
        nameForHelp() {
            return this.help ?? this.name ?? this.$route.path;
        },
        headerStyle() {
            return {
                maxWidth: this.fullWidth ? '100%' : this.$store.screenWidth + 'px',
            };
        },
    },
    methods: {
        /**
         * Navigates back to the previous page.
         */
        async goBack() {
            if (await this.checkFormChanged()) return;
            this.$store.state[this.$route.path] = null;
            this.$store.level--;
            this.$router.go(-1);
        },
    },
};
</script>
<style scoped>
.header-container {
    display: flex;
    justify-content: space-between;
}
</style>