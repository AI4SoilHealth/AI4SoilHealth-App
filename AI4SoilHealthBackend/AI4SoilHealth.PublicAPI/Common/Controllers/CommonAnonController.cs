using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Data;
using PublicAPI.Common.Extensions;
using PublicAPI.Common.Models;
using PublicAPI.Common.Services;
using System.Diagnostics;

namespace PublicAPI.Common.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
     * Common anonymous controller (common anonymous methods for getting data related to current user)
     * @module CommonAnonController
    */
    [AllowAnonymous]
    public class CommonAnonController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CommonAnonController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;

        public CommonAnonController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<CommonAnonController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
         * Ping (check if the server is running)
         * @return {ActionResult} - Ok
        */
        [HttpGet("Ping")]
        public ActionResult Ping() {
            return Ok("Ok");
        }

        /**
        * Get locale options
        * @return {ActionResult} - Locale options
        */
        [HttpGet("GetLocaleOptions")]
        public async Task<ActionResult> GetLocaleOptions() {
            var ret = await _db.QueryJsonAsyncCached("meta.get_locale_options", null, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get i18n strings
            * @return {ActionResult} - I18n
*/
        [HttpGet("GetI18N")]
        public async Task<ActionResult> GetI18N() {
            var ret = await _db.QueryJsonAsyncCached("meta.get_i18n", new { _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get help
            * @param {string} name - Name
            * @return {ActionResult} - Help
*/
        [HttpGet("GetHelp/{name}")]
        public async Task<ActionResult> GetHelp(string name) {
            var ret = await _db.QueryJsonAsyncCached("meta.get_help", new { Name = name, _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
    * Get news
    * @param {int} skip - news items to skip 
    * @param {int} n - news items to get
    * @return {ActionResult} - News
*/
        [HttpGet("GetNews/{skip}/{n}")]
        public async Task<ActionResult> GetNews(int skip, int n) {
            try {
                var news = await _cache.GetAsync<List<News>>("News" + _currentUserInfo.LangId.ToString() + "_" + _currentUserInfo.PersonId.ToString());
                if (news == null) {
                    using var connection = _db.CreateConnection(_options.ConnectionString, true);
                    news = (await connection.QueryAsync<News>("meta.get_news", new { _currentUserInfo.LangId },
                        commandType: CommandType.StoredProcedure)).AsList();
                    await _cache.SetAsync("News" + _currentUserInfo.LangId.ToString() + "_" + _currentUserInfo.PersonId.ToString(), news, "long");
                }
                var ret = news.Skip(skip).Take(n);
                return Ok(ret);
            } catch (Exception e) {
                var ret = Helper.CreateError(_logger, e.Message);
                return Ok(ret);
            }

        }

        /**
         * returns the policy for the given policy type
            * @param {string} polycyType - type of policy to return
            * @return {ActionResult} - Policy
        */
        [HttpGet("GetPolicy/{policyType}")]
        public async Task<ActionResult> GetPolicy(string policyType) {

            Debug.WriteLine("Logging testpolicy " + _currentUserInfo.PersonId.ToString());
            _logger.LogError("testpolicy " + _currentUserInfo.PersonId.ToString(), null);
            _logger.LogInformation("testpolicy information");
            _logger.LogDebug("testpolicy information {policyType}", policyType);

            var ret = await _db.QueryScalarAsyncCached("meta.get_policy", new { PolicyType = policyType }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
         * returns app catalogs
         * @return  {ActionResult} - Catalogs
         */
        [HttpGet("GetCatalogs")]
        public async Task<ActionResult> GetCatalogs() {
            var ret = await _db.QueryJsonAsyncCached("data.get_catalogs", new { _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
         * returns tableAPI properties
         * @return  {ActionResult} - tableAPI properties
         */
        [HttpGet("GetTableAPIProps/{tableAPI}")]
        public async Task<ActionResult> GetTableAPIProps(string tableAPI) {
            var ret = await _db.QueryJsonAsyncCached("meta.get_tableapi_props", new { TableAPI = tableAPI }, _options.ConnectionString, "long");
            return Ok(ret);
        }
    }
}
