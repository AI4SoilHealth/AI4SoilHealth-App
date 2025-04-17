using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text;
using Newtonsoft.Json;
using PublicAPI.Common.Services;
using PublicAPI.Common.Controllers;
using PublicAPI.Specific.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    /**
        * Chat controller
        @module ChatController
*/
    public class ChatController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ChatController> _logger;
        private readonly CurrentUserInfo _currentUserInfo;


        public ChatController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<ChatController> logger, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _currentUserInfo = currentUserInfo;
        }

        /**
            * Delete a message
            * @param {Dictionary<string, string>} d - Message id
            * @return {ActionResult} - Result of the operation
        */
        [HttpPost("DeleteMessage")]
        public async Task<ActionResult> DeleteMessage(Dictionary<string, string> d) {
            var ret = await _db.ExecuteAsync("general.delete_message", new { Id = int.Parse(d["id"]) }, _options.ConnectionString);
            return Ok(ret);
        }

        /**
            * Delete all messages
            * @return {ActionResult} - Result of the operation
        */
        [HttpPost("DeleteAllMessages")]
        public async Task<ActionResult> DeleteAllMessages() {
            var ret = await _db.ExecuteAsync("general.delete_all_messages", new { _currentUserInfo.PersonId }, _options.ConnectionString);
            return Ok(ret);
        }
        /**
            * Get messages
            * @return {ActionResult} - List of messages
        */
        [HttpGet("GetMessages")]
        public async Task<ActionResult> GetMessages() {
            var ret = await _db.QueryAsync("general.get_messages", null, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
            * Send a message
            * @param {Dictionary<string, string>} d - Message
            * @return {ActionResult} - Result of the operation
        */

        //curl -X POST "http://161.53.133.250:8010/query" -F "question=What is SatMAE?"
        //curl -X POST "http://161.53.133.250:8010/upload" -F "file=@"C:\Users\marin\Documents\MyDocuments\AI4SoilHealth\papers\SatMAE.pdf"
        [HttpPost("SendMessage")]
        public async Task<ActionResult> SendMessage(Dictionary<string, string> d) {
            try {

                AIModel m = _db.QuerySingleAsync<AIModel>("general.get_ai_model", new { Id = Int32.Parse(d["model_id"]) }, _options.ConnectionString).Result;
                HttpClient client = new HttpClient();
                var user = m.username; 
                var password = m.password; 
                if (user != null) {
                    var auth = Encoding.ASCII.GetBytes($"{user}:{password}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));
                }
                var prompt = d["prompt"];
                var payload = m.body.Replace("{{prompt}}", prompt);
                payload = payload.Replace("\n", " ");

                HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(m.url, content);

                if (response.IsSuccessStatusCode) {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    var j = JObject.Parse(jsonResponse);

                    string contentValue = "";
                    if (j[m.answer] != null) { 
                        contentValue = j[m.answer].ToString();
                        contentValue = Regex.Replace(contentValue, @"\*\*(.*?)\*\*", @"<b>$1</b>");
                        contentValue = contentValue.Replace("\"", "\\\"");
                        contentValue = contentValue.Replace("\n\n", "\n");
                        contentValue = contentValue.Replace("\n", "<br />");
                    } else {
                        contentValue = jsonResponse;
                    }

                    string id = "0";
                    string saveMessages = "";
                    d.TryGetValue("saveMessages", out saveMessages);
                    if (saveMessages.ToLower() == "true") {
                        id = await _db.QueryScalarAsync("general.save_message", new { _currentUserInfo.PersonId, Prompt = prompt, Response = contentValue }, _options.ConnectionString);
                        if (id.Contains("error")) return Ok(Helper.CreateError(_logger, id));
                    }

                    string res = JsonConvert.SerializeObject(new
                    {
                        id,
                        response = contentValue,
                        prompt
                    });
                    
                    return Ok(res);
                } else {
                    // Handle the error
                    return Ok(Helper.CreateError(_logger, $"{response.StatusCode} - {response.ReasonPhrase}"));
                }
            } catch (Exception e) {
                return Ok(Helper.CreateError(_logger, e.Message));
            }
        }

    }
   
}
