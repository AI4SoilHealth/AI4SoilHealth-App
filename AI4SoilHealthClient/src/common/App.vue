<template>
  <div>
    <q-scroll-area v-if="noInterface" style="height: 100vh; max-width: 100vw;" :bar-style="{ width: '10px' }">
      <component :is="noInterfaceComponent" :props="noInterfaceProps" :parentPopup="null" :noInterface="true"/>
    </q-scroll-area>
    <div v-else id="q-app" style="min-height: 100vh;" class="nomy nopx nomx" @keydown.f9="translate">
      <q-layout view="hHh Lpr fFf" container style="height: 100vh" class="nomy nopx nomx">
        <q-header style="height: 40px;">
          <q-toolbar class="nopx" style="min-height:40px;">
            <q-btn flat @click="$store.drawer = !$store.drawer" dense icon="menu" />
            <q-toolbar-title class="text-subtitle1 nomy">{{ $store.appName }} {{ $store.version
              }}
            </q-toolbar-title>

            <autocomplete bg-color="primary" v-if="$store.userData && $store.userData.is_admin" v-model="$store.EU"
              style="width: 100px" clearable filled :options="$store.users" dense options-dense
              :display-value="$store.EU ? $store.EU.short_display_value : null"
              @update:model-value="emulatedUserChanged" />
            <q-btn flat dense class="nomy" v-if="!$store.isOnline" icon="wifi_off" />
            &nbsp;{{ $store.userData && $store.userData.first_name > '' && $store.userData.last_name > '' ?
            ($store.isWide ? `${$store.userData.first_name} ${$store.userData.last_name}` :
            $store.userData.first_name.charAt(0)
            +
            $store.userData.last_name.charAt(0)) :
            '' }}&nbsp;
            <q-btn v-if="$keycloak.token" class="nomy" flat @click="$logout" dense
              :label="$store.isWide ? $t('Logout') : ''" icon-right="logout" no-caps />
            <q-btn v-else class="nomy" flat dense :label="$store.isWide ? $t('Login') : ''" icon-right="login"
              @click="$keycloak.login()" no-caps>
            </q-btn>
            <accessibility />
            <lang-switcher v-show="$store.hasLangSwitcher" ref="langSwitcher" />
            <q-btn class="nomy" flat @click="toggleFullscreen" dense
              :icon="(fullscreen ? 'fullscreen_exit' : 'fullscreen')" />
          </q-toolbar>
        </q-header>

        <q-drawer style="top: 40px" v-model="$store.drawer" :width="$store.drawerWidth" bordered
          :breakpoint="breakpoint" :overlay="false">
          <q-scroll-area :style="{ height: $store.screenHeight }">
            <!-- <q-scroll-area style="height: 100vh;" :bar-style="{ width: '10px' }"> -->
            <div v-if="isAdmin" class="row">
              <q-input v-model="treeFilter" dense style="width:130px" e>
                <template v-slot:prepend>
                  <q-icon name="search"></q-icon>
                </template>
              </q-input>
              <q-btn flat dense icon="cancel" @click="treeFilter=''" />
              <q-btn flat dense icon="refresh" @click="refresh" />
            </div>
            <q-tree ref="tree" class="primary text-body2" :nodes="tree" node-key="path" no-connectors
              :filter="treeFilter" :default-expand-all="treeFilter.length > 0" v-model:selected="selected"
              v-if="tree.length > 0" @update:selected="selectionUpdated">
              <template v-slot:default-header="props">
                <div>
                  <span class="drop-zone" v-if="draggedItem" @drop="onDrop($event, props, 'before')" @dragover.prevent>
                  </span>
                  <span :draggable="$store.userData && $store.userData.is_admin" @dragstart="onDragStart($event, props)"
                    @dragend="onDragEnd" @drop="onDrop($event, props, 'on')" @dragover.prevent>
                    <q-icon v-if="props.node.icon" class="q-pr-xs" :name="props.node.icon"
                      :color="props.node.iconColor" />
                    <span v-html="props.node.label" />
                    <ContextMenu v-if="$store.userData && $store.userData.is_admin" ref="clickMenu" :options="[
                      { label: $t('CRUD'), callback: crud, options: props.node, visible: !props.node.locked },
                      { label: $t('Lock'), callback: setLocked, options: props.node, visible: !props.node.locked  },
                      { label: $t('Unlock'), callback: setLocked, options: props.node, visible: props.node.locked },
                      { label: $t('Delete'), callback: deleteRoute, options: props.node, visible: !props.node.locked },
                      { label: $t('Roles'), callback: setRouteRole, options: props.node },
                      { label: $t('Settings'), callback: editSettings, options: props.node },
                      ]" />
                  </span>
                  <span class="drop-zone" v-if="draggedItem" @drop="onDrop($event, props, 'after')" @dragover.prevent>
                  </span>
                </div>
              </template>
            </q-tree>
          </q-scroll-area>
        </q-drawer>

        <q-scroll-area style="height: 100vh; max-width: 100vw;" :bar-style="{ width: '10px' }"
          :thumb-style="{ width: '0px' }">
          <q-page-container class="q-pt-none">
            <q-page>
                <router-view />
                <!-- <router-view v-slot="{ Component, route }">
                  <keep-alive>
                    <component :is="Component" :key="route.path" />
                  </keep-alive>
                </router-view> -->
            </q-page>
          </q-page-container>
        </q-scroll-area>

      </q-layout>

      <div v-for="popup in $store.newPopups">
        <popup v-if="popup.show" :name="popup.name"/>
      </div>

      <task-progress v-if="$store.progress.show" />

      <PWAPrompt ref="pwa" />
      <q-dialog v-model="$store.showAd"><ad-popup /></q-dialog>
      <component-settings ref="componentSettings" v-show="false" :path="csPath"/>
    </div>
  </div>
</template>

<script>

import { setCssVar } from 'quasar';
import { loadComponent } from '@/common/component-loader';
import { markRaw } from 'vue';
import PWAPrompt from '@/common/components/pwa-prompt.vue';
import AdPopup from './components/ad-popup.vue';

/**
* The main component of the application.
* Renders the layout and handles user interactions.
*
* @component
* @example
*
<App />
*/

export default {
  name: "App",
  components: {
    LangSwitcher: loadComponent("lang-switcher"),
    Accessibility: loadComponent("accessibility"),
    Popup: loadComponent("popup"),
    PWAPrompt,
    HelpDialog: loadComponent("help-dialog"),
    ChartPopup: loadComponent("chart-popup"),
    TaskProgress: loadComponent("task-progress"),
    Autocomplete: loadComponent("autocomplete"),
    ContextMenu: loadComponent("context-menu"),
    AdPopup: loadComponent("ad-popup"),
    ComponentSettings: loadComponent("component-settings"),
  },
  data: () => ({
    selected: null,
    fullscreen: false,
    canInstall: false,
    breakpoint: 800,
    treeFilter: '',
    draggedItem: null,
    hoveredItem: null,
    loaded: false,
    noInterface: false,
    noInterfaceComponent: null,
    noInterfaceProps: {},
    csPath: null,
  }),
  watch: {
    treeFilter(val) {
      if (val.length > 0) {
        this.$refs.tree.expandAll();
      }
    }
  },
  computed: {
    /**
    * Filters the tree based on the provided filter text.
    * @returns {Array} The filtered tree items.
    */
    treeFiltered() {
      // Filter the tree based on the treeFilter, recusively filtering the children
      return this.tree.filter((item) => {
        if (item.label.toLowerCase().includes(this.treeFilter.toLowerCase())) return true;
        if (item.children) {
          item.children = item.children.filter((child) => {
            if (child.label.toLowerCase().includes(this.treeFilter.toLowerCase())) return true;
            return false;
          });
          return item.children.length > 0;
        }
        return false;
      });
    },

    /**
    * Retrieves the tree data.
    */
    tree() {
      let root = this.$store.routes.filter((item) => !item.parent && item.active && item.order_no > 0);
      let children = root.map(route => this.routeAtts(route));
      children = children.filter(c => (this.$store.isOnline || c.offline));
      return children;
    },
  },
  /**
  * The created lifecycle hook.
  * Checks if the server is online and calls the init method.
  */
  async created() {

    if (!this.$store.isOnline && !this.$store.pwa) {
      await this.showError(this.$t('The server is offline. Please try again later.'));
      return;
    }

    //let test = await this.get("CommonAnon/Ping", null);

    if (this.$store.noInterfaceComponent) { // can be invoked with no interface
      for (let nip of this.$store.noInterfaceParams) { // ist here a param to invoke with no iterface?
        if (this.$route.params[nip]) {
          this.noInterfaceComponent = markRaw(loadComponent(this.$store.noInterfaceComponent));
          //this.noInterfaceComponent = loadComponent(this.$store.noInterfaceComponent);
          this.noInterfaceProps[nip] = this.$route.params[nip];
          this.noInterface = true;
          break;
        }
      }
    }

    if (!this.noInterface) {
      await this.init();
    }

    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.addEventListener('controllerchange', async () => {
        if (await this.confirmDialog(this.$t('New version available. Reload now?'))) {
          window.location.reload();
        }
      });
    }

    if (this.$store.dataAdClient) this.$store.showAd = true;

    for (let i = 0; i < this.$store.maxPopups; i++) {
      this.$store.newPopups[i.toString()] = { name: i.toString(), show: false, props: {} };
    }

  },

  methods: {
    /**
    * Initializes the application.
    *
    * @returns {Promise<void>} A promise that resolves when the initialization is complete.
    */
    async init() {
      setCssVar("tooltip-fontsize", "12px");
      this.$store.drawer = this.$q.screen.width >= this.breakpoint;
      this.$store.userData = this.$q.localStorage.getItem("userData");

      this.$store.localeOptions = await this.get("CommonAnon/GetLocaleOptions", null, true);
      if (this.$store.hasCatalogs) {
        this.$store.catalogs = await this.get("CommonAnon/GetCatalogs", null, true);
      }

      if (this.$store.hasNews) {
        await this.loadNews();
      }

      // set default context values
      if (this.$store.defaultContextValues) {
        for (let cv of this.$store.defaultContextValues) {
          if (!this.$q.localStorage.has("context_value_" + cv.name)) {
            this.$q.localStorage.setItem("context_value_" + cv.name, cv);
          }
        }
      }

      if (this.$keycloak.authenticated) {
        let ref = "";
        if (this.$route.query.ref) {
          ref = "/" + this.$route.query.ref;
        }
        let ret = await this.get('Auth/GetUser' + ref);
        if (ret) {
          if (ret.agreement) {
            if (await this.confirmDialog(ret.agreement, this.$t('You have to accept the terms and conditions to continue:'),
              this.$t('Accept'), this.$t('Decline'))) {
              await this.post('Auth/AcceptAgreement');
              this.$store.userData = ret;
              this.$q.localStorage.set('userData', this.$store.userData);
            } else {
              this.$logout();
            }
          } else {
            this.$store.userData = ret;
            this.$q.localStorage.set('userData', this.$store.userData);
            if (this.$store.userData.is_admin) {
              this.$store.users = await this.get('Auth/GetUsers');
            }
          }
        } else {
          //await this.showError(this.$t('User not found'));
          setTimeout(this.$logout, 3000);
        }
      }

      //if (this.$store.hasLangSwitcher) {
      await this.waitForRefs(["langSwitcher"]);
      this.$refs.langSwitcher.localeChanged();
      this.$store.pwaComponent = this.$refs.pwa;
    },

    /**
    * Updates the selection with the specified ID.
    *
    * @param {number} id - The ID of the selection to update.
    * @returns {Promise<void>} - A promise that resolves when the selection is updated.
      */
    async selectionUpdated(id) {
      this.$store.popupLevel = 0;
      if (id == null) return;
      if (await this.checkFormChanged()) return;
      let route = this.$store.routes.find((item) => item.path == id);
      if (route.component > "") {
        this.$store.state = {};
        this.$store.level = 0;
        this.activateRoute(route);
        this.$store.drawer = this.$q.screen.width >= this.breakpoint;
      } else {
        this.$refs.tree.setExpanded(id, !this.$refs.tree.isExpanded(id));
      }
      this.selected = null;
    },

    /**
    * Toggles the fullscreen mode.
    */
    toggleFullscreen() {
      if (this.fullscreen) {
        document.exitFullscreen();
      } else {
        document.documentElement.requestFullscreen();
      }
      this.fullscreen = !this.fullscreen;
    },

    /**
    * Reloads the current user after emulated user changed.
    */
    emulatedUserChanged() {
      this.getRoutes();
      this.activateRoute(this.$store.routes.find((item) => item.path == this.$route.path));
    },

    /**
     * Refreshes the tree.
     */
    refresh() {
      this.delete("Dev/ClearCache");
      this.getRoutes();
    },

    /*
     * Starts the drag operation of a menu item
     */
    onDragStart(event, props) {
      //event.dataTransfer.setData('text/plain', props.node.id)
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.dropEffect = 'move';
      setTimeout(() => {
        this.draggedItem = props.node;
      }, 10);
    },

    /*
     * Ends the drag operation of a menu item
     */
    onDragEnd(event) {
      this.draggedItem = null;
    },

    /*
     * Handles the drop event of a menu item
     */
    async onDrop(event, props, where) {
      await this.post(`Dev/ReorderMenu/${this.draggedItem.id}/${props.node.id}/${where}`);
      this.draggedItem = null;
      await this.delete("Dev/ClearCache");
      await this.getRoutes();
    },

    /*
      * Creates a CRUD for the selected route
      * @param {Object} options - The options object.
    */
    async crud(options) {
      if (!await this.confirmDialog(this.$t('This will overwrite the existing CRUD. Continue?'))) {
        return;
      }
      if (!options.props.tableAPI) {
        await this.showError(this.$t('Table API not defined'));
        return;
      } else {
        let i = options.props.tableAPI.indexOf('_');
        await this.post("Dev/Crud", { schemaName: options.props.tableAPI.substring(0, i), tableName: options.props.tableAPI.substring(i+1), forDetail: false, withOwnership: false });       
      }
    },

    /*
      * Locks or unlocks the selected route
      * @param {Object} options - The options object.
      */
    async setLocked(options) {
      await this.put("Dev/SetLocked/" + options.id + "/" + !options.locked);
      await this.delete("Dev/ClearCache");
      await this.getRoutes();
    },

    /*
      * Deletes the selected route
      * @param {Object} options - The options object.
      */
    async deleteRoute(options) {
      if (!await this.confirmDialog(this.$t('This will delete this route. Continue?'))) {
        return;
      }
      await this.delete("Dev/DeleteRoute/" + options.id);
      await this.delete("Dev/ClearCache");
      await this.getRoutes();
    },

    async setRouteRole(options) {
      this.initPopup({ component: "table", tableAPI: "auth_route_role", tableAPIKey: options.id, masterKey : "route_id", masterValue : options.id, title : "Roles for " + options.label });
    },

    async editSettings(options) {
      this.csPath = options.path;
      await this.$refs.componentSettings.getSettings();
    },

    /**
     * Creates an object with the attributes of a route.
     * @param {Object} route - The route object.
     * @returns {Object} - The route attributes.
     */
    routeAtts(route) {
      return {
        label: route.title,
        name: route.name,
        path: route.path,
        icon: route.icon,
        iconColor: route.iconColor ?? "primary",
        offline: route.offline,
        id: route.id,
        children: this.getChildRoutes(route.path),
        schema_name: route.schema_name,
        table_name: route.table_name,
        props: route.props,
        locked: route.locked
      }
    },

    /**
    * Retrieves the child routes for a given parent route.
    *
    * @param {string} parentName - The name of the parent route.
    * @returns {Array} - An array of child routes.
    */
    getChildRoutes(parentName) {
      // Filter and return the child routes for the given parentName
      let children = this.$store.routes.filter((route) => route.parent === parentName && route.active);
      if (children.length === 0) return [];
      return children.map(route => this.routeAtts(route));
    },
  }
}
</script>

<style scoped>
/* .drop-zone {
  height: 0;
  opacity: 0;
  transition: all 0.2s ease;
}

.drop-zone.visible { */
.drop-zone {
  display: inline-block;
  height: 15px;
  width: 15px;
  margin: 0 5px;
  opacity: 1;
  background-color: rgba(0, 0, 0, 0.1);
}
</style>
