/**
 * @desc A mixin object containing methods for table initalization and action invocations
 * @module TableMixin
 */
import { exportFile } from 'quasar';
import { TableExportMixin } from '@/specific/mixins/table-export.js';
import { TableCustomMixin } from '@/specific/mixins/table-custom.js';
import { TableUtilsMixin } from '@/common/mixins/table-utils.js';

export const TableMixin = {
    props: {
        options: null,
        detailTable: false,
        parentPopup: null,
    },
    mixins: [TableExportMixin, TableCustomMixin, TableUtilsMixin],
    data() {
        return {
            name: null,
            title: null,
            selection: "none",
            selectedRows: [],
            pagination: { rowsPerPage: 0 },
            rows: [],
            columns: [],
            from: null,
            to: null,
            filter: {},
            filter2: {},
            filterExp: {},
            showFilter: false,
            frugal: false,
            json: false,
            tableAPI: null,
            tableAPIKey: null,
            restAPI: null,
            editingRow: null,
            editingRowSaved: {},
            editingRowIndex: 0,
            dbFunction: null,
            key: "id",
            rowActions: null,
            tableActions: null,
            columns: [],
            changedRows: {},  
            showDetails: false,
            masterKey: null,
            allowEdit: true,
            allowNew: true,
            allowDelete: true,
            selection: "none",
            loaded: false,
            params: null,
            contextValuesLocal: [],
            data: [],
            exportPreprocess: null,
            noInlineEditing: false,
            grid: false,
            lookupsLoaded: false,
            activeLookup: null,
            summary: null,
            summary_top: null,
            read_only: false,
            hideDefaultToolbar: false,
            hideRecordsToolbar: false,
            currentOverlay: null,
            props: {},
            index: 0,
            editedItem: null,
            lookupDisplayIndex: 0,
            overlayShown: null,
            overlays: { },
            asForm: false,
            visibleColumns: [],
            contextValuesLoaded: false,
            singleRow: false,
            offline: false,
            //rowToolbarWidth: 50,
        }
    },
    methods: {

        /**
         * Initializes the tableAPI  component.
         */
        async init() {  
            console.log("init table");      
            // when child table is initiated in popup it is possible that $route.path of parent table is already saved in $store.state
            if (!this.parentPopup && this.$store.state[this.$route.path]) {
                //console.log("copying from store");
                //return;
                Object.assign(this.$data, this.$store.state[this.$route.path]);
                
                //this.copyObject(this.$store.state[this.$route.path], this, true);
                return;
            }
            this.$store.fromMenu = false;
            // set default values (needed because component is reused)
            this.$store.formChanged = false;    
            this.title = null;
            this.frugal = false;
            this.json = false;
            this.tableAPI = null;
            this.tableAPIKey = null;
            this.restAPI = null;
            this.dbFunction = null;
            this.rowActions = null;
            this.tableActions = null;
            this.isA = null;
            this.params = null;
            this.filter = {};
            this.filter2 = {};
            this.rows = [];
            this.selectedRows = [];
            this.changedRows = {};
            this.colAtts = {};
            this.masterKey = null;
            this.selection = "none";
            this.columns = [];
            this.loaded = false;
            this.contextValuesLocal = [];
            this.contextValues = [] ;
            this.data = [];
            this.exportPreprocess = null;
            this.noInlineEditing = false;
            this.lookupsLoaded = false;
            this.activeLookup = null;
            this.summary = null;
            this.summary_top = null;
            this.hideDefaultToolbar = false;
            this.hideRecordsToolbar = false;
            this.allowDelete = true;
            this.allowEdit = true;
            this.allowNew = true;
            this.props = {},
            this.index = 0,
            this.editedItem = null,
            this.lookupDisplayIndex = 0,
            this.overlayShown = null,
            this.overlays = { },
            this.asForm = false;
            this.inEdit = false;
            this.editingRowIndex = 0;
            this.contextValuesLoaded = false;
            this.singleRow = false;

            this.grid = this.$q.screen.width <= 800;

            if (this.options) { // embedded with options
                this.copyObject(this.options, this, true);
            } else if (this.parentPopup) { // called in popup
                this.copyObject(this.$store.newPopups[this.parentPopup.name].props, this, true);
            } else {
                this.copyObject(this.$store.props[this.$route.path], this, true);
            }

            if (this.parentPopup) {
                this.parentPopup.title = this.title;
            }
              
            let r = this.$store.routes.find(r => r.path == this.$route.path);
            this.read_only = r ? r.read_only : false;

            if (this.title) this.title = this.$t(this.title);

            if (!this.tableAPI || this.read_only) {
                this.allowDelete = false;
                this.allowEdit = false;
                this.allowNew = false;
            }

            await this.$nextTick();

            if (this.contextValues) {
                this.copyArray(this.contextValues, this.contextValuesLocal);
                for (let cv of this.contextValuesLocal) {
                    cv.value = null;
                    if ((cv.lookup?.minChars ?? 0) == 0) {
                        if (this.$q.localStorage.has("context_value_" + cv.name)) {
                            this.$store.contextValues[cv.name] = this.$q.localStorage.getItem("context_value_" + cv.name);
                            cv.value = this.$store.contextValues[cv.name].value;
                        }
                    }
                    if (cv.catalog) {
                        cv.options = this.$store.catalogs[cv.catalog];
                    }
                };
                this.contextValuesLoaded = true;
            }

            let routerRoute = this.$store.routes.filter((item) => item.path == this.$route.path)[0];
            this.offline = false;

            if (routerRoute) {
                this.offline = routerRoute.offline;
            }

            // get data from the server
            await this.reload();
            await this.$nextTick();
            this.loaded = true; 

            this.hasPdfReport = await this.getHasPdfReport();
        },

 
        /**
         * Wraps a value in a CSV cell.
         * @param {*} val - The value to be wrapped.
         * @param {*} formatFn - The function to format the value.
         * @param {*} row - The row containing the value.
         * @returns {string} The wrapped value.
        */
        wrapCsvValue (val, formatFn, row) {
            let formatted = formatFn !== void 0
                ? formatFn(val, row)
                : val
            formatted = formatted === void 0 || formatted === null ? '' : String(formatted)
            formatted = formatted.split('"').join('""')
            return `"${formatted}"`
        },
        
        /**
         * Creates content for export based on the provided export rows and columns.
         *
         * @param {Array} exportRows - The rows to be exported.
         * @param {Array} columns - The columns to be exported.
         * @returns {string} The content for export.
         */
        createContentForExport(exportRows, columns) {
            const content = [columns.map(col => this.wrapCsvValue(col.label.replace(/<[^>]*>/g, '')))].concat(
                exportRows.map(row => columns.map(col => this.wrapCsvValue(
                    typeof col.field === 'function'
                        ? col.field(row)
                        : row[col.field === void 0 ? col.name : col.field],
                    col.format,
                    row
                )).join(','))
            ).join('\r\n');
            return content;
        },	

        /**
         * Exports the tableAPI  to a CSV file.
         */
        exportTable() {
            let exportRows;

            if (this.selectedRows.length > 0) {
                exportRows = this.selectedRows;
            } else {
                exportRows = this.$refs.table.filteredSortedRows;
            }
            if (!exportRows || exportRows.length === 0) return;

            let content;

            if (this.exportPreprocess) {
                content = this[this.exportPreprocess].call(this, exportRows, this.columns)
            } else {
                content = this.createContentForExport(exportRows, this.columns);
            }

            const bom = '\uFEFF';
            const status = exportFile(
                this.$route.name + '.csv',
                bom + content,
                'text/csv'
            )

            if (status !== true) {
                $q.notify({
                    message: this.$t('Browser denied file download...'),
                    color: 'negative',
                    icon: 'warning'
                })
            }
                
        },

        /**
         * Replaces moustache variables in an object with corresponding values from row.
         * 
         * @param {Object} obj - The object containing variables to be replaced.
         * @param {Object} rowToPass - The object containing values to replace the variables.
         */
        replaceVariables(obj, rowToPass, rows) {
            for (let keyO in obj) {
                if (typeof obj[keyO] == "string" && (obj[keyO].startsWith("{{{"))) {
                    for (let keyR in rowToPass) {
                        if (obj[keyO] === "{{{" + keyR + "}}}") {
                            obj[keyO] = rowToPass[keyR];
                        }
                    }
                    for (let keyG in this.$store.globalValues) {
                        if (obj[keyO] === "{{{store.globalValues." + keyG + "}}}") {
                            obj[keyO] = this.$store.globalValues[keyG];
                        }
                    }
                } else if (typeof obj[keyO] == "string" && (obj[keyO].includes("{{"))) {
                    for (let keyR in rowToPass) {
                        obj[keyO] = obj[keyO].replaceAll("{{" + keyR + "}}", rowToPass[keyR]);
                    }
                    for (let keyG in this.$store.globalValues) {
                        obj[keyO] = obj[keyO].replaceAll("{{store.globalValues." + keyG + "}}", this.$store.globalValues[keyG]);
                    }
                    for (let keyCV in this.$store.contextValues) {
                        let cv = this.$store.contextValues[keyCV];                        
                        obj[keyO] = obj[keyO].replaceAll("{{store.contextValues.value." + keyCV + "}}", this.$store.contextValues[keyCV].value);
                        obj[keyO] = obj[keyO].replaceAll("{{store.contextValues.label." + keyCV + "}}", this.$store.contextValues[keyCV].label);
                    }
                } else if (typeof obj[keyO] == "object" && keyO != "rowActions") {
                    this.replaceVariables(obj[keyO], rowToPass, rows);
                } else if (typeof obj[keyO] == "string" && (obj[keyO].includes("[["))) {
                    // find all ggroups between [[ and ]] and replace them with values from rows
                    let matches = obj[keyO].match(/\[\[(.*?)\]\]/g);
                    if (matches) {
                        for (let match of matches) {
                            let key = match.substring(2, match.length - 2);
                            if (this.frugal) {
                                key = this.columns.find(c => c.name == key).index;
                            }
                            obj[keyO] = obj[keyO].replace(match, "[" + rows.map(r => r[key]).join(",") + "]");
                        }
                    }
                }
                if (keyO == "store.globalValues") {
                    this.copyObject(obj[keyO], this.$store.globalValues, true);    
                }
            }
        },
        
        /**
         * Deletes a row from the array in store, if necessary.
         * @param {Object} row - The row to be deleted.
         * @returns {void}
         */
         deleteInStore(row, action) {
            if (action.deleteInStore) {
                let index = this.$store[action.deleteInStore].findIndex(r => r.id == row.id);
                this.$store[action.deleteInStore].splice(index, 1);
            }
        },
         
        /**
         * Runs a row action.
         * @param {Object} action - Action to be run.
         * @param {Object} row - The row to be acted upon.
         */
        async runRowAction(action, row) { 
         
            let rowToPass = {};
            if (this.frugal) {
                rowToPass = this.rowToObject(row, this.columns);
            } else {
                rowToPass = row;
            }

            let a = this.deepClone(action);
            this.replaceVariables(a, rowToPass);
            
            this.editingRowIndex = this.rows.indexOf(row);

            if (a.confirmationMessage) {
                if (!(await this.confirmDialog(a.confirmationMessage))) {
                    return;
                }
            }

            if (a.conditionalConfirmationMessage) {
                let f = new Function('row', 'columns', a.conditionalConfirmationMessage.condition);
                if (f(row, this.columns)) {
                    if (!(await this.confirmDialog(a.conditionalConfirmationMessage.message))) {
                        return;
                    }
                };                
            }

            if (a.route) {
                if (this.$route) this.$store.state[this.$route.path] = this.deepClone(this.$data);
                this.prepareRoute(a, rowToPass, this.rows, this.columns, this.editingRowIndex);
            } else if (a.customFunction) {
                this[a.customFunction](rowToPass);
            } else if (a.restAPI) {

                let url = a.restAPI;

                let ret = await this.api(this.axios.API[a.method ?? "get"], url, a.params, this.offline);
                if (ret != null) {

                    if (a.method == "delete" && !a.noRowDelete) {
                        this.rows.splice(this.editingRowIndex, 1);
                        this.deleteInStore(row, a);
                    } else if (a.redirect) {
                        // open a new tab
                        window.open(ret);
                    } else if (ret && ret.taskId) {
                        this.$store.progress.props = { taskId: ret.taskId, min: 0, max: ret.count, parent: this, ...action };
                        this.$store.progress.show = true;
                    }
                }
            } else if (a.delete) {
                this.tableAPI = a.tableAPI;
                this.deleteRow(row, a.confirmationMessage);
                this.deleteInStore(row, a);
            } else if (a.clone) {
                await this.post("Table/Clone/" + a.clone + "/" + rowToPass[a.key]);
                await this.showMessage(this.$t("Record cloned. Table will be refreshed."));
                await this.reload(); 
            } else if (a.chart) {          
                this.chart(this.rows, a, row);
            } else {
                // activate a component
                this.initPopup(a, rowToPass, this.rows, this.columns, this.editingRowIndex);
            }

            if (a.reload) {
                await this.reload();
            }
        },

        /**
         * Runs a tableAPI  action (custom function).
         * @param {Object} action - Action to be run.
         */
        async runTableAction(action) {

            let rows;

            if (this.selection == "multiple") {
                if (action.mustSelectRows && this.selectedRows.length == 0) {
                    this.showMessage(action.mustSelectRowsMessage ? this.$t(action.mustSelectRowsMessage) : this.$t("Please select rows!"));
                    return;
                }
                if (this.selectedRows.length == 0) {
                    rows = this.$refs.table.filteredSortedRows;
                } else {
                    rows = this.selectedRows;
                }

            } else {
                rows = this.$refs.table.filteredSortedRows;
            }
            console.log("runTableAction", action, rows);

            if (action.confirmationMessage) {
                if (!await this.confirmDialog(action.confirmationMessage)) {
                    return;
                }
            }

            let a = this.deepClone(action);
            this.replaceVariables(a, {}, rows);

            if (a.customFunction) {
                await this[a.customFunction](rows);
            } else if (a.byRows) {  // run as row actions for each row
                this.$store.progress.props = { min: 0, max: rows.length };
                this.$store.progress.show = true;
                let i = 0;
                for (let row of rows) {
                    this.$store.progressValue = ++i;
                    let rowAction = this.deepClone(a);
                    rowAction.byRows = false;
                    rowAction.reload = false;
                    await this.runRowAction(rowAction, row);
                    if (this.$store.progress.aborted) break;
                }
                this.$store.progress.show = false;
            } else if (a.route) {
                this.$store.state[this.$route.path] = this.deepClone(this.$data);
                this.prepareRoute(a, null, rows, this.columns, this.editingRowIndex);
            } else if (a.component) {
                this.initPopup(a, null, rows, this.columns, this.editingRowIndex);
            } else if (a.restAPI) {
                let keys = rows.map(row => row[a.keyForKeys ?? this.key]);
                let ret = await this.api(this.axios.API[a.method ?? "get"], a.restAPI, { keys: keys, ...a.params });
                if (ret && ret.taskId) {
                    this.$store.progress.props = { taskId: ret.taskId, min: 0, max: ret.count, parent: this, ...a };
                    this.$store.progress.show = true;
                }
            } else if (a.chart) {
                this.chart(rows, a);
            }

            if (a.reload) {
                await this.reload();
            }
        },

        async chart(rows, action, row) {           
            let rowsToChart;
            let actionRetrievesData = action.chart.dbFunction || action.chart.restAPI;

            if (action.chart.dbFunction) {
                rowsToChart = await this.get("Table/GetTable", {
                    dbFunction: action.chart.dbFunction,
                    frugal: action.chart.frugal ? action.chart.frugal.toString() : "false",
                    json: action.chart.json ? action.chart.json.toString() : "false",
                    pars: JSON.stringify(action.chart.params) ?? "{}"
                });
            } else if (action.chart.restAPI) {
                rowsToChart = await this.get(action.chart.restAPI);
            } else if (action.chart.singleRow) {
                if (this.frugal) row = this.rowToObject(row, this.columns);
                let x = row[action.chart.labelField];
                let y = row[action.chart.valueField];
                rowsToChart = { row: row, x: x, y: y };
            } else {
                rowsToChart = rows;
            }
            
            if (rowsToChart) {
                let props = this.deepClone(action.chart);
                if (action.chart.preprocess) {
                    props.data = this[action.chart.preprocess].call(this, props, rowsToChart);
                } else if (this.frugal && !actionRetrievesData || actionRetrievesData && action.chart.frugal) {
                    props.data = rowsToChart.map(row => this.rowToObject(row, this.columns));
                } else {
                    props.data = rowsToChart;
                }
                this.initPopup({ component:'chart-popup', maximized : true, ...props });
            }
        },

        /**
         * Calculates the width of a column based on its type.
         * @param {string} type - The type of the column.
         * @returns {string} The width of the column.
         */
        calcWidth(type) {
            return this.colWidths[type] ?? '100px';
        },

        /**
         * Shows the row info in a popup.
         * @param {Object} row - The row to show info for.
         */
        showRowInfo(row, convert) {
            let rowToPass = row;
            if (this.frugal && convert) {
                rowToPass = this.rowToObject(row, this.columns);
            }
            this.initPopup({
                title: "Row info",
                component: 'row-info',
                tableAPIPropsKey: this.tableAPIPropsKey,
            }, rowToPass, null, this.columns);
        },
    }
}