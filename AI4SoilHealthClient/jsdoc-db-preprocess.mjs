/**
 * gets db schema and creates jsdoc compatible file
 */
import fs from 'fs';
import path from 'path';
import fetch from 'node-fetch';

function snakeToSentence(s) {
    return s.replace(/_/g, " ").replace(/\b\w/, l => l.toUpperCase());
}

const apiUrl = 'http://localhost:5012/api/Dev/'; 

let params = "";
if (process.argv.length > 2) {
    params = "?schemaName=" + process.argv[2] + "&procName=" + process.argv[3];
}

let s = "";
let r, tables, schemas, procs;
r = await fetch(apiUrl + 'GetDbProcedures' + params, { method: 'GET' });
procs = await r.json();

if (params == "") {
    // get db schema (tables, columns)

    r = await fetch(apiUrl + 'GetDbSchema', { method: 'GET' });
    let j = await r.json();
    tables = j.tables;
    schemas = j.schemas;


    // for distinct table_schema
    // let schemas = new Set();
    // for (let table of tables) {
    //     console.log(table.table_schema, table.table_name, table.columns.length);
    //     schemas.add(table.table_schema);
    // }
    // for (let proc of procs) {
    //     schemas.add(proc.proc_schema);
    // }

    for (let schema of schemas) {
        s += "/**\n * @namespace " + schema.schema_name + "\n"
            + "\n * @description " + schema.description + "\n" 
        + "*/\n";
    }
    
    for (let table of tables) {
        s += "\n/**\n";
        s += " * @class " + table.table_name + "\n";
        s += " * @memberof " + table.table_schema + "\n";
        s += " * @description " + (table.table_comment ?? snakeToSentence(table.table_name)) + "\n";
        for (let column of table.columns) {
            s += " * @property {" + column.data_type.replaceAll(" ", "_") + "} " + column.column_name
            if (column.column_comment) s += " " + column.column_comment
            if (column.referenced_schema_name) s += " foreign key to " + column.referenced_schema_name + "." + column.referenced_table_name + "." + column.referenced_column_name;
            s += "\n";
        }
        //s += " */\n";
        s += "*/\nclass " + table.table_name + " {}\n";
    }
    fs.writeFileSync("jsdoc-db-schema.js", s, { flag: 'w' });
}
// get db procedures


s = "";

for (let proc of procs) {
    // does proc.function_definition contain a /** ... */ block?
    // if so, extract it and use it as the jsdoc for the function
    // otherwise, create a jsdoc block from the proc data
    let jsdoc = proc.function_definition.match(/\/\*\*[\s\S]*?\*\//);
    let c = "";
    if (jsdoc) {
        c += jsdoc[0];
    } else {
        c += "\n/**\n";
        c += " * @function " + proc.proc_name + "\n";
        c += " * @memberof " + proc.proc_schema + "\n";
        c += " * @description " + (proc.proc_comment ?? snakeToSentence(proc.proc_name)) + "\n";
        let returns = [];
        for (let param of proc.params) {
            if (param.input_output == "t") {
                returns.push(param.param_name.replaceAll(" ", "_") + " " + param.data_type.replaceAll(" ", "_"));
            } else if (param.param_name) {
                c += " * @param {" + param.data_type.replaceAll(" ", "_") + "} " + param.param_name
                if (param.input_output == "o") s += " out";
                c += "\n";
            }
        }
        if (returns.length == 0) {
            c += " * @returns {" + proc.return_type.replaceAll(" ", "_") + "}\n";
        } else {
            //s += " * @returns {" + returns.join(", ") + "}\n";
            c += " * @returns {Object}";
            //c += "\n";
            let p = " ";
            for (let ret of returns) {
                if (p != " ") p + ", "; 
                p += ret.split(" ")[1] + " " + ret.split(" ")[0];
                //c += " * @property {" + ret.split(" ")[1] + "} " + ret.split(" ")[0] + "\n";
            }
            c += p + "\n";
        }
        c += " */";
        // insert c into proc.function_definition afert begin
        let i = proc.function_definition.toUpperCase().indexOf("BEGIN") + 5;
        proc.function_definition = proc.function_definition.slice(0, i) + "\n" + c + proc.function_definition.slice(i);
        await fetch(apiUrl + 'UpdateDbProcedure', { method: 'PUT', body: JSON.stringify({ definition: proc.function_definition }), headers: { 'Content-Type': 'application/json' } }  );
        //console.log("proc.function_definition", proc.function_definition);        
    }
    s += c + "\n";
    //s += "function " + proc.proc_name + "() {}\n";
    let code = proc.function_definition.replace(c, '');
    code = code.replaceAll("\r", "");
    code = code.replaceAll("\n\n\n", "\n\n");
    code = code.replaceAll("\n\n\n", "\n\n");
    s += "function " + proc.proc_name + "() {\n"
    s += "/*\n";
    s += code + "\n";
    s += "*/\n }\n";


    if (params != "") {
        console.log(proc);
        console.log(c);
        process.exit();
    }
}
fs.writeFileSync("jsdoc-db-procedures.js", s, {flag:'w'});
 