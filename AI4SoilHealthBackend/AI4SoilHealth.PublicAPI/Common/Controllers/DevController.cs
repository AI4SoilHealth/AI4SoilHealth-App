using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Data;
using System.Diagnostics;
using PublicAPI.Common.Services;
using PublicAPI.Common.Extensions;

namespace PublicAPI.Common.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * Dev controller - methods for development
        @module DevController
*/
    [AllowAnonymous]
    public class DevController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DevController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;


        public DevController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<DevController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
        *Add i18n keys (called from dev script which finds all keys in the code and adds them to the database)
        *@param {Dictionary<string, string>} d - Keys
        *@return {ActionResult} - Result of the operation
        */
        [HttpPost("AddI18NKeys")]
        public async Task<ActionResult> AddI18NKeys([FromBody] Dictionary<string, string> d) {
            if (!Debugger.IsAttached) {
                return Unauthorized();
            }
            await _db.ExecuteAsync("meta.add_i18n_keys", new { Keys = d["keys"] }, _options.ConnectionString);
            return Ok();
        }

        /**
         * Gets entire schema for documentation purposes
         * @return {ActionResult} - Schema in JSON
         */
        [HttpGet("GetDbSchema")]
        public async Task<ActionResult> GetDbSchema() {
            if (!Debugger.IsAttached) {
                return Unauthorized();
            }
            var ret = await _db.QueryJsonAsync("meta.get_db_schema", null, _options.ConnectionString);
            return Ok(ret);
        }

        /**
         * Gets all db procedures for documentation purposes
         * @return {ActionResult} - Procedures in JSON
         */
        [HttpGet("GetDbProcedures")]
        public async Task<ActionResult> GetDbProcedures(string schemaName, string procName) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.QueryJsonAsync("meta.get_db_procedures", new { SchemaName = schemaName, ProcName = procName }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpGet("GetSettings")]
        public async Task<ActionResult> GetSettings(string path) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.QueryAsync("SELECT * FROM meta.route WHERE path=@Path", new { Path = path }, _options.ConnectionString, cmdType:CommandType.Text);
            return Ok(ret);
        }

        /**
         * Adds JSON documentation to a procedure
         * @return {ActionResult} - OK or error
         */
        [HttpPut("UpdateDbProcedure")]
        public async Task<ActionResult> UpdateDbProcedure(Dictionary<string, string> d) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.QueryJsonAsync(d["definition"], null, _options.ConnectionString, cmdType: CommandType.Text);
            return Ok(ret);
        }

        /**
        * Creates CRUD procedures for a given table
        * @return {ActionResult} - OK or error
        */
        [HttpPost("CRUD")]
        public async Task<ActionResult> CRUD([FromBody] Dictionary<string, object> d) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            bool forIsA = d.ContainsKey("forIsA") ? bool.Parse(d["forIsA"].ToString()) : false;
            bool forDetail = d.ContainsKey("forDetail") ? bool.Parse(d["forDetail"].ToString()) : false;
            bool withOwnership = d.ContainsKey("withOwnership") ? bool.Parse(d["withOwnership"].ToString()) : false;
            string masterKey = d.ContainsKey("masterKey") ? d["masterKey"].ToString() : null;
            var ret = await _db.QueryAsync("meta.crud", 
                new { 
                    SchemaName = d["schemaName"].ToString(), 
                    TableName = d["tableName"].ToString(),
                    ForDetail = forDetail,
                    MasterKey = masterKey,
                    WithOwnership = withOwnership,
                    ForIsA = forIsA
                }, _options.ConnectionString);
            return Ok(ret);
        }

        /**
        * Creates lookup procedures for a given table
        * @return {ActionResult} - OK or error
        */
        [HttpPost("Lookup")]
        public async Task<ActionResult> Lookup([FromBody] Dictionary<string, object> d) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.QueryAsync("meta.lookup",
                new
                {
                    SchemaName = d["schemaName"].ToString(),
                    TableName = d["tableName"].ToString(),
                    ReferencedColumnDisplayName = "name",
                    ReferencedColumnType = "int"
                }, _options.ConnectionString);
            return Ok(ret);
        }

        /**
        * Reorders the menu
        * @return {ActionResult} - OK or error
        */
        [HttpPost("ReorderMenu/{sourceId}/{targetId}/{where}")]
        public async Task<ActionResult> ReorderMenu(int sourceId, int targetId, string where) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized(); 
            var ret = await _db.ExecuteAsync("meta.reorder_menu", new { SourceId = sourceId, TargetId = targetId, Where = where }, _options.ConnectionString);
            return Ok(ret);
        }

        /**
         * Clears all cached items
         * @return {ActionResult} - OK
         */
        [HttpDelete("ClearCache")]
        public async Task<ActionResult> ClearCache() {
            if (!_currentUserInfo.IsAdmin) return Unauthorized(); 
            await _cache.ClearAll();
            return Ok();
        }

        /*
         * Sets the locked attribute on a route
         * @param {int} id - Route id
         * @param {bool} locked - true or false
         * @return {ActionResult} - OK or error
         */
        [HttpPut("SetLocked/{id}/{locked}")]
        public async Task<ActionResult> SetLocked(int id, bool locked) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.ExecuteAsync("meta.set_locked", new { Id = id, Locked = locked }, _options.ConnectionString);
            return Ok(ret);
        }

        /*
         * Deletes a route
         * @param {int} id - Route id
         * @return {ActionResult} - OK or error
         */
        [HttpDelete("DeleteRoute/{id}")]
        public async Task<ActionResult> DeleteRoute(int id) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            var ret = await _db.ExecuteAsync("delete from meta.route where id = @Id", new { Id = id }, _options.ConnectionString, false, CommandType.Text );
            return Ok(ret);
        }

        /**
            * returns tableAPI properties
            * @return  {ActionResult} - OK or error
            */
        [HttpPut("SetTableAPIProps/{tableAPI}")]
        public async Task<ActionResult> SetTableAPIProps(string tableAPI) {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string body = await reader.ReadToEndAsync();
                var ret = await _db.ExecuteAsync("meta.set_tableapi_props", new { Tableapi = tableAPI, Json = body }, _options.ConnectionString);
                await _cache.Clear("meta.set_tableapi_props_" + tableAPI);
                return Ok(ret);
            }
        }

    }
}
