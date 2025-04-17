<template>
  <div>
  <!-- <Header :title="title" /> -->
  <div>
    <q-uploader style="width:100%; min-width: 400px;" ref="uploader" multiple batch :label="$t('Select or drop files')"
      @uploaded="uploaded" @failed="failed" @rejected="rejected" :url="uploadURL" :accept="accept ?? '*/*'"
      :form-fields="formFields" :headers="[{ name: 'Authorization', value: 'Bearer ' + this.$keycloak.token }]" />
    <div v-if="error" class="text-negative text-bold q-pa-sm">{{ error }}</div>
    <q-btn v-if="$refs.uploader && $refs.uploader.files.length > 0" class="q-pa-sm" flat no-caps dense icon="upload" :label="$t('Upload')" @click="upload" />
  </div>
  </div>
</template>

<script>

/**
 * Vue component for file uploader. Not used in the current version of the application.
 * @component
 * @name FileUploader
 * @example
 * <FileUploader />
 */

export default {
  name: "FileUploader",
  props: ["parentPopup"],
  data() {
    return {
      accept: null,
      uploadURL: null,
      error: null,
      title: this.$t("Upload file"),
      saveLast: false,
    };
  },

  async mounted() {
    this.initializeComponent(this.parentPopup);
    this.uploadURL = this.axios.API.defaults.baseURL + this.uploadURL;
    await this.$nextTick();
    if (this.saveLast && this.$store.lastFileUploaderFiles) {
      this.$refs.uploader.addFiles(this.$store.lastFileUploaderFiles);
    }
  },

  beforeUnmount() {
    if (this.saveLast) this.$store.lastFileUploaderFiles = this.$refs.uploader.files;
  },

  methods: {

    formFields(files) {
      const a = [{ name: "params", value: JSON.stringify(this.params) }];
      return a;
    },

    upload() {
      this.error = null;
      this.$refs.uploader.upload();
    },

    async uploaded(info) {
      if (this.parent && this.parent.reload) {
        await this.parent.reload();
      }
      if (this.afterUpload) {
        this.afterUpload.data = JSON.parse(info.xhr.response);
        this.invokeComponent(this.afterUpload);
      }
      if (this.parentPopup) this.closePopup();
    },

    failed(info) {
      console.log("failed", info);
      if (info.xhr.statusText == "" && info.xhr.responseText == "") {
        this.error = this.$t("Upload failed. Is file open in another application? Sending too many bytes?");
      } else {
        this.error = info.xhr.statusText + ": " + info.xhr.responseText;
      }
    },

    rejected(info) {
      console.log("rejected", info);
      if (info.xhr.statusText == "" && info.xhr.responseText == "") {
        this.error = this.$t("Upload failed. Is the file open in another application?");
      } else {
        this.error = info.xhr.statusText + ": " + info.xhr.responseText;
      }
    }

  }
};
</script>
