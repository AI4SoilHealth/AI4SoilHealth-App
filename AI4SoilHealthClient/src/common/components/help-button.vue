<template>
    <div>
        <q-btn class="q-ml-sm" round padding="xs" size="sm" color="primary" @click.stop="onClick" icon="question_mark">
            <q-tooltip>{{ $t("Help") }}</q-tooltip>
        </q-btn>
    </div>
</template>
<script>
export default {
    /**
     * Represents the help button component.
     * 
     * @component
     * @name HelpButton
     * @example
     * <HelpButton :name="name" :titleToShow="titleToShow" />
     */
    name: "HelpButton",
    props: {
        name: String,
        titleToShow: String,
    },
    methods: {

        /**
         * Handles the click event for the help button.
         * @async
         * @returns {Promise<void>}
         */
        async onClick() {
            let name = this.name ?? this.$route.name;
            let response = await this.get("CommonAnon/GetHelp/" + name.replaceAll("/", ""));
            let text;
            if (response && response.help) {
                text = response.help;
            } else {
                text = this.$t("No help yet");
            }
            this.initPopup({ component: "help-dialog", text : text, name : name.replaceAll("/", ""), caption : this.titleToShow });
        },
    },
};
</script>
