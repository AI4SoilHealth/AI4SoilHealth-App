using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Data;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;

using PublicAPI.Specific.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parquet.Schema;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class FileController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<FileController> _logger;
        private readonly GdalService _gdalService;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;

        private const string uploadUrl = "http://161.53.133.250:8001/add";
        private const string deleteUrl = "http://161.53.133.250:8001/delete";

        public FileController(AuthService authService, GdalService gdalService, IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<FileController> logger, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _gdalService = gdalService;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
         * Uploads files to "learning" model
         * 
         */
        [HttpPost("Upload")]
        public async Task<ActionResult> Upload() {

            try {
                if (!Directory.Exists(GlobalConstants.fileDir)) Directory.CreateDirectory(GlobalConstants.fileDir);

                //var pars = JsonConvert.DeserializeObject<Dictionary<string, string>>(Request.Form["params"]);
                
                string file_id;

                using (var client = new HttpClient()) {

                    for (var i = 0; i < Request.Form.Files.Count; i++) {
                        var file = Request.Form.Files[i];
                        var fileName = file.FileName;

                        using (var fileStream = file.OpenReadStream())
                        
                        using (var formContent = new MultipartFormDataContent()) {
                            var fileContent = new StreamContent(fileStream);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                            formContent.Add(fileContent, "file", file.FileName);

                            var response = await client.PostAsync(uploadUrl, formContent);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                                // parse object
                                var responseContent = await response.Content.ReadAsStringAsync();
                                JObject o = JObject.Parse(responseContent.ToString());
                                file_id = o["file_id"].ToString();
                            } else {
                                return BadRequest(new { message = response.ReasonPhrase });
                            }
                            

                        }

                        using (var msi = new MemoryStream()) {
                            file.CopyTo(msi);
                            using (var archive = Helper.CreateZippedMemoryStream(msi, fileName)) {
                                var id = await _db.QueryScalarAsync("general.save_file", new { FileId = file_id, Name = fileName, Size = (int)msi.Length, ZippedSize = (int)archive.Length }, _options.ConnectionString);
                                using var s = new FileStream(GlobalConstants.fileDir + "/" + id + ".zip", FileMode.Create);
                                archive.CopyTo(s);
                                s.Close();
                            }
                        }
                    }
                }

                return Ok();
            } catch (Exception ex) {
                return StatusCode(400, new { message = ex.Message });
                //return BadRequest (new { message = ex.Message });
            }
        }


        [HttpGet("Download/{id}/{fileName}")]
        public async Task<ActionResult> Download(int id, string fileName) {
            try {

                var ret = await _db.ExecuteAsync("general.file_check_download", new { Id = id }, _options.ConnectionString);
                if (Helper.IsError(ret)) {  //} && !_currentUserInfo.IsAdmin) {
                    return BadRequest(new { message = ret});
                }

                MemoryStream file;
                using (var fileStream = new FileStream(GlobalConstants.fileDir + "/" + id + ".zip", FileMode.Open)) {
                    using (var memoryStream = new MemoryStream()) {
                        await fileStream.CopyToAsync(memoryStream); // Copy FileStream to MemoryStream
                        memoryStream.Position = 0; // Reset position to the beginning

                        file = Helper.ExtractFromZippedMemoryStream(memoryStream, fileName);
                        if (file == null) {
                            return Ok();
                        } else {
                            return File(file, "application/pdf; charset=UTF-8", "document.pdf");
                        }
                    }
                }
            } catch (Exception ex) {
                return Ok(Helper.HandleException(_logger, ex));
            }
        }

        [HttpDelete("Delete/{id}/{fileId}")]
        public async Task<ActionResult> Delete(int id, string fileId) {
            if (!_currentUserInfo.IsAdmin) return Unauthorized();
            try {
                await _db.ExecuteAsync("delete from general.file where id=@Id and file_id=@FileId", new { Id = id, FileId = fileId}, _options.ConnectionString, false, CommandType.Text);
                System.IO.File.Delete(GlobalConstants.fileDir + "/" + id + ".zip");
                using (var client = new HttpClient()) {
                    var response = await client.DeleteAsync(deleteUrl + '/' + fileId);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                        return Ok();
                    } else {
                        return BadRequest(response.ReasonPhrase);
                    }
                }
            } catch (Exception ex) {
                return Ok(Helper.HandleException(_logger, ex));
            }
        }

        /**
            * Uploads json files with associated images
            * 
        */
        [HttpPost("UploadJson")]
        public async Task<IActionResult> UploadJson() {

            try {
                //string dirName = _currentUserInfo.PersonId.ToString();
                //if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                var jsons = new List<(string, string)>();
                for (var i = 0; i < Request.Form.Files.Count; i++) {
                    var file = Request.Form.Files[i];
                    var fileName = file.FileName;
                    if (Path.GetExtension(fileName) == ".json") {
                        var fileStream = file.OpenReadStream();
                        var fileContent = new StreamContent(fileStream);
                        var json = await fileContent.ReadAsStringAsync();
                        if (json == null) {
                            return BadRequest($"File \"{fileName}\" is not recognized as json file, you must upload a json file.");
                        }
                        jsons.Add((fileName, json));
                    } else {
                        return BadRequest($"File \"{fileName}\" is not recognized as json file, the file should have \".json\" extension.");
                    }
                }
                var errors = new Dictionary<string, List<UploadedJsonErrorsModel>>();
                foreach (var (fileName, json) in jsons) {
                    JObject jsonObject;
                    try {
                        var settings = new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load };
                        jsonObject = JObject.Parse(json, settings);
                    } catch (Exception ex) {
                        return BadRequest($"File \"{fileName}\" can not be parsed as json file. (Error message: {ex.Message})");
                    }
                    var loadedJson = jsonObject.ToString();                   
                    var execute_result = await _db.ExecuteAsync("utils.import_json", new { Json = loadedJson }, _options.ConnectionString);
                    var fileErrors = new List<UploadedJsonErrorsModel>();
                    if (execute_result != "") {
                        var definition = new { error = "" };
                        var errorsMessage = JsonConvert.DeserializeAnonymousType(execute_result, definition).error;
                        if (!errorsMessage.StartsWith("IMPORT_JSON_ERROR:")) {
                            throw new Exception(errorsMessage);
                        }
                        string errorsJson = errorsMessage.Substring("IMPORT_JSON_ERROR:".Length).Trim();
                        fileErrors = JsonConvert.DeserializeObject<List<UploadedJsonErrorsModel>>(errorsJson);                        
                        foreach (var error in fileErrors) {
                            error.row = Helper.GetLineForJsonPath(jsonObject, error.json_path);
                            error.file_name = fileName;
                        }
                    }
                    errors.Add(fileName, fileErrors);
                }
                var ret = JsonConvert.SerializeObject(new { json_errors = errors });
                return Ok(ret);
            } catch (Exception ex) {
                return Ok(Helper.CreateError(_logger, ex.Message));
            }
        }

    }
}
