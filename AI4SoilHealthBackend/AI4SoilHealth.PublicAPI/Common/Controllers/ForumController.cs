using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicAPI.Common.Extensions;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using PublicAPI.Specific.Models;

namespace PublicAPI.Common.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
      * Forum controller (common methods for getting data related to forum)   
      * @module UserController
    */
    public class ForumController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CommonUserController> _logger;
        private readonly CurrentUserInfo _currentUserInfo;
#if DEBUG
        const string imageDir = "C:/AI4ASoilHealth/uploads/";
#else
        const string imageDir = "/uploads/"; 
#endif

        /**
         * Constructor
         */
        public ForumController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<CommonUserController> logger, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _currentUserInfo = currentUserInfo;
        }

        [HttpGet("GetForum/{id}")]
        public async Task<ActionResult> GetForum(int id) {
            var ret = await _db.QueryAsync("general.get_forum", new { Id = id }, _options.ConnectionString );
            return Ok(ret);
        }

        [HttpPost("AddForum")]
        public async Task<ActionResult> AddForum() {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryScalarAsync("general.add_forum", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        [HttpPost("UpdateForum")]
        public async Task<ActionResult> UpdateForum() {
            using (StreamReader reader = new StreamReader(Request.Body)) {
                string json = await reader.ReadToEndAsync();
                var ret = await _db.QueryAsync("general.update_forum", new { Json = json }, _options.ConnectionString, true);
                return Ok(ret);
            }
        }

        [HttpDelete("DeleteForum/{id}")]
        public async Task<ActionResult> DeleteForum(int id) {
            var ret = await _db.QueryAsync("general.delete_forum", new { Id = id }, _options.ConnectionString, true);
            return Ok(ret);
        }
    }
}
