using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using Parquet;
using Parquet.Data;
using NetTopologySuite.Geometries;
using System.Drawing;
using Parquet.Schema;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    /**
        * Gdal controller
        @module ParquetController
    */
    public class ParquetController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ParquetController> _logger;
        private readonly GdalService _gdalService;
        private readonly AuthService _authService;

        private const string tmpDir = "tmp";

        public ParquetController(AuthService authService, GdalService gdalService, IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<ParquetController> logger) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _gdalService = gdalService;
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpGet("ParquetCols/{parquetFile}")]
        public async Task<IActionResult> ParquetCols(string parquetFile) {
            try {
                string parquetPath = Path.Combine(GlobalConstants.ai4soilhealthDir, parquetFile);

                using (Stream fileStream = System.IO.File.OpenRead(parquetPath))
                using (ParquetReader reader = await ParquetReader.CreateAsync(fileStream)) {
                    ParquetRowGroupReader group = reader.OpenRowGroupReader(0);
                    var columnData = new List<object>();

                    foreach (var field in reader.Schema.Fields) {
                        if (field is DataField dataField) {
                            // Read only if it's a DataField
                            DataColumn column = await group.ReadColumnAsync(dataField);
                            columnData.Add(new
                            {
                                ColumnName = dataField.Name,
                                FirstValue = column.Data.Length > 0 ? column.Data.GetValue(0) : "No Data"
                            });
                        } else {
                            // Handle nested or unsupported fields
                            columnData.Add(new
                            {
                                ColumnName = field.Name,
                                FirstValue = "Nested or Unsupported Field"
                            });
                        }
                    }

                    return Ok(columnData); // ✅ Returns JSON
                }
            } catch (Exception ex) {
                return BadRequest($"Error reading Parquet file: {ex.Message}");
            }
        }


        //[HttpGet("ParquetData/{parquetFile}")]
        //public async Task ParquetData(string parquetFile) {
        //    string parquetPath = Path.Combine(GlobalConstants.ai4soilhealthDir, parquetFile); // Define parquetPath

        //    using (Stream fileStream = System.IO.File.OpenRead(parquetPath)) {
        //        ParquetReader reader = await ParquetReader.CreateAsync(fileStream);
        //        ParquetRowGroupReader group = reader.OpenRowGroupReader(0);

        //        List<Point> points = new List<Point>();
        //        Parquet.Data.DataColumn lonCol = await group.ReadColumnAsync((Parquet.Data.DataField)reader.Schema.GetDataField("longitude"));
        //        Parquet.Data.DataColumn latCol = await group.ReadColumnAsync((Parquet.Data.DataField)reader.Schema.GetDataField("latitude"));

        //        for (int i = 0; i < lonCol.Data.Length; i++) {
        //            double lon = (double)lonCol.Data.GetValue(i);
        //            double lat = (double)latCol.Data.GetValue(i);
        //            points.Add(new Point(lon, lat));
        //        }

        //        Console.WriteLine($"Extracted {points.Count} points.");
        //    }
        //}
    }

}