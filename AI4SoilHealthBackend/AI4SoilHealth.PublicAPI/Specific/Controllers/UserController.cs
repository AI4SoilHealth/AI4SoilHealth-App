using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using PublicAPI.Specific.Models;
using System.Data;
using Dapper;
using System.Diagnostics;

namespace PublicAPI.Common.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * User controller (methods for getting data related to current user)   
        @module UserController
    */
    public class UserController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserController> _logger;
        private readonly CurrentUserInfo _currentUserInfo;


        /**
         * Constructor
         */
        public UserController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<UserController> logger, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _currentUserInfo = currentUserInfo;
        }

        [HttpGet("GetSpectrum/{data_source_id}/{point_id}/{date}")]
        public async Task<ActionResult> GetSpectrum(int data_source_id, int point_id, DateTime date) {
            var ret = await _db.QueryAsync("data.get_spectrum", new { DataSourceId = data_source_id, PointId = point_id, Date = date }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpGet("GetSpectrumTable")]
        public async Task<ActionResult> GetSpectrumTable(int data_source_id) {
            int? point_id = null;
            string jsonColumns = null;
            var ret = await _db.QueryJsonAsync("data.get_spectrum", new { PointId = point_id, DataSourceId = data_source_id }, _options.ConnectionString);
            PreprocessService.Preprocess("spectraTable", ref ret, ref jsonColumns);
            return Ok(ret);
        }

        [HttpGet("GetImage/{id}/{extension}")]
        public async Task<ActionResult> GetImage(int id, string extension) {
            try {
                var fileName = id.ToString() + extension;
                var path = GlobalConstants.imageDir + fileName;
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(path);
                var type = MimeTypes.GetMimeType(fileName);
                Response.Headers.Add("Content-Type", type);
                return File(fileBytes, type);
            } catch (Exception e) {
                return Ok(Helper.CreateError(_logger, e.Message));
            }
        }

        [HttpPost("UploadImage/{simple_observation_id}")]
        public async Task<ActionResult> UploadFile(int simple_observation_id) {
            List<UploadedFileModel> files = new List<UploadedFileModel>();
            for (var i = 0; i < Request.Form.Files.Count; i++) {
                var f = Request.Form.Files[i];
                var extension = Path.GetExtension(f.FileName).ToLower();
                var mimeType = MimeTypes.GetMimeType(f.FileName);
                JObject o = JObject.Parse(Request.Form[f.FileName]);
                int? compass = (int?)o["compass"];
                double? lat = (double?)o["lat"];
                double? lon = (double?)o["lon"];
                var ret = await _db.QueryScalarAsync("data.save_image",
                    new
                    {
                        Id = (int?)null,
                        SimpleObservationId = simple_observation_id,
                        Name = f.FileName, // initial name
                        FileName = f.FileName,
                        Extension = extension,
                        MimeType = mimeType,
                        Compass = compass,
                        Lat = lat,
                        Lon = lon,
                    }, _options.ConnectionString);
                if (Helper.IsError(ret)) {
                    return Ok(ret);
                }
                try {
                    // save file to disk
                    var path = GlobalConstants.imageDir + ret.ToString() + Path.GetExtension(f.FileName);
                    using (var stream = new FileStream(path, FileMode.Create)) {
                        await f.CopyToAsync(stream);
                    }
                } catch (Exception e) {
                    return Ok(Helper.CreateError(_logger, e.Message));
                }
                files.Add(new UploadedFileModel
                {
                    id = int.Parse(ret),
                    simple_observation_id = simple_observation_id,
                    name = f.FileName,
                    mime_type = mimeType,
                    extension = extension,
                    compass = compass,
                    lat = lat,
                    lon = lon
                });

            }
            return Ok(files);
        }

        [HttpDelete("DeleteImage/{id}")]
        public async Task<ActionResult> DeleteImage(int id) {
            var ret = await _db.QueryScalarAsync("data.delete_image", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        [HttpPut("SaveImage")]
        public async Task<ActionResult> SaveImage([FromBody] UploadedFileModel f) {
            var ret = await _db.QueryScalarAsync("data.save_image", new
            {
                Id = f.id,
                SimpleObservationId = f.simple_observation_id,
                Name = f.name,
                FileName = f.file_name,
                Extension = f.extension,
                MimeType = f.mime_type,
                Compass = f.compass,
                Lat = f.lat,
                Lon = f.lon,
            }, _options.ConnectionString, true);

            if (Helper.IsError(ret)) {
                return Ok(ret);
            }
            return Ok(ret);
        }

        /**
            * Get custom geometry created by the user, as FeatureCollection
            * @param {string} route_key - Route key
            * @return {ActionResult} - Custom geometry
        */
        [HttpGet("GetCustomGeometry")]
        public async Task<ActionResult> GetCustomGeometry(int? id) {
            var ret = await _db.QueryJsonAsync("data.get_custom_geometry", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
            * Delete custom geometry
            * @param {int} d - Id of the custom geometry
            * @return {ActionResult} - Result of the operation
        */
        [HttpDelete("DeleteCustomGeometry/{id}")]
        public async Task<ActionResult> DeleteCustomGeometry(int id) {
            var ret = await _db.QueryAsync("data.delete_custom_geometry", new { _currentUserInfo.PersonId, Id = id }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpPut("UpdateCompass/{id}/{value}")]
        public async Task<ActionResult> DeleteCustomGeometryTime(int id, int value) {
            var ret = await _db.QueryAsync("data.update_compass", new { Id = id, Value = value }, _options.ConnectionString, true);
            return Ok(ret);
        }

        [HttpGet("GetSingleGeometry")]
        public async Task<ActionResult> GetSingleGeometry(int? id) {
            var ret = await _db.QueryJsonAsync("data.get_single_geometry", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        [HttpPost("SetSingleGeometry")]
        public async Task<ActionResult> SetSingleGeometry() {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryJsonAsync("data.set_single_geometry", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        [HttpPost("SetCustomGeometry")]
        public async Task<ActionResult> SetCustomGeometry() {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryJsonAsync("data.set_custom_geometry", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        /**
            * Update point
            * @param {Dictionary<string, string>} d - id
            * @return {ActionResult} - Result of the operation
        */
        [HttpPost("SetPoint")]
        public async Task<ActionResult> SetPoint() {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryJsonAsync("data.set_point", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        /**
            * Get site points
            * @param {int} id - site id
            * @param {int srid} - srid
            * @return {ActionResult} - Result of the operation
        */
        [HttpGet("GetSitePoints")]
        public async Task<ActionResult> GetSitePoints(int site_id) {
            var ret = await _db.QueryJsonAsync("data.get_site_points", new { Id = site_id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        [HttpDelete("DeleteSimpleObservation/{id}")]
        public async Task<ActionResult> DeleteSimpleObservation(int id) {
            var ret = await _db.ExecuteAsync("data.delete_simple_observation", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }

        [HttpGet("GetDataSourceData/{sDataSourceId}")]
        public async Task<ActionResult> GetPivot(string sDataSourceId) {

            try {
                int? dataSourceId;
                if (sDataSourceId == "null") {
                    return Ok(Helper.CreateError(_logger, "Please select the datasource"));
                //    dataSourceId = null;
                } else {
                    dataSourceId = Int32.Parse(sDataSourceId);
                }
                IDbConnection con = _db.CreateConnection(_options.ConnectionString);
                var a = await con.ExecuteReaderAsync("data.get_data_source_atts", new { DataSourceId = dataSourceId }, commandType: CommandType.StoredProcedure);
                var pivotColumnsDictionary = PivotHelper.PreparePivotColumnsDictionary(a);
                var r = await con.ExecuteReaderAsync("data.get_data_source_vals", new { DataSourceId = dataSourceId }, commandType: CommandType.StoredProcedure);
                var ret = PivotHelper.Pivot("values", r, pivotColumnsDictionary, new int[] { 0, 2, 3 }, 5, 6);
                return Ok(ret);
            } catch (Exception e) {
                return Ok(Helper.CreateError(_logger, e.Message));
            }
        }

        /**
            * Get indicators for data source
            * @dataSourceId int
            * @return {ActionResult} - List of routes
        */
        [HttpGet("GetIndicatorsForDataSource")]
        public async Task<ActionResult> GetIndicatorsForDataSource(string dataSourceId) {
            var numberArray = dataSourceId.Split(',').Select(int.Parse).ToArray();
            var ret = await _db.QueryJsonAsyncCached("data.get_indicators_for_data_source", new { DataSourceId = numberArray, _currentUserInfo.LangId }, _options.ConnectionString, "long", true);
            return Ok(ret);
        }

        /**
            * Get shapes for data source
            * @return {ActionResult} - Sites for data source
        */
        [HttpGet("GetSitesForDataSource")]
        public async Task<ActionResult> GetSitesForDataSource(string dataSourceId) {
            var numberArray = dataSourceId.Split(',').Select(int.Parse).ToArray();
            var ret = await _db.QueryJsonAsyncCached("data.get_sites_for_data_source", new { DataSourceId = numberArray }, _options.ConnectionString, "long", true);
            return Ok(ret);
        }

        /**
            * Get indicator values
            * @param {int} indicatorId - Indicator id
            * @param {int} dataSourceId - Data source id
            * @return {ActionResult} - Indicator values
        */
        [HttpGet("GetIndicatorValues")]
        public async Task<ActionResult> GetIndicatorValues(int indicatorId, string dataSourceId, float? valueFrom, float? valueTo, int? depthId) {
            var numberArray = dataSourceId.Split(',').Select(int.Parse).ToArray();
            var ret = await _db.QueryAsyncFrugal("data.get_indicator_values", new { IndicatorId = indicatorId, DataSourceId = numberArray, ValueFrom = valueFrom, ValueTo = valueTo, DepthId = depthId }, _options.ConnectionString, true);
            return Ok(ret);
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
            * Get boundaries
            * @param {int} level - Level
            * @param {int} srid - Srid
            * @param {int} zoom - Zoom
            * @param {string} extent - Extent
            * @param {int} indicatorId - Indicator id
            * @param {int} dataSourceId - Data source id
            * @return {ActionResult} - Boundaries
        */
        [HttpGet("GetNutsBoundaries")]
        public async Task<ActionResult> GetNutsBoundaries([FromQuery] int level, int srid, int zoom, string extent, int indicatorId, string dataSourceId, int depthId) {
            int[] numberArray;
            if (dataSourceId == null) {
                numberArray = [];
            } else {
                numberArray = dataSourceId.Split(',').Select(int.Parse).ToArray();
            }
            var ret = await _db.QueryJsonAsyncCached("data.get_nuts_boundaries",
                new
                {
                    Level = level,
                    Srid = srid,
                    Zoom = zoom,
                    Extent = extent,
                    IndicatorId = indicatorId,
                    DataSourceId = numberArray,
                    DepthId = depthId
                }, _options.ConnectionString, "long");
            Debug.WriteLine(ret.Length);
            return Ok(ret);
        }

        /**
            * Get field boundaries
            * @param {int} srid - Srid
            * @param {string} extent - Extent
            * @return {ActionResult} - Boundaries
        */
        [HttpGet("GetFieldBoundaries")]
        public async Task<ActionResult> GetFieldBoundaries(int Srid, int Zoom, string Extent) {
            var ret = await _db.QueryJsonAsyncCached("data.get_field_boundaries",
                new { Srid, Zoom, Extent },                 
                _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
        * Get land use boundaries
        * @param {int} srid - Srid
        * @param {string} extent - Extent
        * @return {ActionResult} - Boundaries
        */
        [HttpGet("GetLandUseBoundaries")]
        public async Task<ActionResult> GetLandUseBoundaries(int srid, string extent) {
            var ret = await _db.QueryJsonAsyncCached("data.get_land_use_boundaries",
                new
                {
                    Srid = srid,
                    Extent = extent
                }, _options.ConnectionString, "long");
            System.IO.File.WriteAllText("tmp/output.json", ret);
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
