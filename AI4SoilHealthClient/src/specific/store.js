import { reactive } from 'vue'
/**
 * @desc Module containing the application specific store.
 * @module store
 */
export const cstore = reactive({
    /* These properties in the `store` object are related to app-specific functionalities: */
    appName: "AI4SoilHealth",
    pwa: true,
    hasNews: true,
    hasCatalogs: true,
    hasLangSwitcher: true,
    clientCoordinates: [0, 0],
    geometryProps: [],
    position: null,
    compass: null,
    config: {
        dark: false,
        
        brand: {
            primary: '#315440',
            
            background: '#E0E5E2',
            'background-dark': '#333333',
            tooltip: 'rgba(49, 84, 64, 0.85)',

            secondary: '#26A69A',
            accent: '#9C27B0',

            dark: '#1d1d1d',
            'dark-page': '#121212',

            positive: '#21BA45',
            negative: '#C10015',
            info: '#31CCEC',
            warning: '#F2C037'
        }
    },
    noInterfaceParams: [],
    noInterfaceComponent: null,
    maxPopups: 5
});




