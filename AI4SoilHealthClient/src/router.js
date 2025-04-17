import { createMemoryHistory, createRouter, createWebHistory } from 'vue-router';
/**
 * @desc Router module 
 * @module router
 */
const routes = [
  { path: '/', name: "Home", component: () => import("@/specific/components/home.vue"), meta : { title: 'Home', icon: 'home' } },
  { path: '/About', name: "About", component: () => import("@/specific/components/about.vue"), meta: { title: 'About', icon: 'info' } },
  { path: '/PrivacyPolicy', name:"Privacy policy", component: () => import("@/common/components/policy.vue"), props: { policyProp: 'privacy' } },
  { path: '/TermsOfService', name:"Terms of service", component: () => import("@/common/components/policy.vue"), props: { policyProp: 'terms' } },
  { path: '/ContactUs', name:"Contact us", component: () => import("@/common/components/contact-us.vue") },
  { path: '/:pathMatch(.*)*', redirect: '/' },
];

const router = createRouter({
  history: createWebHistory(),
  //history: createMemoryHistory(),
  routes: routes,
});

router.beforeEach((to, from, next) => {
  const store = router.app.config.globalProperties.$store;
  
  if (store.formChanged) {
    next(false);
    return;
  }
  
  if (store.popupLevel > 0) {
    store.newPopups[--store.popupLevel].show = false;
    next(false);
    return;
  }
  next();
});

export default router;

