/**
 * @desc A mixin object containing custom methods for the table component.
 * @module TableCustomMixin
 */
export const TableCustomMixin = {
    methods: {
        preprocessXY(props, rows) {
            let indicator_id = this.$store.contextValues.indicator_id.value;
            if (this.$store.catalogs.descriptions[indicator_id]) {
                props.numerical = false;
                props.statInRow = false;
                let r = this.deepClone(rows);
                for(let i = 0; i < r.x.length; i++){
                    r.x[i] = this.$store.catalogs.descriptions[indicator_id][r.x[i]].name;
                };
                return r; // Return the modified rows
            } else {
                return rows; // Return the original rows if the indicator ID is not found
            }
        },
        /**
         * Converts the given rows containing arrays of wawlengths and corresponding values into a standardized format for chart data.
         * @param {Array} rows - The rows to convert.
         * @returns {Array} - The converted chart data.
         */
        wavelengths4chart(props, rows) {
            let data = [];
            if (this.frugal) {
                for (let row of rows) {
                    for (let i = 0; i < row[3].length; i++) {
                        let obj = {};
                        obj.name = row[0];
                        obj.date = row[1];
                        obj.wavelength = row[2][i];
                        obj.value = row[3][i];
                        data.push(obj);
                    }
                }
            } else {
                for (let row of rows) {
                    for (let i = 0; i < row.values.length; i++) {
                        let obj = {};
                        obj.name = row.name + " " + row.date;
                        //obj.date = row.date;
                        obj.wavelength = row.wavelengths[i];
                        obj.value = row.values[i];
                        data.push(obj);
                    }
                }
            }
            return data;
        }
    }
};