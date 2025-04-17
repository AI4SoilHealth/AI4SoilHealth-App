<template>
<q-card class="q-pt-none">
    <q-card-actions align="right" class="q-py-none">
        <q-btn v-if="$store.formChanged" flat color="positive" :label="$t('Save')" @click="save" />
        <q-btn flat color="negative" :label="$store.formChanged ? $t('Cancel') : $t('Close')" @click="cancel" />
    </q-card-actions>
    <q-card-section class="q-py-none">
        
            <q-input dense outlined v-model="o.name" :label="$t('Location name')" style="width:300px"/>
            <div class = "row">
                <q-btn v-if="hasFeatureStatsButton" icon="bar_chart" flat dense @click="parent.showFeatureStats(false)" :label="$t('Statistics')" no-caps>
                    <q-tooltip>{{ $t("Show statistics for this feature") }}</q-tooltip>
                </q-btn>  
                <q-btn v-if="hasFeatureStatsButton" icon="compare" flat dense @click="parent.showFeatureStats(true)" :label="$t('Compare')" no-caps>
                    <q-tooltip>{{ $t("Compare statistics for this feature with containing geometries") }}</q-tooltip>
                </q-btn>  
                <q-btn v-if="hasHistoryButton" icon="show_chart" flat dense @click="parent.showHistory()" :label="$t('History')" no-caps>
                    <q-tooltip>{{ $t("Show history for this feature") }}</q-tooltip>
                </q-btn>  
            </div>
             
        <hr class="hr2">

        <div class="text-weight-bold">{{ $t("Observations") }}</div>
        
        <q-tabs dense align="left" no-caps outside-arrows v-model="activeTab" indicator-color="positive">
            <q-tab dense v-for="so of o.simple_observations" :key="so.id" :name="tabName(so.id)"  :disable="$store.formChanged">
                <div class="row items-center no-wrap q-gutter-sm">
                    <span v-html="(so.indicator ?? '') + '<br/>' + (so.id ? so.date : 'New')" style="max-width:90px; white-space: normal; word-wrap: break-word;"/>
                </div>
            </q-tab>    
        </q-tabs>

        <q-tab-panels v-model="activeTab">
            <q-tab-panel v-for="so of o.simple_observations" :key="so.id" :name="tabName(so.id)">
                <q-form :ref="'qform' + tabName(so.id)">
                    <div class="row justify-between">
                    <q-input :rules="$store.rules.required" class="q-pb-xs" type="date" dense outlined v-model="so.date" :label="$t('Date')" /> 
                    <q-btn :disable="$store.formChanged" dense flat v-if="so.id" icon="delete" color="negative" @click="deleteObservation(so.id)" :label="$t('Delete this observation')" no-caps/>
                    </div>
                    <autocomplete :rules="$store.rules.required" class="q-pb-xs" v-model="so.indicator_id" :label="$t('Type')" map-options emit-value :lookup="simple_observation_lookup" :clearable="false" @update:model-value="selectedTypeChanged" hide-bottom-space/>
                    <!-- <q-input :rules="$store.rules.required" class="q-pb-xs" dense outlined v-model="so.name" :label="$t('Name')" /> -->
                    <div class="row" v-if="selectedType.numerical" >
                        <q-input type="number" class="q-pb-xs" dense outlined v-model="so.value" :label="$t('Value')" style="width:150px" :mask="mask" reverse-fill-mask :rules="[...rule, ...$store.rules.required]" hide-bottom-space/>
                        <span class="q-ml-sm">{{ selectedType.unit }}</span>
                    </div>
                    <q-input class="q-pb-xs" type="textarea" dense outlined v-model="so.comment" :label="$t('Comment')" rows="2"/>
                </q-form>

                <hr class="hr2">
                <div  class="text-weight-bold">
                {{ $t("Images") }}
                </div>
                <div>
                <span class="q-ml-sm">{{ $t("Use ") }}</span>
                    <q-radio v-model="coordType" val="gps" :label="$t('GPS')"><q-tooltip>{{ $t("Set image position to GPS coordinates") }}</q-tooltip></q-radio>
                    <q-radio v-model="coordType" val="loc" :label="$t('Location')"><q-tooltip>{{ $t("Set image position to location coordinates") }}</q-tooltip></q-radio>
                    <q-btn class="q-ml-lg" flat color="positive" icon="photo_camera" @click="addImage" :label="$t('Add image')" no-caps>
                        <q-tooltip>{{ $t("Select or take a new image") }}</q-tooltip>
                    </q-btn>
                </div>
                <div v-if="so.images.length == 0">{{ $t("No images") }}</div>
                <q-markup-table class="q-pa-none q-ma-none" dense flat v-if="so.images.length > 0">
                    <tbody>
                        <template v-for="(f, index) of so.images" :key="f.id">
                            <tr>
                                <td class="text-left" width="60px">
                                    <q-btn   :disable="$store.formChanged" class="q-py-none q-pr-sm" dense flat icon="edit" @click="editImage(f)">
                                        <q-tooltip>{{ $t("Edit properties") }}</q-tooltip>
                                    </q-btn>
                                    <q-btn   :disable="$store.formChanged" class="q-py-none q-pr-sm" dense flat icon="visibility" @click="showImage(index)">
                                        <q-tooltip>{{ $t("Show image") }}</q-tooltip>
                                    </q-btn>
                                    <q-btn   :disable="$store.formChanged" class="q-py-none q-pr-sm" dense flat color="negative" @click="deleteImage(f)"
                                        icon="delete">
                                        <q-tooltip>{{ $t("Delete image") }}</q-tooltip>
                                    </q-btn></td>
                                <td @click="showImage(index)" class="q-py-none text-left"
                                    style="max-width:300px; overflow: hidden; cursor: pointer;">
                                    {{ f.name }}</td>
                            </tr>
                        </template> 
                    </tbody>
                </q-markup-table>
            </q-tab-panel>
        </q-tab-panels>
    </q-card-section>
    <q-uploader v-show="false" ref="uploader" :url="uploadURL" accept="image/*,text/plain" :form-fields="formFields"  :upload-factory="customUploadFactory" 
    @uploaded="filesUploaded" @failed="filesFailed" @added="filesAdded" :headers="[{ name: 'Authorization', value: 'Bearer ' + this.$keycloak.token }]"
    />
    <q-dialog v-model="editingImage" persistent style="z-index: 9999;">
        <q-card>
            <q-card-section>
        <q-input v-model="editedImage.name" dense :label="$t('Name')" style="width:300px;" />
        <div class="row">
            <q-input v-model="editedImage.compass" dense :label="$t('Compass')" style="width:100px;" />
            <q-input v-model="editedImage.lon" dense :label="$t('Longitude')" style="width:120px;" />
            <q-input v-model="editedImage.lat" dense :label="$t('Latitude')" style="width:120px;" />
        </div>
        <div class="row">

            <q-btn no-caps flat :icon="$icons.my_location" @click="setGPSPosition"
            :label="$t('Set GPS position')"></q-btn>
            <q-btn no-caps flat icon="location_on" @click="setLocationPosition"
                :label="$t('Set location position')"></q-btn>
        </div>
            </q-card-section>
            <q-card-actions align="right">
        <q-btn v-if="imageChanged" flat color="positive" @click="saveImage" :label="$t('Save')"></q-btn>
        <q-btn flat color="negative" @click="cancelImage" :label="imageChanged ? $t('Cancel') : $t('Close')"></q-btn>
            </q-card-actions>
    </q-card>
    </q-dialog>
</q-card>
</template>

<script>
/**
 * SimpleObservation component
 * @component components/simple-observation
 * @description SimpleObservation component
 * @example
 *  <SimpleObservation />
 */
import { loadComponent } from '@/common/component-loader';
import { SimpleObservationMixin } from '../mixins/simple-observation';
//import eventBus from '@/common/event-bus';

export default {
    name: "SimpleObservation",
    mixins: [SimpleObservationMixin],
    components: {
        autocomplete: loadComponent('autocomplete'),
    },
    props: ["parentPopup"],
    data() {
        return {
            simple_observation_lookup: {
                refTable: 'catalog_simple_observation'
            },
            o: {},
            oSav: {},
            activeSO: {},
            activeTab: null,
            editIndex: false,
            editedImage: null,
            imageSav: {},
            editingImage: false,
            coordType: "gps",
            selectedType: {},
            mask: null,
            rule: null,
            parent: null
        };
    },

    computed: {
        uploadURL: function () {
            // return this.axios.API.defaults.baseURL + 'User/UploadImage/' + this.activeSO.id;
            return 'User/UploadImage/' + this.activeSO.id;
        },
        imageChanged: function () {
            return !this.equalObjects(this.editedImage, this.imageSav);
        },
        /**
        * Checks should we display feature stats button
        * 
        * @returns {boolean} True if the feature has a history button, false otherwise.
        */
        hasFeatureStatsButton() {
            return this.parent && this.parent.selectedTitle && this.parent.selectedFeature.get("key") && this.parent.selectedFeature.getGeometry().getType() != "Point";
        },

        /**
        * Checks should we display feature history button
        * 
        * @returns {boolean} True if the feature has a history button, false otherwise.
        */
        hasHistoryButton() {
            return this.parent && this.parent.selectedTitle && this.parent.selectedFeature.get("key") && this.parent.selectedFeature.getGeometry().getType() == "Point";
        },
    },

    watch: {
        o: {
            handler: function (val) {
                this.$store.formChanged = !this.deepEqualObjects(this.o, this.oSav);
            },
            deep: true,
        },
        activeTab: {
            handler: function (val) {
                let search = val;
                if (val == "new") {
                    search = null;
                } else {
                    search = val;
                }
                this.activeSO = this.o.simple_observations.filter (so => so.id == search)[0];
            }
        },  
    },
    async mounted() {
        this.initializeComponent(this.parentPopup);
        await this.loadLookup(this.simple_observation_lookup, true);
        if (this.id != null) {
            this.o = await this.get("User/GetSingleGeometry", { id: this.id }, true);
        } else {
            this.o = { id: null, name: "New " + this.formatDate(new Date()) };
        }
        if (!this.o.simple_observations) this.o.simple_observations = [];

        this.o.simple_observations.push ({id : null, date: this.defaultDate(), name: null, indicator_id: null, comment: null, images: []});
        for (let so of this.o.simple_observations) {
            if (!so.images) so.images = [];
        }
        this.activeTab = this.tabName(this.o.simple_observations[0].id);
        await this.$nextTick();

        this.selectedTypeChanged();
        window.addEventListener('deviceorientationabsolute', this.getCompass);
        //eventBus.on('popupClosed', this.popupClosed);

        this.oSav = this.deepClone(this.o);
        this.$store.formChanged = false;

    },  
    unmounted() {
        //eventBus.off('popupClosed');
        window.removeEventListener('deviceorientationabsolute', this.getCompass);
    },
    methods: {
        selectedTypeChanged() {
            this.selectedType = this.simple_observation_lookup.options.filter(x => x.id == this.activeSO.indicator_id)[0] ?? {}; 
            this.activeSO.indicator = this.selectedType.name;
            if (!this.selectedType.numerical) {
                this.activeSO.value = null;
                this.mask = null;
            } else {
                this.mask = this.createMask(this.selectedType.decimals_for_display);
            }
            this.rule = this.createBetweenRule(this.selectedType.value_from, this.selectedType.value_to);
        },
        // async close() {
        //     if (this.$store.formChanged) {
        //         if (await this.confirmDialog(this.$t("All changes will be lost. Do you want to continue?"))) {
        //             this.$store.formChanged = false;
        //             this.cancel();
        //         }
        //     } else {
        //         this.closePopup();
        //     }
        //     console.log("parent", this.parent);
        //     if (this.parent.reload) {
        //         await this.parent.reload();
        //     }
        // },
        async customUploadFactory(files) {

            // Polja iz `form-fields` koja želite dodati uz upload
            const extraFields = this.formFields.reduce((acc, field) => {
                acc[field.name] = field.value;
                return acc;
            }, {});

            const headers = {
                'Content-Type': 'multipart/form-data',
                Authorization: 'Bearer ' + this.$keycloak.token, // Dodavanje autentifikacijskog tokena
            };


            // Kreiramo niz Promise-ova za svaki upload
            const uploadPromises = files.map((file) => {
                // Kreiramo formData koji uključuje datoteku i dodatna polja
                let formData = new FormData();
                formData.append('file', file);

                // Dodajemo sva dodatna polja u formData
                Object.keys(extraFields).forEach((key) => {
                formData.append(key, extraFields[key]);
                });

                // Kreiramo i vraćamo axios post zahtjev
                return this.post(this.uploadURL, formData, true
                // , {
                //   headers: headers,
                //   onUploadProgress: (progressEvent) => {
                //     // Praćenje napretka uploada (ako želite)
                //     const percentCompleted = Math.round(
                //       (progressEvent.loaded * 100) / progressEvent.total
                //     );
                //     console.log(`Upload progress for ${file.name}: ${percentCompleted}%`);
                //   },
                // }
            );
        });

        try {
            // Čekamo da se svi uploadi završe
            const responses = await Promise.all(uploadPromises);
            // Ako želite da Q-Uploader vidi da su svi prošli, možete vratiti uspjeh
            return { success: true };
        } catch (error) {
            console.error("Error uploading files:", error);

            // Ako želite da Q-Uploader vidi grešku, možete je ovako vratiti
            throw new Error("Failed to upload files");
        }
        },
    },
};
</script>