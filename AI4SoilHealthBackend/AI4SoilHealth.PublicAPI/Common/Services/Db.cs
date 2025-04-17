using System.Data;
using Npgsql;
using Dapper;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ExcelDataReader;
using PublicAPI.Common.Extensions;
using PublicAPI.Specific.Services;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;

namespace PublicAPI.Common.Services {

    /**
        * Database related operations   
        @module Db
*/

    public class Db {

        private readonly ILogger<Db> _logger;
        private readonly IDistributedCache _cache;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly AppOptions _options;

        public Db(ILogger<Db> logger, IDistributedCache cache, CurrentUserInfo currentUserInfo, IOptions<AppOptions> options) {
            _logger = logger;
            _cache = cache;
            _currentUserInfo = currentUserInfo;
            _options = options.Value;
        }

        /**
            * Create database context with current user and language
            * @param {object} sender - Sender
            * @param {StateChangeEventArgs} e - Event arguments
*/
        private void CreateContext(object sender, StateChangeEventArgs e) {
            if (e.CurrentState == ConnectionState.Open) {
                NpgsqlConnection conn = sender as NpgsqlConnection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "auth.create_public_context";
                cmd.CommandType = CommandType.StoredProcedure;
                //cmd.Parameters.Add(new NpgsqlParameter("PersonId", NpgsqlDbType.Integer) { Value = _currentUserInfo.PersonId });
                cmd.Parameters.AddWithValue("PersonId", _currentUserInfo.PersonId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("LangId", _currentUserInfo.LangId);
                cmd.Parameters.AddWithValue("IsAdmin", _currentUserInfo.IsAdmin);
                cmd.ExecuteNonQuery();
            }
        }

        /**
            * Create database connection
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @return {IDbConnection} - Connection
*/
        public IDbConnection CreateConnection(string connectionString, bool withContext = false) {
            var conn = new NpgsqlConnection(connectionString);
            if (withContext) {
                conn.StateChange += CreateContext;
            }
            return conn;
        }

        /**
            * Normalize procedure name
            * @param {string} procName - Procedure name
            * @return {string} - Normalized procedure name
        */
        public string NormalizeProcedureName(string procName) {
            return procName;
        }

        /**
            * Query json asynchroneously 
            * @param {string} cmd - Command returning json
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QueryJsonAsync(string cmd, object paramObject, string connectionString, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            string ret;
            try {
                using var connection = CreateConnection(connectionString, withContext);
                ret = (string)await connection.ExecuteScalarAsync(cmd, paramObject, commandType: cmdType);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query json asynchroneously with caching
            * @param {string} cmd - Command returning json
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {string} cacheType - Cache type
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
*/
        public async Task<string> QueryJsonAsyncCached(string cmd, object paramObject, string connectionString, string cacheType, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            string key = $"{cmd}_{JsonConvert.SerializeObject(paramObject)}";
            if (withContext) key += "_" + _currentUserInfo.PersonId.ToString() + "_" + _currentUserInfo.LangId.ToString();
            string ret = await _cache.GetAsync<string>(key);
            if (ret == null) {
                try {
                    using var connection = CreateConnection(connectionString, withContext);
                    ret = (string)await connection.ExecuteScalarAsync(cmd, paramObject, commandType: cmdType);
                    await _cache.SetAsync(key, ret, cacheType);
                } catch (Exception ex) {
                    ret = Helper.HandleException(_logger, ex);
                }
            }
            return ret;
        }

        /**
            * Execute asynchroneously
            * @param {string} cmd - Command
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
*/
        public async Task<string> ExecuteAsync(string cmd, object paramObject, string connectionString, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            string ret = "";
            try {
                using var connection = CreateConnection(connectionString, withContext);
                //await connection.ExecuteAsync("delete from meta.route where id = @Id", new { Id = 224 }, commandType: CommandType.Text);
                await connection.ExecuteAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query asynchroneously
            * @param {string} cmd - Command returning table
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {bool} multiple - Multiple
            * @param {bool} camelCase - Camel case
            * @param {HashSet<string>} keys - Keys
            * @param {string} keyExpr - Key expression
            * @param {string[]} tableNames - Table names
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QueryAsync(string cmd, object paramObject, string connectionString, bool withContext = false, bool multiple = false, bool camelCase = false, HashSet<string> keys = null, string keyExpr = null, string[] tableNames = null, CommandType cmdType = CommandType.StoredProcedure) {
            string ret;
            try {
                using var connection = CreateConnection(connectionString, withContext);
                using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                ret = Helper.SerializeReader(reader, multiple, camelCase, keys, keyExpr, tableNames);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query asynchroneously
            * @param {string} cmd - Command returning frugal table (optimized for large tables)
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {bool} multiple - Multiple
            * @param {bool} camelCase - Camel case
            * @param {HashSet<string>} keys - Keys
            * @param {string} keyExpr - Key expression
            * @param {string[]} tableNames - Table names
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
*/
        public async Task<string> QueryAsyncFrugal(string cmd, object paramObject, string connectionString, bool withContext = false, bool multiple = false, bool camelCase = false, HashSet<string> keys = null, string keyExpr = null, string[] tableNames = null, CommandType cmdType = CommandType.StoredProcedure) {
            string ret;
            try {
                using var connection = CreateConnection(connectionString, withContext);
                using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                ret = Helper.SerializeReaderFrugal(reader);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query asynchroneously with caching
            * @param {string} cmd - Command returning frugal table (optimized for large tables)
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {string} cacheType - Cache type
            * @param {bool} withContext - With context
            * @param {bool} multiple - Multiple
            * @param {bool} camelCase - Camel case
            * @param {HashSet<string>} keys - Keys
            * @param {string} keyExpr - Key expression
            * @param {string[]} tableNames - Table names
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
*/
        public async Task<string> QueryAsyncFrugalCached(string cmd, object paramObject, string connectionString, string cacheType, bool withContext = false, bool multiple = false, bool camelCase = false, HashSet<string> keys = null, string keyExpr = null, string[] tableNames = null, CommandType cmdType = CommandType.StoredProcedure) {
            string key = $"{cmd}_{JsonConvert.SerializeObject(paramObject)}";
            if (withContext) key += "_" + _currentUserInfo.PersonId.ToString() + "_" + _currentUserInfo.LangId.ToString();
            string ret = await _cache.GetAsync<string>(key);
            if (ret == null) {
                try {
                    using var connection = CreateConnection(connectionString, withContext);
                    using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                    ret = Helper.SerializeReaderFrugal(reader);
                    await _cache.SetAsync(key, ret, cacheType);
                } catch (Exception ex) {
                    ret = Helper.HandleException(_logger, ex);
                }
            }
            return ret;
        }

        /**
            * Query asynchroneously with caching
            * @param {string} cmd - Command returning table
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {string} cacheType - Cache type
            * @param {bool} withContext - With context
            * @param {bool} multiple - Multiple
            * @param {bool} camelCase - Camel case
            * @param {HashSet<string>} keys - Keys
            * @param {string} keyExpr - Key expression
            * @param {string[]} tableNames - Table names
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
*/
        public async Task<string> QueryAsyncCached(string cmd, object paramObject, string connectionString, string cacheType, bool withContext = false, bool multiple = false, bool camelCase = false, HashSet<string> keys = null, string keyExpr = null, string[] tableNames = null, CommandType cmdType = CommandType.StoredProcedure) {
            string key = $"{cmd}_{JsonConvert.SerializeObject(paramObject)}";
            if (withContext) key += "_" + _currentUserInfo.PersonId.ToString() + "_" + _currentUserInfo.LangId.ToString();
            string ret = await _cache.GetAsync<string>(key);
            if (ret == null) {
                try {
                    using var connection = CreateConnection(connectionString, withContext);
                    using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                    ret = Helper.SerializeReader(reader, multiple, camelCase, keys, keyExpr, tableNames);
                    await _cache.SetAsync(key, ret, cacheType);
                } catch (Exception ex) {
                    ret = Helper.HandleException(_logger, ex);
                }
            }
            return ret;
        }

        /**
            * Query byte array asynchroneously
            * @param {string} cmd - Command returning byte array
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<byte[]>} - Result of the operation
        */
        public async Task<byte[]> QueryByteAsync(string cmd, object paramObject, string connectionString, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            using var connection = CreateConnection(connectionString, withContext);
            var scalar = await connection.ExecuteScalarAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
            return (byte[])scalar;
        }

        /**
            * Query table asynchroneously
            * @param {string} cmd - Command returning reader
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<DataTable>} - Result of the operation
        */
        public async Task<DataTable> QueryTableAsync(string cmd, object paramObject, string connectionString, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            using var connection = CreateConnection(connectionString, withContext);
            var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
            DataTable ret = new DataTable();
            ret.Load(reader);
            return ret;
        }

        /**
            * Query scalar asynchroneously cached
            * @param {string} cmd - Command returning scalar
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {string} cacheType - type of cache e.g. long, short etc.
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QueryScalarAsyncCached(string cmd, object paramObject, string connectionString, string cacheType, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            string key = $"{cmd}_{JsonConvert.SerializeObject(paramObject)}";
            if (withContext) key += "_" + _currentUserInfo.PersonId.ToString() + "_" + _currentUserInfo.LangId.ToString();
            string ret = await _cache.GetAsync<string>(key);
            if (ret == null) {
                try {
                    using var connection = CreateConnection(connectionString, withContext);
                    var scalar = await connection.ExecuteScalarAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                    ret = (scalar ?? "").ToString(); // JsonConvert.SerializeObject(scalar);
                    await _cache.SetAsync(key, ret, cacheType);
                } catch (Exception ex) {
                    ret = Helper.HandleException(_logger, ex);
                }
            }
            return ret;
        }


        /**
            * Query scalar asynchroneously 
            * @param {string} cmd - Command returning scalar
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QueryScalarAsync(string cmd, object paramObject, string connectionString, bool withContext = false, CommandType cmdType = CommandType.StoredProcedure) {
            string ret;
            try {
                using var connection = CreateConnection(connectionString, withContext);
                var scalar = await connection.ExecuteScalarAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                ret = (scalar ?? "").ToString(); // JsonConvert.SerializeObject(scalar);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query single row asynchroneously 
            * @param {string} cmd - Command returning single row
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QuerySingleAsync(string cmd, object paramObject, string connectionString, bool withContext = false, bool camelCase = false, CommandType cmdType = CommandType.StoredProcedure, bool returnAttributesIfEmpty = false) {
            string ret;
            try {
                using var connection = CreateConnection(connectionString, withContext);
                using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                ret = Helper.SerializeSingle(reader, camelCase, returnAttributesIfEmpty);
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
            * Query single row asynchroneously cached
            * @param {string} cmd - Command returning single row
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {string} cacheType - type of cache e.g. long, short etc.
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<string>} - Result of the operation
        */
        public async Task<string> QuerySingleAsyncCached(string cmd, object paramObject, string connectionString, string cacheType, bool withContext = false, bool camelCase = false, CommandType cmdType = CommandType.StoredProcedure, bool returnAttributesIfEmpty = false) {
            string key = $"{cmd}_{JsonConvert.SerializeObject(paramObject)}";
            if (withContext) key += "_" + _currentUserInfo.PersonId.ToString() + "_" + _currentUserInfo.LangId.ToString();
            string ret = await _cache.GetAsync<string>(key);
            if (ret == null) {
                try {
                    using var connection = CreateConnection(connectionString, withContext);
                    using var reader = await connection.ExecuteReaderAsync(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
                    ret = Helper.SerializeSingle(reader, camelCase, returnAttributesIfEmpty);
                    await _cache.SetAsync(key, ret, cacheType);
                } catch (Exception ex) {
                    ret = Helper.HandleException(_logger, ex);
                }
            }
            return ret;
        }

        /**
            * Query single object of type T asyncronously
            * @param {string} cmd - Command returning single row compatible with T. Must return TABLE.
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<T>} - Result of the operation
        */
        public async Task<T> QuerySingleAsync<T>(string cmd, object paramObject, string connectionString, bool withContext = false, bool camelCase = false, CommandType cmdType = CommandType.StoredProcedure) {
            using var connection = CreateConnection(connectionString, withContext);
            connection.Open();
            var ret = await connection.QuerySingleAsync<T>(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
            return ret;
        }

        /**
            * Query list of type T asyncronously
            * @param {string} cmd - Command returning recordset compatible with T. Must return TABLE.
            * @param {object} paramObject - Parameters
            * @param {string} connectionString - Connection string
            * @param {bool} withContext - With context
            * @param {CommandType} cmdType - Command type
            * @return {Task<T>} - Result of the operation
        */
        public async Task<IEnumerable<List<T>>> QueryAsync<T>(string cmd, object paramObject, string connectionString, bool withContext = false, bool camelCase = false, CommandType cmdType = CommandType.StoredProcedure) {
            using var connection = CreateConnection(connectionString, withContext);
            connection.Open();
            var ret = await connection.QueryAsync<List<T>>(NormalizeProcedureName(cmd), paramObject, commandType: cmdType);
            return ret;
        }

        public async Task<List<T>> QueryListAsync<T>(string cmd, object paramObject, string connectionString, bool withContext = false, bool camelCase = false, CommandType cmdType = CommandType.StoredProcedure) {
            using var connection = CreateConnection(connectionString, withContext);
            connection.Open();
            var ret = (await connection.QueryAsync<T>(NormalizeProcedureName(cmd), paramObject, commandType: cmdType)).AsList();
            return ret;
        }

        /**
         * Check if the function can be safely called
         * @param {string} procName - Name of the function
         * @param {string} connectionString - Connection string
         * @return {Task<string>} - Result of the operation
         */
        public async Task<string> IsFunctionSafe(string procName, string connectionString) {
            using var connection = CreateConnection(connectionString);
            var ret = await QueryScalarAsyncCached("meta.is_function_safe", new { Function = procName }, connectionString, "long");
            return ret;
        }

        /**
            * Imports excel file into database
            * @param {string} filename - Name of the file
            * @param {string} sheet - Name of the sheet (can be null)
            * @param {string} preprocess - Name of the function that preprocesses the data
            * @param {Stream} file - Data stream
            * @param {string} procName - procedure what handles JSON data 
            * @param {string} pars - function parameters in JSON format, 
            * @param {string} connectionString 
            * @param {bool} withContext - With context
            * @return {Task<string>} - Status of the operation
        */
        public async Task<string> ImportExcel(string filename, Stream file, string pars, string connectionString, bool withContext = false) {
            
            var p = JsonConvert.DeserializeObject<Dictionary<string, string>>(pars);
            string sheet = null, preprocess = null, procName = null, analyze = null;
            
            p.TryGetValue("sheet", out sheet);
            p.TryGetValue("preprocess", out preprocess);
            p.TryGetValue("proc_name", out procName);
            p.TryGetValue("analyze", out analyze);
           
            if (procName != null && await IsFunctionSafe(procName, connectionString) != "True") return Helper.CreateError(_logger, "Unauthorized function " + procName);

            IExcelDataReader reader = null;
            DataTableReader r = null;

            try {
                
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                if (filename.EndsWith(".csv")) {
                    reader = ExcelReaderFactory.CreateCsvReader(file);
                } else {
                    reader = ExcelReaderFactory.CreateReader(file);
                }
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                DataTable t = null;
                if (sheet != "null" && sheet != null) {
                    t = ds.Tables[sheet];
                }
                if (t == null) {
                    t = ds.Tables[0];
                }

                string jsonColumns = "";

                if (analyze != "true") {  // real import
                    
                    r = t.CreateDataReader();
                    var jsonData = Helper.SerializeReader(r, excelReader: false);
                    PreprocessService.Preprocess(preprocess, ref jsonData, ref jsonColumns);
                    jsonData = jsonData.Replace("[]", "");
                    return await ExecuteAsync(procName, new { Params = pars, Json = jsonData }, connectionString, withContext);
                
                } else {  // analyze - create columns in import_detail

                    if (preprocess != "null") {
                        r = t.CreateDataReader();
                        var jsonData = Helper.SerializeReader(r, excelReader: false);
                        PreprocessService.Preprocess(preprocess, ref jsonData, ref jsonColumns);
                    } else { 
                        var stream = new StringWriter();
                        using var writer = new JsonTextWriter(stream);
                        writer.WriteStartArray();
                        foreach (DataColumn c in t.Columns) {
                            writer.WriteStartObject();
                            writer.WritePropertyName("col_name");
                            writer.WriteValue(c.ColumnName);
                            writer.WritePropertyName("col_type");
                            writer.WriteValue(c.DataType.ToString());
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();
                        jsonColumns = stream.ToString();
                    }

                    using var connection = CreateConnection(connectionString, withContext);
                    var ret = await ExecuteAsync("meta.analyze_file",
                    new { Id = Int32.Parse(p["id"]), JsonData = jsonColumns }, connectionString, withContext);
                    return ret;
                }
            } catch (Exception ex) {
                return Helper.CreateError(_logger, ex.Message);
            } finally {
                reader.Close();
                r.Close();

            }
        }
    }
}
