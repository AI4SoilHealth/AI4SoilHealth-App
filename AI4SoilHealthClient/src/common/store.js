/**
 * Store for the application
 * @module store
 */
import { reactive } from 'vue'
import { cstore } from '../specific/store.js';
export const store = reactive({
    version: "0.2.",  
    isOnline: null,
    EU: null,
    userData: null,
    get person_id() { return (this.EU ? this.EU.value : this.userData.id) },
    users: [],
    locale: null,
    langId: 1,
    localeOptions: [],
    routes: [],
    props: {}, // properties for the current route
    state: {}, // state for the current route
    working: false,
    formChanged: false,
    lastRenderTime: 0,
    tileLayers: null,
    drawerWidth: 200,
    drawer: false,
    app: null,
    q: null,
    get realDrawerWidth() { return this.drawer ? this.drawerWidth : 0; },    
    get screenWidth() { return this.q.screen.width - this.realDrawerWidth; },
    get screenHeight() { return (this.q.screen.height - 40) + 'px'; },
    get isWide() { return this.q.screen.width > 800 },
    contextValues: {},
    globalValues: {},
    catalogs: {},
    tableAPIProps: {},
    icons: [],
    newPopups: {},
    popups: {  // will be closed on back in this order
        help: {
            show: false,
        },
        chart: {
            props: {},
            show: false
        },
        upload: {
            props: {},
            show: false
        },
        ...cstore.additionalPopups, // add app-specific popups
        default: {
            show: false,
            props: {}
        },
    },
	progress: {
        show: false,
        value: 0,
        message: null
    },
    level: 1,
    rules: {
        required: [val => !!val || 'Value is required'],
        email: [val => {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(val) || 'Please enter a valid email address';
        }],
        emailMultiple: [val => {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return val.split(';').every(v => emailRegex.test(v.trim())) || 'Please enter valid email address or adresses separated by semicolons';
        }],
    },
    news: [],
    popupLevel: 0,
    ...cstore
});
