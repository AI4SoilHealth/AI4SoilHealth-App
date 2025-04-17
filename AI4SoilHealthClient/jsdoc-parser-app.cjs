/**
 * jsdoc plugin for parsing Vue SFC files
 * @module jsdoc-parser-vue
 * @example
 * jsdoc -c jsdoc-vue.json
*/
const { parse } = require('@vue/compiler-sfc');
const path = require('path');

let scriptStart;
let vueFile;

exports.handlers = {
    beforeParse(event) {
        const { filename, source } = event;
        if (path.extname(filename) === '.vue') {
            vueFile = true;
            const { descriptor } = parse(source);
            event.source = descriptor.script ? descriptor.script.content : '';
            // in event.source, find number of line which starts with <script
            let i = 0;
            for (line of source.split(/\r?\n/)) {
                if (line.indexOf("<script") >= 0) {
                    scriptStart = i;
                    break;
                }
                i++;
            }
        } else {
            vueFile = false;
        }
    },
    newDoclet(e) {
        const componentName = e.doclet.meta.filename.replace(/\.(vue|js)$/, '');
       // console.log(e.doclet);
        const module = `module:${componentName}`;
        //console.log("module", module, "name", e.doclet.name, "longname", e.doclet.longname, "memberof", e.doclet.memberof)
        // The main doclet before `export default {}`
        // if (e.doclet.longname === 'module.exports' ||
        //     (!vueFile && !e.doclet.memberof && !e.doclet.undocumented)) {
        //    // console.log(e.doclet)
        //     e.doclet.kind = 'module';
        //     e.doclet.name = componentName;
        //     e.doclet.alias = componentName;
        //     e.doclet.longname = componentName;  //module;
        //     e.doclet.memberof = null;
        //     if (vueFile) {
        //         e.doclet.meta.lineno = e.doclet.meta.lineno + scriptStart;
        //     }
        //     if (e.doclet.undocumented) {
        //         e.doclet.description = "Module " + componentName;
        //         e.doclet.undocumented = false;
        //     }
        //     return;
        // }
        if (e.doclet.memberof) {
            if (e.doclet.memberof.endsWith(".watch")) {
                e.doclet.scope = 'watcher';
            } else if (e.doclet.memberof.endsWith(".computed")) {
                e.doclet.scope = 'computed';
            } else if (e.doclet.memberof.endsWith(".methods")) {
                e.doclet.scope = 'method';
            // } else if (e.doclet.memberof.endsWith(componentName)) {
            //     e.doclet.scope = 'static';
            } else if (e.doclet.kind == 'function') {
                e.doclet.scope = e.doclet.kind;
            } else {
                //console.log("ommit", e.doclet.memberof, e.doclet.kind)
                delete e.doclet.meta;
                return;
            }
            if (vueFile) {
                e.doclet.meta.lineno = e.doclet.meta.lineno + scriptStart;
            }
            if (e.doclet.undocumented) {
                e.doclet.description = "Method " + e.doclet.name;
                e.doclet.undocumented = false;
            }
            e.doclet.memberof = componentName;  // module;
            //if (e.doclet.descriptor)
            //console.log("add", e.doclet.name, e.doclet.kind, e.doclet.memberof);
            e.doclet.kind = 'function';

        } else {
            //delete e.doclet.meta;
            e.doclet.kind = 'module';
            e.doclet.name = componentName;
            e.doclet.alias = componentName;
            e.doclet.longname = componentName;  //module;
            e.doclet.memberof = null;
            return;
        }
    }
};
