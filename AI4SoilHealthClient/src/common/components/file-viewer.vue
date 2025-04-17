<template>
    <iframe id="iframe" title="Dokument" :style="iframeStyle" :src="document"></iframe>
</template>

<script>
/**
 * File viewer component
 * 
 * @component
 * @name FileViewer
 * @example
 * <FileViewer />
 */
export default {
    name: "FileViewer",
    props: ["parentPopup"],
    data: function () {
        return {
            document: null,
            API: null

        };
    },
    computed: {
        iframeStyle() {
            return {
                width: "100%",
                height: (this.$q.screen.height - 46) + "px",
            }
        }
    },
    /**
     * Mounted lifecycle method
     */
    async mounted() {
        this.initializeComponent(this.parentPopup); 
        this.document = await this.getDocument();
    },
    methods: {
        /**
         * Retrieves the document
         */
        async getDocument() {
            //let response = await this.api(this.axios.API.get, this.API, { responseType: "blob" });
            this.$store.working = true;
            let response = await this.axios.API.get(this.API, { responseType: "blob" });
            if (response && response.data) {
                let ret = window.URL.createObjectURL(new Blob([response.data], { type: "application/pdf" }));
                this.$store.working = false;
                return ret;
            } else {
                this.$store.working = false;
                return null;
            }
        },
    }
}
</script>