<template>
    <q-dialog v-model="$store.progress.show" persistent>
        <q-card>
            <q-card-section style="text-align: center; font-weight: bold;">
                <span>{{ title }}</span>
            </q-card-section>
            <div v-if="$store.working">
                <q-card-section class="q-py-none">
                    <q-linear-progress size="25px" :value="percentage" color="positive" style="width:400px"
                        animation-speed="200" />
                </q-card-section>
                <q-card-section class="q-pa-none" style="text-align: center;">
                    <span v-html="progressText"></span>
                </q-card-section>
            </div>
            <q-card-section v-if="message" v-html="message">
            </q-card-section>
            <q-card-actions align="right">
                <q-btn v-if="ws || !taskId" flat color="negative" :label="$t('Abort')" icon="abort" @click="abort" />
                <q-btn v-else flat color="positive" :label="$t('Close')" icon="close" @click="close" />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>
<script>

/**
 * Task progress component
 * 
 * @component
 * @name TaskProgress
 * @example
 * <TaskProgress />
 */
export default {
    data() {
        return {
            ws: null,
            taskId: null,
            min: 0,
            max: 100,
            title: "Progress",
            message: null,
            autoClose: false,
            aborted: false,
            comment: null,
        };
    },
    computed: {
        percentage() {
            return this.$store.progressValue / this.max;
        },
        progressText() {
            return this.$store.progressValue + "/" + this.max + " (" + Math.round(this.percentage * 100) + "%) " + (this.comment ? this.comment : "");
        },
    },

    /**
     * Mounted lifecycle method
     */
    async mounted() {

        this.copyObject(this.$store.progress.props, this, true);
        this.$store.progressValue = this.min;

        if (this.taskId) {
            //await this.$nextTick();
            let socketUrl = this.axios.API.defaults.baseURL.replace("http", "ws");
            socketUrl = socketUrl.replace("/api", "/ws");

            this.ws = new WebSocket(socketUrl + this.taskId);
            let self = this;
            this.ws.onopen = (e) => {
                this.$store.working = true;
            };

            this.ws.onmessage = async (e) => {
                console.log("Received message", e.data);
                let data = JSON.parse(e.data);
                if (data.status == "abort") {
                    this.message = this.$t("Task aborted");
                    self.stop();
                    return;
                } else if (data.status == "progress") {
                    self.$store.progressValue = parseInt(data.message);
                    self.comment = data.comment;
                } else if (data.status == "finish") {
                    if (data.message) {
                        this.message = data.message;
                    } else {
                        this.message = this.$t("Task finished");
                    }
                    self.stop();
                    if (self.reload) {
                        await self.parent.reload();
                    }
                    if (self.autoClose) {
                        self.close();
                    }
                }
            };
            this.ws.onclose = (e) => {
                //this.message = this.$t("Connection closed");
                this.ws = null;
                this.$store.working = false;
            };
        }
    },
    methods: {
        
        /**
         * Close the dialog
         */
        close() {
            this.$emit("finished");
            this.$store.progress.show = false;
        },

        /**
         * Abort the task
         */
        abort() {
            if (this.ws) {
                this.ws.send("abort");
            } else {
                this.aborted = true;
            }
            this.$store.working = false;
        },

        /**
         * Stop the task
         */
        stop() {
            if (this.ws) {
                this.ws.close();
                this.ws = null;
            }
            this.$store.working = false;
        },
    },
};
</script>
