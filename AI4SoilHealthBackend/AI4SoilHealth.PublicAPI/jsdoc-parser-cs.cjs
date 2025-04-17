// jsdoc plugin for parsing C# files

const path = require('path');
const functions = {};
exports.handlers = {
    beforeParse(event) {
        const { filename, source } = event;
        // iterate source line by line
        let i = 0;
        let commentsCount = 0;
        let start = 0, end = 0;
        let lines = source.split(/\r?\n/);
        let anonymous = false;
        let http = null;
        let regex;
        let match;
        event.source = '';
        for (i = 0; i < lines.length;  i++) {
            if (lines[i].indexOf('/**') > -1) {
                start = i;
                anonymous = false;
                http = null;
            } else if (lines[i].indexOf('*/') > -1) {
                end = i;
                commentsCount++;
                for (let j = i + 1; j < lines.length; j++) {
                    if (lines[j].indexOf('AllowAnonymous') > -1) {
                        anonymous = true;
                    }
                    regex = /\[(HttpGet|HttpPost|HttpPut|HttpDelete)/;
                    match = regex.exec(lines[j]);
                    if (match) {
                        http = match[1];
                        continue;
                    }   
                    if (lines[j].indexOf('(') > -1 || lines[j].indexOf('{') > -1) {
                        // find last word before '(' or '{'
                        regex = /(\w+)\s*(\(|\{)/g;
                        match = regex.exec(lines[j]);
                        //console.log(match[1]);
                        event.source += lines.slice(start, end+1).join('\n');
                        functions[match[1]] = {};
                        functions[match[1]].lineno = j + 1;
                        functions[match[1]].http = http;
                        functions[match[1]].anonymous = anonymous;
                        functions[match[1]].method = lines[j].indexOf('(') > -1;
                        if (commentsCount > 1) {
                            if (functions[match[1]].method)
                                event.source += '\nfunction ' + match[1] + '(){}\n';
                            else
                                event.source += '\nclass ' + match[1] + '{}\n';
                        }
                        //console.log("newmatch[1], anonymous, http);
                        break;
                    }
                }
            }
        }
    },
    newDoclet(e) {
        const componentName = e.doclet.meta.filename.replace(/\.cs$/, '');
        if (e.doclet.kind == "module") {
            e.doclet.name = componentName;
            e.doclet.alias = componentName;
            e.doclet.longname = componentName;
        } else {
            e.doclet.scope = functions[e.doclet.name].method ? 'method' : 'class';
            if (functions[e.doclet.name].http)  e.doclet.scope += ' ' + functions[e.doclet.name].http;
            if (functions[e.doclet.name].anonymous) e.doclet.scope += ' anonymous';
            e.doclet.memberof = componentName;
            e.doclet.meta.lineno = functions[e.doclet.name].lineno;
        }
    }
};
