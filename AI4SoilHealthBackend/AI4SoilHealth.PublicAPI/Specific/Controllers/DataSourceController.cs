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
        * Data source controller
        * 
        @module SpecificAnonController
    */
    public class DataSourceController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DataSourceController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;

        public DataSourceController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<DataSourceController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
            * Get geometry type properties
            * @return {ActionResult} - Geometry type properties
        */
        [HttpGet("GetGeometryTypeProperties")]
        public async Task<ActionResult> GetGeometryTypeProperties() {
            var ret = await _db.QueryJsonAsyncCached("data.get_geometry_type_properties", new { _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get point history
            * @param {int} pointId - Point id
            * @param {int} indicatorId - Indicator id
            * @return {ActionResult} - Point history
*/
        [HttpGet("GetPointHistory")]
        public async Task<ActionResult> GetPointHistory(int pointId, int indicatorId) {
            var ret = await _db.QueryAsync("data.get_point_history", new { PointId = pointId, IndicatorId = indicatorId }, _options.ConnectionString);
            return Ok(ret);
        }

        /**
            * Get indicators table
            * @return {ActionResult} - Indicators table
*/
        [HttpGet("GetIndicatorsTable")]
        public async Task<ActionResult> GetIndicatorsTable() {
            var ret = await _db.QueryAsync("SELECT * FROM data.indicator", new { _currentUserInfo.LangId }, _options.ConnectionString, cmdType: CommandType.Text);
            return Ok(ret);
        }
        /**
            * Get indicator legend
            * @param {int} indicatorId - Indicator id
            * @return {ActionResult} - Indicator legend
*/
        [HttpGet("GetIndicatorLegend")]
        public async Task<ActionResult> GetIndicatorLegend(int indicatorId) {
            var ret = await _db.QueryJsonAsyncCached("data.get_indicator_legend", new { _currentUserInfo.LangId, IndicatorId = indicatorId }, _options.ConnectionString, "long");
            return Ok(ret);
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
            * @param {int} srid - Srid
            * @return {ActionResult} - Assets
*/
        [HttpGet("GetAssets")]
        public async Task<ActionResult> GetAssets([FromQuery] int titleId, string depth, int srid) {
            var ret = await _db.QueryJsonAsyncCached("data.get_assets", new { TitleId = titleId, Depth = depth, Srid = srid }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get boundaries
            * @param {int} level - Level
            * @param {int} srid - Srid
            * @param {int} zoom - Zoom
            * @param {string} extent - Extent
            * @param {int} indicatorId - Indicator id
            * @param {int} dataSourceId - Data source id
            * @return {ActionResult} - Boundaries
*/
        [HttpGet("GetBoundaries")]
        public async Task<ActionResult> GetBoundaries([FromQuery] int level, int srid, int zoom, string extent, int indicatorId, string dataSourceId) {
            int[] numberArray;
            if (dataSourceId == null) {
                numberArray = [];
            } else {
                numberArray = dataSourceId.Split(',').Select(int.Parse).ToArray();
            }
            var ex = extent.Split(',');
            var ret = await _db.QueryJsonAsyncCached("data.get_nuts_boundaries",
                new
                {
                    Level = level,
                    Srid = srid,
                    Zoom = zoom,
                    MinX = double.Parse(ex[0]),
                    MinY = double.Parse(ex[1]),
                    MaxX = double.Parse(ex[2]),
                    MaxY = double.Parse(ex[3]),
                    IndicatorId = indicatorId,
                    DataSourceId = numberArray
                }, _options.ConnectionString, "long");
            Debug.WriteLine(ret.Length);
            return Ok(ret);
        }

        /**
            * Get tile layers
            * @param {int} srid - Srid
            * @return {ActionResult} - Tile layers
       */
        [HttpGet("GetTileLayers")]
        public async Task<ActionResult> GetTileLayers([FromQuery] int srid) {
            var ret = await _db.QueryAsyncCached("data.get_tile_layers", new { Srid = srid }, _options.ConnectionString, "long");
            return Ok(ret);
        }

    }
}
