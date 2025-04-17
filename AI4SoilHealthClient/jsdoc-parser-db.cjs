/**
 * jsdoc plugin for parsing pg database
 * @module jsdoc-parser-pg
 * @example
 * jsdoc -c jsdoc-pg.json
*/
const apiUrl = 'http://localhost:5012/api/Dev/GetDbSchema'; 

exports.handlers = {
    async beforeParse(e) {
    },
    newDoclet(e) {
        // if (e.doclet.kind === 'class') {
        //     e.doclet.kind = 'class';
        //     let i = e.doclet.name.indexOf('_');
        //     if (i > 0) {
        //         let name = e.doclet.name.substring(i + 1);
        //         let memberof = e.doclet.name.substring(0, i);
        //         e.doclet.name = name;
        //         e.doclet.memberof = memberof;
        //         console.log(e.doclet.memberof, e.doclet.name);
        //     }
        // }
    }
};
