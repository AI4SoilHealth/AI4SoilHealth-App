<template>
    <div class="row q-mr-none items-center">
        <q-input class="col"
            dense
            :label="internalLabel"
            :type="type"
            :rows="rows"
            id="voiceInput"
            v-model="text"
            clearable
            @input="$emit('update:modelValue', $event.target.value)"
            @keydown.enter.prevent.stop="$emit('submit')"
            @keydown.up.prevent="$emit('previous')"
            @keydown.down.prevent="$emit('next')"
        />
        <q-btn class="col-auto q-mr-none" round flat :icon="isListening ? 'mic_off' : 'mic'" @click="toggleListening"/>
    </div>
</template>

<script>
export default {
    name: "VoiceInput",
    props: {
        label: {
            type: String,
            default: "Voice input",
        },
        type: {
            type: String,
            default: "text",
        },
        rows: {
            type: Number,
            default: 1,
        },
        modelValue: {
            type: String,
            default: "",
        },
    },

    emits: ["update:modelValue", "submit", "previous", "next"],
    computed: {
        text: {
            get() {
                return this.modelValue; // Reflect the parent value
            },
            set(value) {
                this.$emit('update:modelValue', value); // Emit changes to the parent
            },
        },
    },
    data() {
        return {
            //text: "", // Bound to the input field
            isListening: false, // Indicates if voice recognition is active
            recognition: null, // Holds the SpeechRecognition instance
            internalLabel: this.label,
        };
    },

    methods: {

        toggleListening() {
            if (this.isListening) {
                this.stopListening();

            } else {
                this.startListening();
            }
        },
        
        startListening() {
            this.text = ""; // Clear the input field
            this.internalLabel = this.$t("Listening..."); // Change the label
        // Check if SpeechRecognition is supported
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

            if (!SpeechRecognition) {
                this.showError("Your browser does not support Speech Recognition.");
                return;
            }

            // Create a new instance of SpeechRecognition if not already created
            if (!this.recognition) {
                this.recognition = new SpeechRecognition();
                this.recognition.lang = "en-US"; // Set language (change as needed)
                this.recognition.interimResults = false; // Use interim / final results
                this.recognition.continuous = true; // Keep listening

                // Handle results
                this.recognition.onresult = (event) => {
                    for (let i = event.resultIndex; i < event.results.length; i++) {
                        if (event.results[i].isFinal) {
                            let transcript = event.results[i][0].transcript;
                            if (transcript.trim() == "submit") {
                                this.stopListening();
                                this.$emit("submit");
                                return;
                            } else if (transcript.trim() == "clear") {
                                this.text = "";
                            } else if (transcript.trim() == "stop") {
                                this.stopListening();
                                return;
                            } else if (transcript.trim() == "cancel") {
                                this.stopListening();
                                this.text = "";
                                return;
                            } else {
                                this.text += event.results[i][0].transcript;
                            }
                        }
                    }
                    //const transcript = event.results[0][0].transcript;
                    //this.text += transcript;
                };

                // Handle end of recognition
                this.recognition.onend = () => {
                    this.isListening = false;
                };

                // Handle errors
                this.recognition.onerror = (event) => {
                    this.isListening = false;
                };
            }

            // Start recognition
            this.isListening = true;
            this.recognition.start();
        },

        stopListening() {
            if (this.recognition) {
                this.recognition.stop();
            }
            this.internalLabel = this.label; // Reset the label
            this.isListening = false;
        },
    },
};
</script>

