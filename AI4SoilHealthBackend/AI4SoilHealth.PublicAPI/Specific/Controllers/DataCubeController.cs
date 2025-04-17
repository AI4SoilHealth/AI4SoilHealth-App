using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Data;
using System.Diagnostics;
using PublicAPI.Common.Services;

namespace PublicAPI.Specific.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * Data Cube controller
        * Endpoints associated with data cube in the database (not in tiffs)
        @module DataCubeController
    */
    public class DataCubeController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DataCubeController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;

        public DataCubeController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<DataCubeController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
            * Get titles
            * @return {ActionResult} - Titles
        */
        [HttpGet("GetTitles")]
        public async Task<ActionResult> GetTitles() {
            var ret = await _db.QueryJsonAsyncCached("data.get_titles", null, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get assets
            * @param {int} titleId - Title id
            * @param {string} depth - Depth
            * @param {value} value - m, p025, p975
            * @return {ActionResult} - Assets
*/
        [HttpGet("GetShdcFiles")]
        public async Task<ActionResult> GetAssets([FromQuery] int titleId, string depth, string confidence) {
            var ret = await _db.QueryJsonAsyncCached("data.get_shdc_files", new { TitleId = titleId, Depth = depth, Confidence = confidence }, _options.ConnectionString, "long");
            return Ok(ret);
        }
    }
}
