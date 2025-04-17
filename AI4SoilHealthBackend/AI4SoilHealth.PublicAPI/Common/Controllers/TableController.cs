using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using System.Buffers;
using System.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PublicAPI.Common.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    /**
        * Table controller (methods for getting tables, lookups and CRUD operations)
        @module TableController
*/
    public class TableController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TableController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;
        public TableController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<TableController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
            * Add a row to a table
            * @return {ActionResult} - Result of the operation
        */
        [HttpPost("{table}")]
        public async Task<ActionResult> AddRow(string table) {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryScalarAsync("zzglc." + table + "_c", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        /**
            * Update a row in a table
            * @return {ActionResult} - Result of the operation
        */
        [HttpPut("{table}")]
        public async Task<ActionResult> UpdateRow(string table) {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryScalarAsync("zzglc." + table + "_u", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        /**
            * Get table
            * @return {ActionResult} - Result of the operation
        */
        [HttpGet("{table}")]
        public async Task<ActionResult> GetRows(string table, string pars) {
            object p = null;
            if (pars != null && pars != "{}") {
                p = new { Params = pars };
            }
            var ret = await _db.QueryAsyncFrugal("zzglc." + table + "_r", p, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
        * Get table
        * @return {ActionResult} - Result of the operation
        */
        [HttpGet("{table}/{key}/{json?}/{single?}")]
        public async Task<ActionResult> GetRows(string table, string key, bool? json, bool? single) {
            int? id = null;
            if (key != "null") {
                id = int.Parse(key);
            }
            if (single == null) single = false;
            if (json != null && (bool)json) {
                //var ret = await _db.QueryJsonAsync("zzglc." + table + ((bool) single ? "_s" : "") + "_r", new { Key = id }, _options.ConnectionString, true);
                var ret = await _db.QueryJsonAsync("zzglc." + table + "_r", new { Key = id }, _options.ConnectionString, true);
                return Ok(ret);
            } else {
                if (single == null) single = false;
                //var ret = await _db.QueryAsyncFrugal("zzglc." + table + ((bool)single ? "_s" : "") + "_r", new { Key = id }, _options.ConnectionString, true);
                var ret = await _db.QueryAsyncFrugal("zzglc." + table + "_r", new { Key = id }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        /**
            * Delete a row from a table
            * @param {string} table - Table
            * @param {int} key - Key
            * @return {ActionResult} - Result of the operation
        */
        [HttpDelete("{table}/{key}")]
        public async Task<ActionResult> DeleteRow(string table, int key) {
            var ret = await _db.QueryScalarAsync("zzglc." + table + "_d", new { Key = key }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
            * Delete rows from a table
            * @param {string} table - Table
            * @param {Dictionary<string, object>} pars - Parameters, must contain keys
            * @return {ActionResult} - Result of the operation
        */
        [HttpPut("DeleteRows/{table}")]
        public async Task<ActionResult> DeleteRows(string table, [FromBody] Dictionary<string, object> pars) {
            int[] keys = JsonConvert.DeserializeObject<int[]>(pars["keys"].ToString());
            var ret = await _db.QueryScalarAsync("zzglc." + table + "_d", new { Keys = keys }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
            * Get a lookup for a foreign key
            * @param {Dictionary<string, string>} pars - Function
            * @return {ActionResult} - Lookup
        */
        [HttpGet("GetLookup/{table}")]
        public async Task<ActionResult> GetLookup(string table, string searchValue) {
    
            var ret = await _db.QueryAsyncCached("zzgll." + table + "_l", new { SearchValue = (searchValue ?? "") + "%", Key = (int?)null }, _options.ConnectionString, "long", true);
            return Ok(ret);
        }

        /**
            * Get a lookup for a parameter
            * @param {Dictionary<string, string>} pars - Function
            * @return {ActionResult} - Lookup
        */
        [HttpGet("GetParamLookup/{lookup}")]
        public async Task<ActionResult> GetParamLookup(string lookup, string searchValue) {
            var ret = await _db.QueryAsyncCached(lookup, new { SearchValue = (searchValue ?? "") + "%", Key = (int?)null }, _options.ConnectionString, "long", true);
            return Ok(ret);
        }

        /**
            * Get a generic table
            * @param {Dictionary<string, string>} pars - dbFunction, frugal, json
            * @return {ActionResult} - Table
        */
        [AllowAnonymous]
        [HttpGet("GetTable")]
        public async Task<ActionResult> GetTable(string dbFunction, bool frugal, bool json, string pars, string preprocess) {
            string safe = "True";
            if (!dbFunction.StartsWith("zzglc.")) {
                safe = await _db.QueryScalarAsyncCached("meta.is_function_safe", new { Function = dbFunction }, _options.ConnectionString, "long", true);
            }
            if (safe != "True") {
                return Ok(Helper.CreateError(_logger, dbFunction + " unauthorized " + safe));
            }
            string ret, jsonColumns = null;
            if (dbFunction.StartsWith("zzglc.")) {
                ret = await _db.QueryAsyncFrugalCached(dbFunction, null, _options.ConnectionString, "long", true);
            } else if (frugal) {
                ret = await _db.QueryAsyncFrugalCached(dbFunction, new { Params = pars, _currentUserInfo.PersonId, _currentUserInfo.LangId }, _options.ConnectionString, "long", true);
            } else if (json) {
                ret = await _db.QueryJsonAsyncCached(dbFunction, new { Params = pars, _currentUserInfo.PersonId, _currentUserInfo.LangId }, _options.ConnectionString, "long", true);
            } else {
                ret = await _db.QueryAsyncCached(dbFunction, new { Params = pars, _currentUserInfo.PersonId, _currentUserInfo.LangId }, _options.ConnectionString, "long", true);
            }
            PreprocessService.Preprocess(preprocess, ref ret, ref jsonColumns);
            return Ok(ret);
        }

        /**
            * Clones a row of a table
            * @return {ActionResult} - Result of the operation
        */
        [HttpPost("Clone/{table}/{id}")]
        public async Task<ActionResult> Clone(string table, int id) {
            var ret = await _db.QueryScalarAsync("meta." + table + "_clone", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
         * Executes a function with discrete params
         * @return {ActionResult} 
         */
        [HttpPost("Exec/{dbFunction}")]
        public async Task<ActionResult> Exec(string dbFunction, [FromBody] Dictionary<string, object> pars) {

            var safe = await _db.QueryScalarAsyncCached("meta.is_function_safe", new { Function = dbFunction }, _options.ConnectionString, "long", true);
            if (safe != "True") {
                return Ok(Helper.CreateError(_logger, dbFunction + " unauthorized " + safe));
            }

            var paramObject = new ExpandoObject() as IDictionary<string, object>;
            
            foreach (var kvp in pars) {
                string key = kvp.Key;
                var v = JObject.Parse(kvp.Value.ToString());
                var value = v["value"].ToString();
                var type = v["type"].ToString();
                if (value == "null") {
                    paramObject[kvp.Key] = null;
                } else {
                    if (type == "int") {
                        paramObject[kvp.Key] = Int32.Parse(value);
                    } else if (type == "string") {
                        paramObject[kvp.Key] = value;
                    } else {
                        return Ok(Helper.CreateError(_logger, "Unsupported type: " + type));
                    }
                }      
            }
            
            var ret = await _db.QueryScalarAsync(dbFunction, paramObject, _options.ConnectionString, true);
            return Ok(ret);
        }
    }
}
