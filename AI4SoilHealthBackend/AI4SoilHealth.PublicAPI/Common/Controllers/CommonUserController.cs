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
using System.Data;
using HandlebarsDotNet.Helpers.BlockHelpers;
using System.Globalization;

namespace PublicAPI.Common.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
      * Common user controller (common methods for getting data related to current user)   
      * @module UserController
    */
    public class CommonUserController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CommonUserController> _logger;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly MailService _mailService;
#if DEBUG
        const string imageDir = "C:/AI4ASoilHealth/uploads/";
#else
        const string imageDir = "/uploads/"; 
#endif

        /**
         * Constructor
         */
        public CommonUserController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<CommonUserController> logger, CurrentUserInfo currentUserInfo, MailService mailService) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _currentUserInfo = currentUserInfo;
            _mailService = mailService;
        }

        [HttpGet("ClearCache")]
        public async Task<ActionResult> ClearCache() {
            await _cache.ClearAll();
            return Ok();
        }

        [HttpGet("GetIcons")]
        public async Task<ActionResult> GetIcons() {
            var ret = await Icons.GetIcons();
            return Ok(ret);
        }

        [HttpGet("RefreshIcons")]
        public async Task<ActionResult> RefreshIcons() {
            await Icons.RefreshIcons();
            return Ok();
        }

        [HttpGet("GetEmojis")]
        public async Task<ActionResult> GetEmojis() {
            var ret = await Emojis.GetEmojis();
            return Ok(ret);
        }

        [AllowAnonymous]
        [HttpGet("RefreshEmojis")]
        public async Task<ActionResult> RefreshEmojis() {
            await Emojis.RefreshEmojis();
            return Ok();
        }

        [HttpPut("SetHelp/{name}")]
        public async Task<ActionResult> SetHelp(string name, [FromBody] Dictionary<string, string> d) {
            var help = d["help"];
            var ret = await _db.QueryAsync("meta.set_help", new { Name = name, _currentUserInfo.LangId, Help = help, }, _options.ConnectionString);
            return Ok(ret);
        }

        [HttpPost("ImportExcel")]
        public async Task<ActionResult> ImportExcel() {
            try {
                
                var ms = new MemoryStream();
                var file = Request.Form.Files[0];
                var fileName = file.FileName;
                file.CopyTo(ms);


                var ret = await _db.ImportExcel(fileName, ms, Request.Form["params"], _options.ConnectionString);
                
                if (ret != null && ret.Contains("error")) {
                    return BadRequest(ret);
                } else {
                    return Ok(ret);
                }
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("ContactUs")]
        public async Task<ActionResult> ContactUs([FromBody] Dictionary<string, string> d) {
            
            var ret = await _db.QuerySingleAsync("SELECT contact_provider_id, contact_mail FROM meta.app", null, _options.ConnectionString, cmdType : CommandType.Text);
            if (Helper.IsError(ret)) return Ok(ret);

            JObject json = JObject.Parse(ret);
            JToken root = json;
            
            int provider_id = root["contact_provider_id"].Value<int>();
            string contact_mail =root["contact_mail"].ToString();
            var subject = d["subject"];
            subject += " # " + d["email"] + " # " + _currentUserInfo.PersonId.ToString();
            ret = await _mailService.SendMail(null, null, 0, provider_id, null, null, subject, d["message"], contact_mail);
            if (Helper.IsError(ret)) return Ok(ret);
            return Ok();
        }
    }
}
