<template>
    <div>
        <Header name="Home" :title="$t('Home')" />
        <!-- <q-btn @click="ch">Exec</q-btn> -->
        <news-reader />
    </div>
</template>
<script>
/**
 * Home component
 * @component components/home
 * @description Home component
 * @example
 *  <Home />
 */
import { loadComponent } from '@/common/component-loader';
export default {
    name: "Home",
    components: {
        NewsReader: loadComponent('news-reader'),
    },
    data() {
        return {
        }
    },
    async mounted() {

    },
    methods: {
        ch() {
                     this.initPopup({
             component: "table", show: true, maximized: true, data: [{ a: 1, c: 3 }, { b: 5 }]
        });
            //    this.initPopup({component : "chart-popup",                    maximized: true,
            //         chartType: "boxplot",
            //        data: [{label: "A", min : 0, q1: 4, median : 10, mean: 12, q3 : 15, max : 200.5}, {label: "B", min : 5, q1: 6, median : 12, mean: 14, q3 : 17, max : 22}, {label: "C", min : 7, q1: 8, median : 14, mean: 16, q3 : 19, max : 24}],
            //         labelField: "label",
            //         xScale: "category",
            //         unit: "kg"
            //     });
        }
    }
};
</script>
<style scoped>
.container {
    /* display: flex;
    justify-content: center;
    align-items: center; */
    height: 100vh;
    width: 100vw;
}
</style>