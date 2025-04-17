<template>
    <Header name="About" :title="$t('About')" />
    <q-card>
        <q-card-section class="text-body2">
            <img class="q-ma-none" src="/ai4soilhealth_4f.png" width="150">
            <!-- <div>
                <q-btn class="q-pl-none text-bold" no-caps flat :label="$t('Install application')" text-color="primary" @click="installApp" />
            </div> -->
            <p> </p>
            <p>
                <a href="https://ai4soilhealth.eu/" target="_blank">https://ai4soilhealth.eu/</a> is one of a group of
                Horizon Europe funded projects which fit under the EU’s Soil Health
                Mission for 2030.<br />
                AI4SoilHealth.eu project has received funding from the European Union’s Horizon Europe research an
                innovation programme under grant agreement <a href="https://cordis.europa.eu/project/id/101086179"
                    target="_blank">No. 101086179</a>.
            </p>
            <p>
                Funded by the European Union. Views and opinions expressed are however those of the author(s) only and
                do
                not necessarily reflect those of the European Union or European Research Executive Agency. Neither the
                European Union nor the granting authority can be held responsible for them.
            </p>
            <div>Ver. {{ $store.version }}</div>
            <p></p>
            <div>
                <q-checkbox class="q-pa-none" 
                v-model="pwaInstallSelected"
                :label="$t('Don\'t show installation prompt')" left-label
                dense />
            </div>
            <p></p>
            <div class="row">
                {{ $t("Test compass: ") }}<compass v-model="$store.compass" :allowEdit="false"/>
            </div>
        </q-card-section>
        <q-card-section class="text-body2">
            <p><b>This application uses the following third-party components:</b></p>
            <ul>
                <common-3rd-party-components />
                <specific-3rd-party-components />
            </ul>
            <p><b>Licenses:</b></p>
            <common-3rd-party-licenses />
            <specific-3rd-party-licenses />
        </q-card-section>
        <q-card-section>
            <q-btn class="text-capitalize" v-if="$errors.length > 0"
                @click="$q.dialog({ title: 'Error history', message: $errors.toString() })" flat text-color="Primary"
                label="Error history" />
            {{ this.$q.screen.width.toFixed(0) }} x {{ this.$q.screen.height.toFixed(0) }} {{ this.$q.screen.name }}
        </q-card-section>
    </q-card>
</template>
<script>
/**
 * @component About
 * @description A component that displays information about the project.
 * @example
 * <About />
 */
import { loadComponent } from "@/common/component-loader";
import { StatisticsMixin } from "../../common/mixins/statistics";
export default {
    name: "About",
    mixins: [StatisticsMixin],
    components: {
        Compass: loadComponent("compass"),
        Common3rdPartyComponents: loadComponent("common-3rd-party-components"),
        Common3rdPartyLicenses: loadComponent("common-3rd-party-licenses"),
        Specific3rdPartyComponents: loadComponent("specific-3rd-party-components"),
        Specific3rdPartyLicenses: loadComponent("specific-3rd-party-licenses")
    },
    data() {
        return {
            alphaBuffer: [],
            pwaInstallSelected: false, 
        };
    },
    mounted() {
        this.$store.compass = 30;
        window.addEventListener('deviceorientationabsolute', this.getCompass);
        //  Inicijalizacija stanja iz localStorage pri učitavanju komponenteđ
        // alert(this.$q.localStorage.getItem('pwa-install-selected'));
        this.pwaInstallSelected = this.$q.localStorage.getItem('pwa-install-selected');
    },
    unmounted() {
        window.removeEventListener('deviceorientationabsolute', this.getCompass);

    },
    methods: {
        installApp() {
            this.$store.pwaComponent.triggerPrompt();
        },
        /**
         * Retrieves the compass value
         *
         * @param {Event} event - The event object.
         * @returns {Compass} The compass value.
         */
         getCompass(event) {      
            let alpha = event.alpha;
            if (alpha == null) {
                this.$store.compass = null;
            } else if (event.absolute) {
                alpha = Math.abs(alpha - 360)
                this.$store.compass = Math.round(this.movingAverage(this.alphaBuffer, alpha, 20));
            }
        },
         // Metoda za ažuriranje vrijednosti u localStorage i emitiranje promjene
        updatePwaInstallSelected(newValue) {
            this.$q.localStorage.setItem('pwa-install-selected', newValue);
            // this.$emit('update:pwa-install-selected', newValue);
        },
    },
    watch: {
        // Ažuriraj localStorage kad se stanje checkboxa promijeni
        pwaInstallSelected(newValue) {
            this.$q.localStorage.setItem('pwa-install-selected', newValue);
        },
  },
}
</script>