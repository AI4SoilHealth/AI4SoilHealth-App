using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using PublicAPI.Common.Services;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * Test controller (methods for testing)
        @module TestController
*/
    public class TestController : ControllerBase {
        private readonly AppOptions _options;
        private readonly Db _db;

        public TestController(IOptions<AppOptions> options, Db db) {
            _options = options.Value;
            _db = db;
        }
        /**
            * Get users
            * @return {ActionResult} - Users    
*/
        [HttpGet("GetUsers")]
        public async Task<ActionResult> GetUsers() {
            var users = await _db.QueryAsync("SELECT * FROM t1", null, _options.ConnectionString, true, cmdType: CommandType.Text);
            return Ok(users);
        }

        /**
            * Get neighborhoods
            * @return {ActionResult} - Neighborhoods
*/
        [HttpGet("GetNeighborhoods")]
        public async Task<ActionResult> GetNeighborhoods() {
            //var r = await _db.QueryAsync("select name, st_asgeojson(geom) FROM nyc_neighborhoods", null, _options.ConnectionString, true, cmdType: CommandType.Text);
            var r = await _db.QueryAsync("test.getneighborhoods", null, _options.ConnectionString, true, cmdType: CommandType.StoredProcedure);

            return Ok(r);
        }
        /**
            * Get neighborhoods in an extent
            * @param {double} l - Left
            * @param {double} b - Bottom
            * @param {double} r - Right
            * @param {double} t - Top
            * @return {ActionResult} - Neighborhoods extent
*/
        [HttpGet("GetNeighborhoodsextent")]
        public async Task<ActionResult> GetNeighborhoodsextent(double l, double b, double r, double t, double z) {
            //var r = await _db.QueryAsync("select name, st_asgeojson(geom) FROM nyc_neighborhoods", null, _options.ConnectionString, true, cmdType: CommandType.Text);
            var ret = await _db.QueryJsonAsync("test.getneighborhoodsextent", new { l, b, r, t, z }, _options.ConnectionString, true, cmdType: CommandType.StoredProcedure);
            Debug.WriteLine(ret);
            return Ok(ret);
        }

        /**
            * Get land parcels in an extent
            * @param {double} left - Left
            * @param {double} bottom - Bottom
            * @param {double} right - Right
            * @param {double} top - Top
            * @return {ActionResult} - Land parcels in extent
*/
        [HttpGet("GetLandParcels")]
        public async Task<ActionResult> GetLandParcels(double left, double bottom, double right, double top) {
            var ret = await _db.QueryJsonAsync("test.get_land_parcels", new { l = left, b = bottom, r = right, t = top }, _options.ConnectionString, true, cmdType: CommandType.StoredProcedure);
            Debug.WriteLine(ret);
            return Ok(ret);
        }
    }
}
