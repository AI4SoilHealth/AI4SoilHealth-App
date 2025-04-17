<template>
    <div>
        <div class="row align-center">
            <q-btn v-if="allowEdit" class="q-mx-sm" dense icon="chevron_left" flat @click="decrement" color="primary" no-caps
            v-touch-repeat:500:30.mouse.enter.space="decrement"/>
            <canvas class="q-mx-sm" id="compass" width="200" height="35" style="padding:2px; border: 1px solid lightgray"></canvas>
            <q-btn v-if="allowEdit" class="q-mx-sm" dense icon="chevron_right" flat @click="increment"  color="primary" no-caps v-touch-repeat:500:30.mouse.enter.space="increment"/>
            <q-input v-if="allowEdit"  class="input-field" type="number" v-model="value" dense style="width:50px;" />
            <!-- <input class="q-px-sm input-field" v-model="value" type="text" pattern="[0-9]*" style="height:30px; width:50px"/> -->
            <q-btn v-if="allowEdit" :disable="oldValue == value" dense icon="save" flat @click="save" color="primary" no-caps/>
        </div>
    </div>
</template>

<script>
/**
 * Compass component
 * @component components/compass
 * @description Compass component
 * @example
 *  <Compass v-model="value" :id="featureId" />
 */
export default {
    name: "Compass",
    props: {
        modelValue: null,
        id: null,
        allowEdit: {
            type: Boolean,
            default: true
        }   
    },
    data() {
        return {
            value: 0,
            oldValue: 0,
            sides: {
                "0": "N",
                "22": "NNE",
                "45": "NE",
                "67": "ENE",
                "90": "E",
                "112": "ESE",
                "135": "SE",
                "157": "SSE",
                "180": "S",
                "202": "SSW",
                "225": "SW",
                "247": "WSW",
                "270": "W",
                "292": "WNW",
                "315": "NW",
                "337": "NNW"
            }
        }
    },
    watch: {
        /**
         * Watches for modelValue changes and repaints the compass
         * @param val 
         */
        value: function (val) {
            //this.$emit('update:modelValue', val);
            this.value = val % 360;
            this.drawCompass(val);
        },
        modelValue: function (val) {
            //this.$emit('update:modelValue', val);
            this.value = val % 360;
            this.drawCompass(val);
        }
    },
    /**
     * Mounted hook
     */
    mounted() {
        this.value = this.modelValue;
        this.oldValue = this.modelValue;
        this.drawCompass(this.modelValue);
    },
    methods: {
        /**
         * Decrements the value by 1
         */
        decrement() {
            this.value = (this.value - 1) % 360;
            if (this.value < 0) this.value += 360;
        },
        /**
         * Increments the value by 1
         */
        increment() {
            this.value = (this.value + 1) % 360;
        },
        /**
         * Saves the modified value to the database
         */
        save() {
            this.put("User/UpdateCompass/" + this.id + "/" + this.value);
            this.$emit('update:modelValue', this.value);
            this.oldValue = this.value;
        },
        /**
         * Draws the compass
         * @param {number} angle 
         */
        drawCompass: function (angle) {
            var canvas = document.getElementById('compass');
            var ctx = canvas.getContext('2d');
            let color = this.$q.dark.isActive ? "white" : "black";
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.save();
            ctx.beginPath();
            ctx.strokeStyle = "green";
            ctx.moveTo(100, 0);
            ctx.lineTo(100, 35);
            ctx.stroke();
            const delta = 30;
            for (let i = angle - delta; i <= angle + delta; i++) {
                let j = i % 360;
                if (j < 0) j += 360;
                let x = (i - (angle - delta)) * 200 / 60;
                ctx.beginPath();
                ctx.strokeStyle = color;
                ctx.fillStyle = color;
                ctx.font = "10px Arial";
                ctx.lineWidth = 1;
                if (this.sides[j]) {
                    //calc width of text
                    let width = ctx.measureText(this.sides[j]).width;
                    ctx.fillStyle = color;
                    ctx.fillText(this.sides[j], x - width / 2, 10);
                }
                if (j % 10 == 0) {
                    ctx.moveTo(x, 15);
                    ctx.lineTo(x, 25);
                    let width = ctx.measureText(j).width;
                    ctx.fillText(j, x - width / 2, 35);
                } else if (j % 5 == 0) {
                    ctx.moveTo(x, 15);
                    ctx.lineTo(x, 20);
                }
                ctx.stroke();
            }

            ctx.restore();
        }
    }
}
</script>
<style scoped>

.input-field::-webkit-inner-spin-button,
.input-field::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
}

</style>