<template>
    <div>
        <Header :name="$route.name" :title="$t($route.name)" />
        <div class="row q-pr-sm q-mt-sm">
            <div class= "col"></div>
            <div class="col-auto">
                <q-checkbox class="q-pa-none q-ml-sm" v-model="saveMessages" :label="$t('Save messages')" left-label dense />
                <q-btn flat no-caps dense @click="deleteAllMessages" class="q-ml-sm" icon="delete" color="negative" :label ="$t('Delete all messages')"/>
            </div>
        </div>
        <div id="chat-container">
            <q-scroll-area ref="scroll" :style="{ height: ($q.screen.height - 75 - 110) + 'px' }">
                <div v-for="msg of messages" key="id" class="row">
                    <div class="row q-ml-sm q-mt-sm prompt items-center">
                        <span class="q-pr-sm" v-html="msg.prompt" />
                        <text-to-speech :text="cleanHTML(msg.response)" :customClass="null" size="sm" />
                        <q-btn class="q-pa-none q-ma-none" flat round size="sm" icon="edit"
                            @click="prompt = msg.prompt"></q-btn>
                        <q-btn v-if="msg.id && msg.id != 0" flat round size="sm"
                            icon="delete" @click="deleteMessage(msg.id)"></q-btn>
                    </div>
                    <div class="row response" v-html="msg.response.replaceAll('\\', '')"/> 
                </div>
            </q-scroll-area>
        </div>
        <div class="row q-ml-sm items-center input">
            <div class="col">
                <voice-input type="textarea" :rows="2" v-model="prompt" :label="$t('Type your prompt or press the mic button to speak')" @next="next" @previous="previous" @submit="sendMessage"/>
            </div>
            <div class="col-auto">
                <q-btn flat class="q-ma-sm" round size="sm" icon="send" @click="sendMessage"></q-btn>
            </div>
        </div>
    </div>
</template>

<script>

/**
 * Chat component
 * @component components/chat
 * @description Chat component
 * @example
 *  <Chat />
 */

import { loadComponent } from '@/common/component-loader';

export default {
    name: "Chat",
    components: {
        VoiceInput: loadComponent("voice-input"),
        TextToSpeech: loadComponent("text-to-speech"),
    },
    data() {
        return {
            storable: ["saveMessages"],
            prompt: "",
            models: [],
            messages: [],
            selectedModel: {
                value: 18, label: "learning"
            },
            saveMessages: false,
            toolbarOpen: false,
            i : null
        };
    },
    beforeRouteEnter(to, from, next) {
        next(vm => { vm.init(to.name); });
    },
    beforeRouteUpdate(to, from, next) {
        this.routeKey++;
        this.init(to.name);
        next();
    },
    beforeRouteLeave(to, from, next) {
        this.saveStorable(this, from.name);
        next();
    },

    async mounted() {
    },

    methods: {

        /**
         * Deletes all messages in the chat.
         * 
         * @async
         * @returns {Promise<void>} A promise that resolves when all messages are deleted.
         */
        async deleteAllMessages() {
            if (await this.confirmDialog(this.$t('Are you sure you want to delete all messages?'))) {
                this.post("Chat/DeleteAllMessages");
            }
            this.parent.messages = [];
        },

        async deleteMessage(id) {
            let response = await this.post("Chat/DeleteMessage", { id: id.toString() });
            if (response != null) {
                let index = this.messages.findIndex(m => m.id == id);
                this.messages.splice(index, 1);
            };
            this.i = this.messages.length - 1;
        },

        async init(name) {
            this.loadStorable(this, name);
            this.models = this.$store.catalogs.aiModels; 
            this.messages = await this.get("Chat/GetMessages");
            this.i = this.messages.length - 1;
            this.scrollToBottom();
        },

        scrollToBottom() {
            this.$nextTick(() => {
                this.$refs.scroll.setScrollPosition("vertical", 9999999);
            });
        },

        async sendMessage() {
            if (this.prompt.trim() !== '') {
                let response = await this.post("Chat/SendMessage",
                    {
                        prompt: this.prompt,
                        saveMessages: this.saveMessages.toString(),
                        model: this.selectedModel.label,
                        model_id: this.selectedModel.value.toString()
                    });
                if (response != null) {
                    this.messages.push({
                        id: response.id,
                        prompt: response.prompt,
                        response: response.response
                    });
                    this.prompt = '';
                    this.scrollToBottom();
                };
                this.i = this.messages.length - 1;
            }
        },

        previous() {
            this.prompt = this.messages[this.i].prompt;
            if (this.i > 0) this.i--;
        },

        next() {
            if (this.i < this.messages.length - 1) {
                this.i++;
            }
            this.prompt = this.messages[this.i].prompt;
        }

    }
};
</script>

<style scoped>
#chat-container {
    display: absolute;
    width: 100%;
}

.prompt {
    color: teal;
}

.response {
    margin-left: 20px;
}

.input {
    position: relative;
    bottom: 0px;
    height: 40px;
}
</style>
