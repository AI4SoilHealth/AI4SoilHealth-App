using PublicAPI.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ExcelDataReader;
using Newtonsoft.Json;
using System.Data;
namespace PublicAPI.Common.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * Misc controller (miscellaneous methods)   
        @module MiscController
    */
    public class MiscController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<MiscController> _logger;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly Misc _misc;

        /**
         * Constructor
         */
        public MiscController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<MiscController> logger, CurrentUserInfo currentUserInfo, Misc misc) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _currentUserInfo = currentUserInfo;
            _misc = misc;
        }

        /**
         * Translates multiple values in a separate thread
         * @return taskId
         */
        [HttpPost("Translate")]
        public ActionResult Translate([FromBody] TranslateParams values) {
            string taskId = Guid.NewGuid().ToString();
            //int[] keys = JsonConvert.DeserializeObject<int[]>(pars["keys"].ToString());
            Task.Run(() => _misc.Translate(values.keys, values.targetLang, values.tableName, values.parentKeyName, values.fieldName, taskId));
            return Ok(new { taskId, count = values.keys.Length });
        }

        /**
         * Translates a single value
         * @return taskId
         */
        [HttpGet("TranslateSingle")]
        public async Task<ActionResult> TranslateSingle(string sourceLang, string targetLang, string value) {
            var ret = await _misc.TranslateString(sourceLang, targetLang, value);
            return Ok(ret);
        }


        [HttpPost("AnalyzeOrImportFile")]
        public async Task<ActionResult> AnalyzeFile() {
            try {
                var pars = JsonConvert.DeserializeObject<Dictionary<string, string>>(Request.Form["params"]);
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var apiParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(pars["apiParams"]);

                using var ms = new MemoryStream();
                Request.Form.Files[0].CopyTo(ms);
                IExcelDataReader reader;
                if (Request.Form.Files[0].FileName.EndsWith(".csv")) {
                    reader = ExcelReaderFactory.CreateCsvReader(ms);
                } else {
                    reader = ExcelReaderFactory.CreateReader(ms);
                }
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                DataTable t = null;
                if (apiParams["sheet"] != "") {
                    t = ds.Tables[apiParams["sheet"].ToString()];
                }
                t ??= ds.Tables[0];

                if (apiParams["import"] == "true") {

                    var r = t.CreateDataReader();
                    //var procName = apiParams["schema_name"] + "." + apiParams["schema_name"] + "_" + apiParams["table_name"] + "_import_excel";
                    var procName = apiParams["schema_name"] + ".import_" + apiParams["import_name"];

                    var jsonData = Helper.SerializeReader(r, excelReader: false);
                    var ret = await _db.ExecuteAsync(procName,
                                            new { jsonCV = "", jsonData = jsonData }, _options.ConnectionString, false, cmdType: CommandType.StoredProcedure);
                    return Ok(ret);
                } else {
                    var stream = new StringWriter();
                    using var writer = new JsonTextWriter(stream);
                    writer.WriteStartArray();
                    foreach (DataColumn c in t.Columns) {
                        writer.WriteStartObject();
                        writer.WritePropertyName("col_name");
                        writer.WriteValue(c.ColumnName);
                        writer.WritePropertyName("col_type");
                        writer.WriteValue(c.DataType.ToString());
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();

                    var ret = await _db.ExecuteAsync("meta.analyze_file",
                    new { Id = Int32.Parse(pars["id"]), JsonData = stream.ToString() }, _options.ConnectionString, false, cmdType: CommandType.StoredProcedure);
                    return Ok(ret);
                }
            } catch (Exception ex) {
                return Ok(Helper.HandleException(_logger, ex));
            }
        }

    }

    public class TranslateParams {
        public int targetLang { get; set; }
        public string tableName { get; set; }
        public string parentKeyName { get; set; }
        public string fieldName { get; set; }
        public int[] keys { get; set; }
    }
}
