<template>
    <q-card class="max-width" v-if="edit">
        <html-editor v-model="helpTextEdit" saveCancel @save="saveHelp" @cancel="stopEdit"/>
    </q-card>
    <q-card v-else class="max-width">
        <q-card-section>
            <text-to-speech :text="text"/>
            <div class="text-body2" v-html="replaceIcons(text)"></div>
        </q-card-section>
    </q-card>
</template>
<script>

/**
 * Represents the help dialog component.
 * 
 * @component
 * @name HelpButton
 * @example
 * <HelpDialog />
 */
import { loadComponent } from '@/common/component-loader';

export default {
    name: "HelpDialog",
    components: {
        HtmlEditor: loadComponent('html-editor'),
        TextToSpeech: loadComponent('text-to-speech'),
    },
    props: ["parentPopup"],
    data: () => ({
        helpTextEdit: "",
        caption: null,
        edit: false,
        text: "",
    }),

    mounted() {
        this.initializeComponent(this.parentPopup);
        this.parentPopup.title = this.caption;
        this.parentPopup.help = null;
        this.setButtons();
        window.showSubHelp = this.showSubHelp;
    },

    methods: {

        setButtons() {
            if (this.isAdmin) this.parentPopup.buttons = [{ icon: "edit", tooltip: this.$t("Edit help"), action: this.startEdit }];
        },

        startEdit() {
            this.helpTextEdit = this.text; 
            this.parentPopup.buttons = [];
            this.edit = true;
        },

        stopEdit(event) {
            console.log("stopEdit");
            this.setButtons();
            this.edit = false;
        },

        /**
         * Saves the help information.
         *
         * @param {Object} help - The help information to be saved.
         * @returns {Promise} - A promise that resolves when the help information is saved successfully.
         */
        async saveHelp() {
            await this.put("CommonUser/SetHelp/" + this.name, { help: this.helpTextEdit });
            this.text = this.helpTextEdit;
            this.setButtons();
            this.edit = false;
        },

        async showSubHelp(name) {
            // example (embed in help): <a onclick="showSubHelp('OtherHelp')">Some other help</a>
            // hyperlink is substituted with the other help text
            // do not edit after the substitution
            let response = await this.get("CommonAnon/GetHelp/" + name);
            if (response && response.help) {
                response.help = this.replaceIcons(response.help);
                const reg = new RegExp(`<a .*?${name}.*?a>`);
                this.text = this.text.replace(reg, response.help);
            }
        },
    },
};
</script>
