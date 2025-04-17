/**
 * @desc A mixin object containing custom methods for the table component.
 * @module SimpleObservationMixin
 */
import { toLonLat, fromLonLat } from "ol/proj";
import Point from "ol/geom/Point";
import Feature from "ol/Feature";

export const SimpleObservationMixin = {
    methods: {

        addFileFeature(file) {  
            let point = new Point(fromLonLat([file.lon, file.lat], this.parent.projection));
            let feature = new Feature({
                id: 'im' + file.id,
                geometry: point,
                key: file.custom_geometry_id,
                image_id: file.id,
                extension: file.extension,
                compass: file.compass,
                name: file.name
            });
            feature.setId('im' + file.id);
            this.parent.editableSource.addFeature(feature);
        },

        async validate() {
            return await this.validateForm(this.$refs["qform" + this.tabName(this.activeSO.id)][0]);
        },

        async addImage() {
            if (!await this.validate()) return;
            this.$refs.uploader.pickFiles();
        },

        /**
         * Finishes editing an image. Repositions the image on the map if it was moved or rotated
         * @param {object} editedImage - The edited image.
         * @returns {void}
         */
        finishImage(editedImage) {
            if (this.parent && this.parent.editableSource) {
                let f = this.parent.editableSource.getFeatureById('im' + editedImage.id);
                if (f) {
                    this.parent.editableSource.removeFeature(f);
                    this.addFileFeature(editedImage);
                }
            } 
            this.copyObject(this.o, this.oSav);
            this.$store.formChanged = false;
            this.editingImage = false;
        },

        /**
         * Save modified properties of an image.
         */
        async saveImage() {
            await this.put("User/SaveImage", this.editedImage);
            this.finishImage(this.editedImage);
        },

        /**
         * Handles compassChanged event from ImageViewer.
         * @param {number} compass - The new compass value.
         * @param {number} id - The id of the image.
         * @returns {void}
         */
        compassChanged(compass, id) {
            let editedImage = this.activeSO.images.find(x => x.id == id);
            editedImage.compass = compass;
            this.finishImage(editedImage);
        },

        /**
         * Retrieves the compass value
         *
         * @param {Event} event - The event object.
         * @returns {Compass} The compass value.
         */
        getCompass(event) {
            if (event.alpha == null) this.$store.compass = null;
            else this.$store.compass = Math.abs(event.alpha - 360);
        },

        /**
         * Gets the name of the tab.
         * @param {number} id - The id of the tab.
         * @returns {string} The name of the tab.
         */
        tabName (id) {
            return id ? id : "new";
        },

        /**
         * Formats the today's date.
         * @returns {string} The formatted date.
         */
        defaultDate() {
            const today = new Date();
            return today.toISOString().split('T')[0];
        },

        /**
         * Returns URLs of the files for download from server.
         * 
         * @returns {array} URLs of the files.
        */
        filesUrls() {
            // return this.activeSO.images.map(x => ({ url: this.axios.API.defaults.baseURL + "User/GetImage/" + x.id + "/" + x.extension, compass: x.compass, name:x.name, file_id: x.id }));
            return this.activeSO.images.map(x => ({ url: "User/GetImage/" + x.id + "/" + x.extension, compass: x.compass, name:x.name, image_id: x.id }));
        },

        /**
         * Gets the GPS position.
         * @returns {object} The GPS position as an object with latitude and longitude.
         */
         async getGPSPosition() {
            let coords = { latitude: null, longitude: null };
            if (this.parent && this.parent.trackPosition && this.parent.geolocation.getPosition()) {
                let c = toLonLat(this.parent.geolocation.getPosition(), this.parent.projection);
                coords = { latitude: c[1], longitude: c[0] };
            } else {
                let position = await this.getCurrentPosition();
                coords = position.coords;
            }
            return coords;
        },

        /**
         * Gets the position of selected feature.
         */
        getLocationPosition() {
            let coords = { latitude: null, longitude: null }, f, c;
            if (this.parent && this.parent.selectedFeature) {
                if (this.parent.selectedFeature.getGeometry().getType() == "Polygon") {
                    f = this.parent.selectedFeature.getGeometry().getInteriorPoint().getCoordinates();
                } else {
                    f = this.parent.selectedFeature.getGeometry().getCoordinates();
                }
                c = toLonLat(f, this.parent.projection);
            } else {
                let geometry = this.o.geometry;
                // if geometry is a polygon, get the inner point
                if (geometry.type == "Point") {
                    c = [geometry.coordinates[0], geometry.coordinates[1]];
                } else {
                    // get the inner point of the polygon
                    let sum = [0, 0];
                    for (let i = 0; i < geometry.coordinates[0].length-1; i++) {
                        sum[0] += geometry.coordinates[0][i][0];
                        sum[1] += geometry.coordinates[0][i][1];
                    }   
                    c = [sum[0] / (geometry.coordinates[0].length - 1), sum[1] / (geometry.coordinates[0].length - 1)];
                }
            }
            coords = { latitude: c[1], longitude: c[0] };
            return coords;
        },

        /**
         * Sets the coordinates of a file to GPS position.
         */
         async setGPSPosition() {
            let coords = await this.getGPSPosition();
            this.editedImage.lat = coords.latitude;
            this.editedImage.lon = coords.longitude;
        },

        /**
         * Sets the coordinates of a file to feature position.
        */
        setLocationPosition() {
            let coords = this.getLocationPosition();
            this.editedImage.lat = coords.latitude;
            this.editedImage.lon = coords.longitude;
        },
        
        /**
         * Starts editing the properties of an image.
         * 
         * @param {object} index - Image to edit.
         */
        editImage(image) {
            this.editedImage = image;
            this.copyObject(this.editedImage, this.imageSav);  
            this.editingImage = true;
        },
        
        /**
         * Cancels editing the properties of an image.
         */
        cancelImage() {
            this.copyObject(this.imageSav, this.editedImage);
            this.editingImage = false;
        },

        /**
         * Opens ImageViewer at a given index.
         * 
         * @param {number} index - The index of the image to show.
         */
        showImage(index) {
            this.initPopup({ component : "image-viewer", maximized:true, files: this.filesUrls(), index: index, needsAuthentication: true, cache : true });
        },       

        /**
         * Deletes a single attached file asynchronously.
         *
         * @param {Object} file - The file to be deleted.
         * @returns {Promise} A promise that resolves when the file is successfully deleted.
         */
         async deleteImage(image) {
            if (await this.confirmDialog(this.$t("Delete image?"))) {
                if (await this.delete("User/DeleteImage/" + image.id, null, true) != null) {
                    this.activeSO.images = this.activeSO.images.filter(x => x.id != image.id);
                    if (this.parent && this.parent.editableSource) {
                        let f = this.parent.editableSource.getFeatureById('im' + image.id);
                        if (f) this.parent.editableSource.removeFeature(f);
                    }
                    this.copyObject(this.o, this.oSav);
                    this.$store.formChanged = false;
                }
            }
        },

        /**
         * Deletes a single observation asynchronously.
         * @param {number} id - The id of the observation to be deleted.
         * @returns {Promise} A promise that resolves when the observation is successfully deleted.
         */
        async deleteObservation(id) {
            if (await this.confirmDialog(this.$t("Delete observation with all associated images?"))) {
                if (await this.delete("User/DeleteSimpleObservation/" + id, null, true) != null) {
                    if (this.parent && this.parent.editableSource) {
                        for (let image of this.activeSO.images) {
                            let f = this.parent.editableSource.getFeatureById('im' + image.id);
                            if (f) this.parent.editableSource.removeFeature(f);
                        }
                    }
                    this.o.simple_observations = this.o.simple_observations.filter(x => x.id != id);
                    this.activeTab = this.tabName(this.o.simple_observations[0].id);
                    await this.$nextTick();
                    this.oSave = this.deepClone(this.o);
                    this.$store.formChanged = false;
                }
            }   
        },


        /**
         * Event handler for the file upload success.
         * @param {Object} response - The response object.
         */
        filesUploaded(response) {
            // let newImages = JSON.parse(response.xhr.response);
            let newImages = response;
            this.activeSO.images = [...this.activeSO.images, ...newImages];
            this.copyObject(this.o, this.oSav);
            this.$store.formChanged = false;
            if (this.parent && this.parent.editableSource) {
                for (let file of newImages) {
                    this.addFileFeature(file);
                }
            }
        },

        /**
         * Event handler for the file upload failure.
         * @param {Object} response - The response object.
         */
        async filesFailed(response) {
            await this.showError(response.xhr.response);
            this.$refs.uploader.reset();
        },

        async customUpload(files) {
            // Polja iz `form-fields` koja �elite dodati uz upload
            const extraFields = this.formFields(files).reduce((acc, field) => {
                acc[field.name] = field.value;
                return acc;
            }, {});

            const headers = {
                'Content-Type': 'multipart/form-data',
                Authorization: 'Bearer ' + this.$keycloak.token, // Dodavanje autentifikacijskog tokena
            };


            const uploadPromises = files.map((file) => {
                let formData = new FormData();
                formData.append('file', file);
        
                Object.keys(extraFields).forEach((key) => {
                  formData.append(key, extraFields[key]);
                });
        
                return this.post(this.uploadURL, formData, true
             
            ).then(response => {
                  return response;
                //   {
                //     success: true,
                //     file,
                //     response
                //   };
                });
              });
            
              try {
                const responses = await Promise.all(uploadPromises);
                this.filesUploaded(responses[0]);
                    // {
                //   success: true,
                //   files: responses.map(res => res.file)
                // }
            // );
              } catch (error) {
                console.error("Error uploading files:", error);
                this.filesFailed(error);
              }
        },

        /**
         * Creates object with additional form fields.
         * @param {Array} files - The files to be uploaded.
         * @returns {Array} The array of additional form fields.
         */
        formFields(files) {
            return files.map(x => {
                return {
                    name: x.name,
                    value: JSON.stringify({ compass: x.compass, lat: x.lat, lon: x.lon })
                }
            });
        },
        /**
         * Event handler for the files added.
         * @param {Array} files - The files added.
         * @returns {Promise} A promise that resolves when the files are successfully added.
         */
        async filesAdded(files) {

            if (this.activeSO.id == null) {
                if (!await this.save()) return;
            }

            let coords = { latitude: null, longitude: null };
            if (this.coordType == "gps") {
                coords = await this.getGPSPosition();
            }
            if (coords.latitude == null || coords.longitude == null) {
                coords = this.getLocationPosition();
            }
            for (let file of files) {
                file.lat = coords.latitude;
                file.lon = coords.longitude;
                file.compass = this.$store.compass;
                file.key = this.o.id;
            }
            this.customUpload(files);
            // this.$refs.uploader.upload();
        },

        /**
         * Saves changes.
         */
        async save() {

            if (!await this.validate()) return false;   

            let ret = await this.post('User/SetSingleGeometry', { 
                  custom_geometry: { "id" : this.o.id, "name" : this.o.name },
                  simple_observation: this.activeSO
                }, true 
            );

            if (ret) {
                if (this.o.id == null) {
                    this.parent.selectedFeature.set("key", ret.key);
                    this.parent.saveFeature(this.parent.selectedFeature);
                    this.o.id = ret.key;
                }
                if (ret.simple_observation_id != null) {
                    if (this.activeSO.id == null) {
                        this.o.simple_observations.push ({id : null, date: this.defaultDate(), name: null, simple_observation_type_id: null, comment: null, images: []});
                    }
                    this.activeSO.id = ret.simple_observation_id;
                    this.activeTab = ret.simple_observation_id;
                }
                
                this.oSav = this.deepClone(this.o);
                this.$store.formChanged = false;
                return true;
            } else {
                return false;
            }
        },

        /**
         * Cancels changes.
         */
        cancel() {
            if (this.$store.formChanged) {
                this.o = this.deepClone(this.oSav);
                this.$store.formChanged = false;
            } else {
                if (this.o.id == null) { // not saved, remove from editable source
                    if (this.parent && this.parent.editableSource) {
                        this.parent.editableSource.removeFeature(this.parent.selectedFeature);
                        this.parent.selectedFeature = null;
                    }
                }
                this.closePopup();
            }
            if (this.parent.reload) {
                this.parent.reload();
            }
        },
    }   
};