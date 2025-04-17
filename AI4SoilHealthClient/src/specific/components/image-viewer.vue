<template>
        <q-card class="q-pa-none">
            <q-card-section v-if="files.length > 0" class="q-pa-xs" style="display: flex; justify-content: center; align-items: center;">
                <compass v-model="files[i].compass" :id="image_id" 
                    @update:model-value="compassChanged" />
            </q-card-section>
            <q-card-section class="q-pa-none img-flex-container">
                <q-btn size="xl" v-if="i > 0" class="left" dense flat round icon="arrow_back" @click="backward"
                        @touchstart="backward" color="primary"/>
                    <q-btn size="xl" v-if="i < files.length - 1" class="right" dense flat round icon="arrow_forward"
                        @click="forward" @touchstart="forward" color="primary" />
                <div @wheel="handleZoom" @touchstart.prevent="null" class="img-container">           
                    <img ref="img" class="img" :src="src" @mousedown="handleDragStart" @mousemove="handleDragMove"
                        @mouseup="handleDragEnd" @selectstart="handleSelectStart" @mouseleave="handleDragEnd"
                        @dragstart="handleDefaultDragStart" @touchstart.prevent="onTouchStart" @touchend.prevent="onTouchEnd"
                        @touchmove.prevent="onTouchMove" @load="$store.working = false" />
                </div>
            </q-card-section>

        </q-card>
</template>


<script>
import { loadComponent } from '@/common/component-loader';

/**
 * Image viewer component
 * @component
 * @name ImageViewer
 * @example
 * <ImageViewer />
 */
export default {
    name: "ImageViewer",
    props: ["parentPopup"],
    components: {
        Compass: loadComponent('compass'),
    },
    data() {
        return {
            files: [],
            needsAuthentication: false,
            src: "",
            i: 0,
            img: null,
            scale: 1,
            prevScale: 1,
            prevX: 0,
            prevY: 0,
            dX: 0,
            dY: 0,
            totaldX: 0,
            totaldY: 0,
            prevDist: 0,
            isDragging: false,
            image_id: null,
            cache: false,
            compassChangedFunction: null
        };
    },
    
    async mounted() {
        this.initializeComponent(this.parentPopup);
        this.parentPopup.title = this.files && this.files.length > 0 ? this.files[this.index].name : $t('Image viewer');
        this.parentPopup.buttons = [{
            label: this.$t("Download"),
            icon: "download",
            action: this.downloadImage,
            tooltip: this.$t("Download the image")
        }];
        // async show(files, index, needsAuthentication, cache = false) {
        this.src = "";
        await this.load();
    },

    methods: {

        /**
         * Downloads the image.
         */
        async downloadImage() {
            this.$store.working = true;
            let imageUrl = "";
            if (this.needsAuthentication) {
                imageUrl = await this.getImage(this.files[this.i].url, this.cache);
            } else {
                // todo
                imageUrl = this.files[this.i].url;
            }

            const a = document.createElement("a");
            a.href = imageUrl;
            a.download = this.files[this.i].name || "image.jpg";
   
            a.click();
            if (this.needsAuthentication) {
                URL.revokeObjectURL(imageUrl);
            }
            this.$store.working = false;
        },

        /**
         * Moves the image viewer forward to the next image.
         */
        forward() {
            if (this.i < this.files.length - 1) {
                this.i++;
                this.load();
            }
        },

        /**
         * Moves the image viewer backward to the previous image.
         */
        backward() {
            if (this.i > 0) {
                this.i--;
                this.load();
            }
        },

        /**
         * Loads the image from the server.
         */
        async load() {
            this.$store.working = true;
            this.scale = 1;
            this.dX = 0;
            this.dY = 0;
            this.prevX = 0;
            this.prevY = 0;
            this.totaldX = 0;
            this.totaldY = 0;
            this.isDragging = false;
            this.image_id = this.files[this.i].image_id;
            this.transform();
            if (this.needsAuthentication) {
                this.src = await this.getImage(this.files[this.i].url, this.cache);
            } else {
                // todo
                this.src = this.files[this.i].url;
            }
            this.$store.working = false;
        },

        /**
         * Handles the drag start event for the default drag behavior.
         * 
         * @param {Event} event - The drag start event.
         */
        handleDefaultDragStart(event) {
            event.preventDefault();
        },

        /**
         * Handles the select start event.
         *
         * @param {Event} event - The select start event.
         */
        handleSelectStart(event) {
            event.preventDefault();
            return false;
        },

        reset() {
            this.scale = 1;
            this.dX = 0;
            this.dY = 0;
            this.prevX = 0;
            this.prevY = 0;
            this.totaldX = 0;
            this.totaldY = 0;
            this.isDragging = false;
            this.$refs.img.style.cursor = "default";
            this.transform();
        },

        /**
         * Handles the zoom event.
         *
         * @param {Event} event - The zoom event.
         */
        handleZoom(event) {
            event.preventDefault();
            const delta = event.deltaY ? event.deltaY / 1000 : event.scale - 1;
            this.scale += delta;
            this.scale = Math.min(Math.max(1, this.scale), 10); // Limit scale between 1 and 4
            if (this.scale <= 1) {
                this.reset();
            } else {
                this.$refs.img.style.cursor = "grab";
            }
            this.transform();
        },

        shouldChangeImage(dX) {
            if (this.scale > 1) return;
            if (dX < - 100) {
                this.forward();
            } else if (dX > 100) {
                this.backward();
            };
        },

        /**
         * Handles the drag start event.
         * 
         * @param {Event} event - The drag start event.
         */
        handleDragStart(event) {
            this.prevX = event.clientX;
            this.prevY = event.clientY;
            //if (this.scale <= 1) return;
            this.$refs.img.style.cursor = "grabbing";
            this.isDragging = true;
        },

        /**
         * Handles the drag move event.
         *
         * @param {Event} event - The drag move event.
         */
        handleDragMove(event) {
            if (!this.isDragging || this.scale <= 1) return;
            this.dX = event.clientX - this.prevX;
            this.dY = event.clientY - this.prevY;
            this.prevX = event.clientX;
            this.prevY = event.clientY;
            this.totaldX += this.dX;
            this.totaldY += this.dY;
            this.transform();
        },

        /**
         * Handles the drag end event.
         *
         * @param {Event} event - The drag end event.
         */
        handleDragEnd(event) {
            if (this.scale <= 1 && this.isDragging) {
                this.shouldChangeImage(event.clientX - this.prevX);
            }
            this.$refs.img.style.cursor = "default";
            this.isDragging = false;
        },

        /**
         * Sets CSS image transformation depending on scale and drag.
         */
        transform() {
            this.$refs.img.style.transform = `scale(${this.scale}) translate(${this.totaldX}px, ${this.totaldY}px)`;
        },

        onTouchStart(ev) {
            if (ev.touches.length === 1) {
                this.prevX = ev.touches[0].clientX
                this.prevY = ev.touches[0].clientY
                this.isDragging = true;
            } else if (ev.touches.length === 2) {
                let distX = ev.touches[0].clientX - ev.touches[1].clientX
                let distY = ev.touches[0].clientY - ev.touches[1].clientY
                this.prevDist = Math.sqrt(distX * distX + distY * distY)
            }
        },

        onTouchMove(ev) {
            if (ev.touches.length === 1 && this.isDragging) {
                this.dX = ev.touches[0].clientX - this.prevX;
                this.dY = ev.touches[0].clientY - this.prevY;
                if (this.scale <= 1) return;
                this.prevX = ev.touches[0].clientX;
                this.prevY = ev.touches[0].clientY;
                this.totaldX += this.dX;
                this.totaldY += this.dY;
                this.transform();
                // this.onPointerMove(ev.touches[0].clientX, ev.touches[0].clientY)
            } else if (ev.touches.length === 2) {
                let distX = ev.touches[0].clientX - ev.touches[1].clientX;
                let distY = ev.touches[0].clientY - ev.touches[1].clientY;
                let dist = Math.sqrt(distX * distX + distY * distY);
                this.scale *= dist / this.prevDist;
                this.scale = Math.min(Math.max(1, this.scale), 10);
                if (this.scale <= 1) {
                    this.reset();
                }
                this.prevDist = dist;
                this.transform();
            }
        },
        onTouchEnd(ev) {
            this.isDragging = false;
            if (this.scale <= 1) {
                this.shouldChangeImage(this.dX);
            }
        },
        compassChanged(val) {
            this.files[this.i].compass = val;
            if (this.compassChangedFunction) this.compassChangedFunction(val, this.files[this.i].image_id);
            //this.$emit("compassChanged", this.files[this.i].compass, this.files[this.i].image_id);
        }
    }
}
</script>
<style scoped>

.left {
    position: absolute;
    left: 3px;
    z-index: 100;
    color: black
}

.right {
    position: absolute;
    right: 3px;
    z-index: 100;
    color: black
}

.img-flex-container {
    height: calc(100vh - 90px); display: flex; justify-content: center; align-items: center;
}

/* .img-container {
    position: relative; width: 100%; height: 100%;
} */

.img {
    max-width: 100% !important;
    max-height: calc(100vh - 90px) !important;
    /* width: auto !important;
    height: auto !important; */
    z-index: 0;
    object-fit: contain;
}
</style>
