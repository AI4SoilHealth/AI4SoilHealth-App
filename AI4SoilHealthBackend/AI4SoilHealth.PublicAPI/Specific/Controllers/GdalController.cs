using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Models;
using PublicAPI.Specific.Services;
using System.Data;
using System.Globalization;
using System.Drawing;
using System.Threading.Tasks;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    /**
        * Gdal controller
        @module GdalController
    */
    public class GdalController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GdalController> _logger;
        private readonly GdalService _gdalService;
        private readonly AuthService _authService;

        private const string tmpDir = "tmp";

        public GdalController(AuthService authService, GdalService gdalService, IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<GdalController> logger) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _gdalService = gdalService;
            _authService = authService;
        }

        /**
        * Get history of a point
        * @param {PointForHistory} p - Point
        * @return {ActionResult} - History of the point
        */
        [HttpPost("GetHistory")]
        public ActionResult GetHistory(PointForHistory p) {
            var ret = _gdalService.GetHistory(p);
            return Ok(ret);
        }

        /**
        * Get average in geometry
        * @param {Dictionary<string, string>} d - File, geometry, srid, scale_factor, no_data
        * @return {ActionResult} - Average in geometry
        */
        [HttpPost("CalcStatisticsForGeometry")]
        public async Task<IActionResult> CalcStatisticsForGeometry([FromBody] Dictionary<string, string> d) {
            var ret = await _gdalService.CalcStatisticsForGeometries(null, d["file"], d["geometry"], d["scale_factor"], null, null);
            return Ok(ret);
        }

        /**
            * Calculate statistics for geometries
            * @return {ActionResult} - Nothing, stored in data.geometry_statistics
        */
        [HttpGet("CalcStatisticsForGeometries/{indicator_id?}")]
        public async Task<IActionResult> CalcStatisticsForGeometries(int? indicator_id) {
            var tg = await _db.QueryTableAsync("data.get_geometries_for_statistics", new { IndicatorId = indicator_id }, _options.ConnectionString, false);
            var ta = await _db.QueryTableAsync("data.get_assets_for_statistics", new { IndicatorId = indicator_id }, _options.ConnectionString, false);

            if (tg.Rows.Count > 0) {
                foreach (DataRow arow in ta.Rows) {
                    string url = arow["url"].ToString();
                    if (!System.IO.Directory.Exists(GlobalConstants.ai4soilhealthDir)) System.IO.Directory.CreateDirectory(GlobalConstants.ai4soilhealthDir);
                    string filePath = url.Replace("https://", "");
                    filePath = System.IO.Path.GetFileName(filePath);
                    filePath = GlobalConstants.ai4soilhealthDir + filePath;
                    if (!System.IO.File.Exists(filePath)) {
                        await Helper.DownloadFileAsync(url, filePath);
                    }
                    url = filePath;

                    if (ta.Rows.Count == 1) {
                        string taskId = Guid.NewGuid().ToString();
                        Task.Run(() => _gdalService.CalcStatisticsForGeometries(taskId, url, null, arow["scale_factor"].ToString(), tg, arow));
                        return Ok(new { taskId, count = tg.Rows.Count });
                    } else {
                        await _gdalService.CalcStatisticsForGeometries(null, url, null, arow["scale_factor"].ToString(), tg, arow);
                    }
                }
            }
            return Ok();
        }

        /**
            * Update values for points
            * @param {DataSourceId}
            * @return {ActionResult} - Nothing, stored in data.attribzer
        */
        [HttpGet("UpdateAttributesForPoint/{ShdcFileId}")]
        public async Task<IActionResult> UpdateAttributesForPoint(int ShdcFileId) {

            try {
                var tg = await _db.QueryTableAsync("SELECT id, lat, lon FROM data.point WHERE lat IS NOT NULL and lon IS NOT NULL", null, _options.ConnectionString, false, CommandType.Text);
                var ta = await _db.QueryTableAsync("SELECT s.scale_factor, s.indicator_id, f.temporal_coverage_start, f.depth_id, f.wasabi_link FROM data.shdc_file f INNER JOIN data.shdc s ON s.id = f.shdc_id WHERE f.id=@Id", new { Id = ShdcFileId }, _options.ConnectionString, false, CommandType.Text);

                if (tg.Rows.Count > 0) {
                    var arow = ta.Rows[0];

                    string url = arow["wasabi_link"].ToString();

                    if (!System.IO.Directory.Exists(GlobalConstants.ai4soilhealthDir)) System.IO.Directory.CreateDirectory(GlobalConstants.ai4soilhealthDir);
                    string filePath = url.Replace("https://", "");
                    filePath = System.IO.Path.GetFileName(filePath);
                    filePath = GlobalConstants.ai4soilhealthDir + filePath;
                    if (!System.IO.File.Exists(filePath)) {
                        await Helper.DownloadFileAsync(url, filePath);
                    }
                    url = filePath;

                    string taskId = Guid.NewGuid().ToString();
                    Task.Run(() => _gdalService.UpdateAttributesForPoints(taskId, url, tg, arow));
                    return Ok(new { taskId, count = tg.Rows.Count });

                }
                return Ok();
            } catch (Exception e) {
                return Ok(Helper.CreateError(e.Message));
            }
        }
    }
}
