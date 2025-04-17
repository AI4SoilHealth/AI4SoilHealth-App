/**
 * @desc A mixin object containing methods for data preprocessing before download.
 * @module TableCustomMixin
 */
export const TableExportMixin = {
    methods: {       
        /**
         * @desc Converts the spectra table data for download to a CSV file.
         * @param {Array} rows - The table rows.
         * @param {Array} columns - The table columns. 
         */
        spectraTable(rowsOrig, columns) {
            
            if (rowsOrig.length == 0) return "";

            let rows = rowsOrig;
            if (this.frugal) {
                rows = rowsOrig.map(row => this.rowToObject(row, this.columns));
            }


            let owl = {};
            if (rows[0].wavelengths) {
                // collect all wavelengths (covers case when some rows have different wavelengths)
                for (let row of rows) {
                    if (row.wavelengths) {
                        for (let wl of row.wavelengths) {
                            if (!owl[wl]) {
                                owl[wl] = 0;
                            }
                        }
                    }
                }
            }

            // assign indices to wavelengths
            let i = 0;
            let cwl = "";
            for (let key of Object.keys(owl).sort((a, b) => a - b)) {
                owl[key] = i++;
                cwl += '"' + key + '",';
            }

            // create the header row
            let content = "";
            for (let c of columns) {
                if (c.name != "wavelengths" && c.name != "values") {
                    content += '"' + c.label + '",';
                }
            }
        
            content += cwl + "\r\n";

            // create the content rows
            for (let row of rows) {

                // create other row columns
                let cr = "";
                for (let c of columns) {
                    if (c.name != "wavelengths" && c.name != "values") {
                        if (c.type == "string" && c.name == "date") {
                            cr += '"' + (row[c.name] ?? "") + '",';
                        } else {
                            cr += (row[c.name] ?? "") + ",";
                        }
                    }
                }
                content += cr;

                if (row.wavelengths) {
                    // create the values
                    let v = [];
                    for (let i = 0; i < row.wavelengths.length; i++) {
                        v[owl[row.wavelengths[i]]] = row.values[i];
                    }
                    content += v.join(",");
                }

                content += "\r\n";
            }
        
            return content;
        }
    }
};