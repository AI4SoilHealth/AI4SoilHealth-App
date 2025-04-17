<template>
    <Table v-if="showStatusTable" :detailTable="true" :options="statusTableOptions" @close="close">
    </Table>
    <Table v-if="showErrorsTable" :detailTable="true" :options="errorsTableOptions" @close="close">
    </Table>
</template>

<script>
/**
 * Json upload results component
 * 
 * @component
 * @name JsonUploadResults
 * @example
 * <JsonUploadResults />
 */
import { loadComponent } from '@/common/component-loader';

export default {
    name: 'JsonUploadResults',
    components: {
        Table: loadComponent('table'),
    },
    props: {
        parentPopup: {
            type: Object,
            default: null,
        },
    },
    data() {
        return {
            data: {},
            statusTableOptions: null,
            errorsTableOptions: null,
            showStatusTable: false,
            showErrorsTable: false,
        };
    },
    async mounted() {
        this.initializeComponent(this.parentPopup);        
        if (this.data.error) {
            await this.showError(this.data.error);
            await this.parentPopup.closeDialog();
        } else if (this.data.json_errors) {
            const uploadStatuses = [];
            for (const fileName in this.data.json_errors) {
                const uploadSuccess = this.data.json_errors[fileName].length === 0 ? 'Completed' : 'Failed';
                uploadStatuses.push({
                    file_name: fileName,
                    upload_status: uploadSuccess,
                    errors: this.data.json_errors[fileName],
                });
            }
            if (uploadStatuses.every(statusInfo => statusInfo.upload_status === 'Completed')) {
                await this.showMessage(this.$t("All json files uploaded successfully."));
                await this.parentPopup.closeDialog();
            } else if (uploadStatuses.length === 1) {
                this.errorsTableOptions.data = uploadStatuses[0].errors;
                this.parentPopup.title = this.errorsTableOptions.title;
                if (this.errorsTableOptions.help) {
                    this.parentPopup.help = this.errorsTableOptions.help;
                }
                if (this.errorsTableOptions.titleToShow) {
                    this.parentPopup.titleToShow = this.errorsTableOptions.titleToShow;
                }
                this.showErrorsTable = true;
            } else {
                this.statusTableOptions.data = uploadStatuses;
                this.parentPopup.title = this.statusTableOptions.title;
                if (this.statusTableOptions.help) {
                    this.parentPopup.help = this.statusTableOptions.help;
                }
                if (this.statusTableOptions.titleToShow) {
                    this.parentPopup.titleToShow = this.statusTableOptions.titleToShow;
                }
                this.showStatusTable = true;
            }
        }
        
        
    },
    methods: {
        close() {
            this.closePopup();
        },
        showErrorDialog() {},
        hideErrorDialog() {}
    }
}
</script>
<style scoped>

</style>