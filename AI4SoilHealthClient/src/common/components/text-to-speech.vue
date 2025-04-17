<template>
  <div>
    <q-btn v-if="speaking" dense round flat icon="volume_off" @click="stopSpeaking" :class="customClass" :size="size">
        <q-tooltip>{{$t("Stop speaking")}}</q-tooltip>
    </q-btn>
    <q-btn v-else dense round flat icon="volume_up" @click="speakText" :class="customClass" :size="size">
        <q-tooltip>{{$t("Speak text")}}</q-tooltip>
    </q-btn>
  </div>
</template>

<script>
export default {
    name: "TextToSpeech",
    props: {
        text: {
            type: String,
            default: "",
        },
        customClass: {
            type: String,
            default: 'absolute-bottom-right q-ma-md' // Default position if none provided
        },
        size:
        {
            type: String,
            default: 'md'
        }
    },
    data() {
        return {
            voices: [],
            selectedVoice: null,
            speaking: false,
        };
    },
    mounted() {
        this.getVoices();
        window.speechSynthesis.onvoiceschanged = this.getVoices;
    },
    unmounted() {
        window.speechSynthesis.onvoiceschanged = null;
    },
    methods: {
        getVoices() {
            this.voices = speechSynthesis.getVoices().map(voice => ({
                label: voice.name,
                value: voice,
            }));
            this.selectedVoice = this.voices[0]; // Default to first available voice
        },
        speakText() {
            if ("speechSynthesis" in window && this.selectedVoice) {
                let text = this.cleanHTML(this.text);
                text = text.replaceAll(".\n", ". ").replaceAll("\n", ". "); // Replace newlines with spaces
                console.log("Speaking text:", text);
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.voice = this.selectedVoice.value;
                utterance.onend = () => {
                    this.speaking = false;
                };
                utterance.onstart = () => {
                    this.speaking = true;
                };
                speechSynthesis.speak(utterance);
            } else {
                this.showError("Text-to-Speech is not supported in this browser.");
            }
        },
        stopSpeaking() {
            speechSynthesis.cancel();
            this.speaking = false;
        },
    },
};
</script>
