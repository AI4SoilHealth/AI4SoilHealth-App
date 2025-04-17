using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Data;
using Newtonsoft.Json;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using Newtonsoft.Json.Linq;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ShapeController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ShapeController> _logger;
        private readonly GdalService _gdalService;
        private readonly AuthService _authService;

        private const string tmpDir = "tmp";
        public ShapeController(AuthService authService, GdalService gdalService, IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<ShapeController> logger) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _gdalService = gdalService;
            _authService = authService;
        }

        [HttpPost("ShpToTable")]
        //        public async Task<IActionResult> ShpToTable([FromForm] List<IFormFile> f) {
        public async Task<IActionResult> ShpToTable() {

            try {
                var pars = JsonConvert.DeserializeObject<Dictionary<string, string>>(Request.Form["params"]);
                string shapeFile = null;
                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
                
                // delete all files in tmpDir
                foreach (var file in Directory.GetFiles(tmpDir)) {
                    System.IO.File.Delete(file);
                }
                for (var i = 0; i < Request.Form.Files.Count; i++) {
                    var file = Request.Form.Files[i];
                    var fileName = file.FileName;
                    if (fileName.EndsWith(".shp") || fileName.EndsWith(".kmz") || fileName.EndsWith(".kml")) {
                        shapeFile = fileName;
                    }
                    using var s = new FileStream(tmpDir + "/" + fileName, FileMode.Create);
                    file.CopyTo(s);
                    s.Close();
                }

                if (shapeFile == null) {
                    return BadRequest(new { error = "Must upload shp, kml or kmz" });
                }

                int? id;
                int geometry_type_id;
                //if (pars["id"] != null) {   // uploaded from app
                    id = pars["id"] != null ? int.Parse(pars["id"]) : null;
                    geometry_type_id = int.Parse(pars["geometry_type_id"]);
                //} else {
                //    var api_params = JsonConvert.DeserializeObject<Dictionary<string, string>>(pars["apiParams"]);
                //    id = int.Parse(api_params["id"]);
                //    geometry_type_id = int.Parse(api_params["geometry_type_id"]);
                //}
                var ret = await _gdalService.ShpToTable(id, tmpDir + "/" + shapeFile, geometry_type_id);
                return Ok(ret);
            } catch (Exception ex) {
                return BadRequest( new { error = ex.Message });
            }
        }

        [HttpGet("ShpToGeoJSON")]
        public ActionResult ShpToGeoJSON() {
            var shpPath = "C:/tmp/HYDROLOGICAL_NETWORK.shp";
            var ret = _gdalService.ShpToGeoJSON(shpPath);
            return Ok(ret);
        }

        [HttpGet("GetGeometryOf")]
        public async Task<ActionResult> GetGeometryOf(int Id, int Srid, int GeometryTypeId, int Zoom, string Extent) {
            System.Diagnostics.Debug.WriteLine($"{Zoom}");
            var ret = await _db.QueryJsonAsync("data.get_geometry", new { Id, Srid, Zoom, Extent, GeometryTypeId }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpPost("GetGeometries")]
        public async Task<ActionResult> GetGeometries() { 
            try {
                StreamReader sr = new StreamReader(Request.Body);
                string body = await sr.ReadToEndAsync();
                sr.Close();

                JObject jObject = JObject.Parse(body);
                int Srid = Int32.Parse(jObject["Srid"].ToString());
                int Zoom = Int32.Parse(jObject["Zoom"].ToString());
                string Extent = jObject["Extent"].ToString();

                int[] ids = JsonConvert.DeserializeObject<int[]>(jObject["Ids"].ToString());
                string GeometryTypeIds = jObject["GeometryTypeIds"].ToString();

                

                int[] geometryTypeIds;
                if (GeometryTypeIds.IndexOf("[") >= 0) {
                    geometryTypeIds = JsonConvert.DeserializeObject<int[]>(GeometryTypeIds);
                } else {
                    geometryTypeIds = Enumerable.Repeat(Int32.Parse(GeometryTypeIds), ids.Length).ToArray();
                }
                var ret = await _db.QueryJsonAsync("data.get_geometries", new { Ids = ids, Srid, Zoom, Extent, GeometryTypeIds = geometryTypeIds }, _options.ConnectionString);
                return Ok(ret);
            } catch (Exception ex) {
                return Ok(Helper.CreateError(_logger, ex.Message));
            }
        }

        [HttpGet("GetGeometry")]
        public async Task<ActionResult> GetGeometry(string SchemaName, string TableName, string DataField, string KeyField, string KeyValue) {
            try {
                var sql = $"SELECT st_asgeojson(\"{DataField}\") geometry FROM \"{SchemaName}\".\"{TableName}\" WHERE \"{KeyField}\" = @KeyValue::integer";
                var ret = await _db.QueryAsync(sql, new { KeyValue }, _options.ConnectionString, false, cmdType: CommandType.Text);
                return Ok(ret);
            } catch (Exception ex) {
                return Ok(Helper.CreateError(_logger, ex.Message));
            }
        }

        [HttpGet("GetNuts")]
        public async Task<ActionResult> GetNuts(int Id, int Srid) {
            var ret = await _db.QueryJsonAsync("general.get_nuts", new { Id, Srid }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpGet("GetBoundary")]
        public async Task<ActionResult> GetBoundary(int OsmId, int Srid) {
            var ret = await _db.QueryJsonAsync("general.get_boundary", new { OsmId, Srid }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpGet("GetChildrenBoundaries")]
        public async Task<ActionResult> GetChildrenBoundaries(int OsmId, int Srid) {
            var ret = await _db.QueryJsonAsync("general.get_children_boundaries", new { OsmId, Srid }, _options.ConnectionString);
            return Ok(ret);
        }

    }
}
