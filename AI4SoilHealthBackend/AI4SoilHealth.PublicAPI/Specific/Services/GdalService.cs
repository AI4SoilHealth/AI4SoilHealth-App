using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using Microsoft.Extensions.Options;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Models;
using System.Data;
using Newtonsoft.Json.Linq;
using PublicAPI.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;
using System;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;
using System.Xml.Linq;

namespace PublicAPI.Specific.Services
{
    /**
        * GDAL related operations   
        @module GdalService
*/
    public partial class GdalService {

        private readonly ILogger<GdalService> _logger;
        private readonly IDistributedCache _cache;
        private readonly Db _db;
        private readonly AppOptions _options;

        public GdalService(ILogger<GdalService> logger, IDistributedCache cache, Db db, IOptions<AppOptions> options) { 
            _logger = logger;
            _cache = cache;
            _db = db;
            _options = options.Value;
        }

        /**
        * Import shapefile to database
        * @param {int} shapeId - shape id
        * @param {string} shapeFile - shape file path
        * @returns {string} - error message
        */
        public async Task<string> ShpToTable(int? id, string shapeFile, int geometry_type_id) {
            string ret = "";
            ret = await _db.ExecuteAsync("delete from data.geometry where geometry_type_id = @geometry_type_id and parent_id = @id", new { id = id, geometry_type_id = geometry_type_id }, _options.ConnectionString, true, System.Data.CommandType.Text);
            if (Helper.IsError(ret)) return ret;
            try {
                GdalInit();
                //OSGeo.OGR.Ogr.RegisterAll();
                OSGeo.OGR.Driver drv;

                if (shapeFile.EndsWith(".shp")) {
                    drv = Ogr.GetDriverByName("ESRI Shapefile");
                } else {
                    drv = Ogr.GetDriverByName("KML");
                }

                if (shapeFile.EndsWith(".kmz")) {
                    using (ZipArchive archive = ZipFile.OpenRead(shapeFile)) {
                        foreach (var entry in archive.Entries) {
                            if (entry.FullName.EndsWith(".kml")) {
                                shapeFile = "tmp/tmp.kml";
                                entry.ExtractToFile(shapeFile, true);
                                break;
                            }
                        }
                    }
                }

                DataSource ds = drv.Open(shapeFile, 0);

                OSGeo.OGR.Layer layer = ds.GetLayerByIndex(0);
                OSGeo.OGR.Feature f;
                layer.ResetReading();

                //determine srid
                var srid = Int32.Parse(layer.GetSpatialRef().GetAuthorityCode(null));

                while ((f = layer.GetNextFeature()) != null) {
                    //Geometry
                    var geom = f.GetGeometryRef();
                    string geometry = null;
                    if (geom != null) {
                        geometry = geom.ExportToJson(new string[] { });
                    }

                    //Properties
                    string properties = "";
                    int count = f.GetFieldCount();
                    string pointKey = "";
                    if (count != 0) {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        sb.Append("{");
                        for (int i = 0; i < count; i++) {
                            FieldType type = f.GetFieldType(i);
                            string key = f.GetFieldDefnRef(i).GetName();
                            if (type == FieldType.OFTInteger) {
                                var field = f.GetFieldAsInteger(i);
                                sb.Append("\"" + key + "\":" + field + ",");
                            } else if (type == FieldType.OFTReal) {
                                var field = f.GetFieldAsDouble(i);
                                sb.Append("\"" + key + "\":" + field + ",");
                            } else {
                                var field = f.GetFieldAsString(i);
                                sb.Append("\"" + key + "\":\"" + field + "\",");
                                if (key == "ID") {
                                    pointKey = field;
                                }
                            }
                        }
                        sb.Length--;
                        sb.Append("}");
                        properties = sb.ToString();
                    } else {
                        properties = null;
                    }

                    //System.Diagnostics.Debug.WriteLine(properties);

                    string res = "";
                    if (geom.GetGeometryType() == wkbGeometryType.wkbPoint) {
                        double[] transformedPoint = TransformPoint(srid, 4326, geom.GetX(0), geom.GetY(0));
                        res = await _db.ExecuteAsync("data.set_point_lat_lon",
                        new { PointKey = pointKey, Lat = transformedPoint[0], Lon = transformedPoint[1], SiteId = id }, _options.ConnectionString);
                    } else {
                        res = await _db.ExecuteAsync("data.set_geometry",
                        new { Id = id, Srid = srid, GeometryTypeId = geometry_type_id, Properties = properties, Geometry = geometry }, _options.ConnectionString);
                    }

                    if (Helper.IsError(res)) {
                        ds.Dispose();
                        throw new Exception();
                    }
                }
                //close dataset                   
                ds.Dispose();
            } catch (Exception ex) {
                ret = Helper.HandleException(_logger, ex);
            }
            return ret;
        }

        /**
        * Convert shapefile to GeoJSON
        * @param {string} InputPath - shape file path
        * @returns {string} - GeoJSON
        */
        public string ShpToGeoJSON(string InputPath) {

            OSGeo.OGR.Ogr.RegisterAll();
            OSGeo.OGR.Driver drv = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = drv.Open(InputPath, 0);

            OSGeo.OGR.Layer layer = ds.GetLayerByIndex(0);
            OSGeo.OGR.Feature f;
            layer.ResetReading();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("{\"type\":\"FeatureCollection\", \"features\":[");
            while ((f = layer.GetNextFeature()) != null) {
                //Geometry
                var geom = f.GetGeometryRef();
                if (geom != null) {
                    var geometryJson = geom.ExportToJson(new string[] { });
                    sb.Append("{\"type\":\"Feature\",\"geometry\":" + geometryJson + ",");
                }

                //Properties
                int count = f.GetFieldCount();
                if (count != 0) {
                    sb.Append("\"properties\":{");
                    for (int i = 0; i < count; i++) {
                        FieldType type = f.GetFieldType(i);
                        string key = f.GetFieldDefnRef(i).GetName();

                        if (type == FieldType.OFTInteger) {
                            var field = f.GetFieldAsInteger(i);
                            sb.Append("\"" + key + "\":" + field + ",");
                        } else if (type == FieldType.OFTReal) {
                            var field = f.GetFieldAsDouble(i);
                            sb.Append("\"" + key + "\":" + field + ",");
                        } else {
                            var field = f.GetFieldAsString(i);
                            sb.Append("\"" + key + "\":\"" + field + "\",");
                        }

                    }
                    sb.Length--;
                    sb.Append("},");
                }

                //FID
                long id = f.GetFID();
                sb.AppendLine("\"id\":" + id + "},");
            }

            sb.Length -= 3;
            sb.AppendLine("");
            sb.Append("]}");
            var ret = sb.ToString();
            return ret;
        }

        double[] TransformPoint(int fromSrid, int toSrid, double x, double y) {
            var originalSpatialRef = new SpatialReference("");
            originalSpatialRef.ImportFromEPSG(fromSrid);
            var targetSpatialRef = new SpatialReference("");
            targetSpatialRef.ImportFromEPSG(toSrid);
            if (fromSrid == 4326) {
                originalSpatialRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
                //targetSpatialRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
            }
            var coordinateTransformation = new CoordinateTransformation(originalSpatialRef, targetSpatialRef);
            double[] transformedPoint = new double[3] { x, y, 0 };
            coordinateTransformation.TransformPoint(transformedPoint);
            return transformedPoint;
        }

        /**
        * Convert Gets history for a given point
        * @param {PointForHistory} p - point
        * @returns {string} - history
        */
        public string GetHistory(PointForHistory p) {
            string ret = "";
            if (p.sources.Count == 0) {
                return Helper.CreateError(_logger, "No sources provided");
            }
            try {
                GdalInit();

                double[] transformedPoint = TransformPoint(p.srid, 3035, p.x, p.y);

                int swapXY = p.srid == 3035 ? 1 : 0;
                //swapXY = 0;

                double scaleFactor;
                int formulaIndex;
                findScaleFactorAndFormulaIndex(p.scaleFactor, out scaleFactor, out formulaIndex);

                foreach (var f in p.sources) {
                    Dataset ds = Gdal.Open("/vsicurl/" + f.file, Access.GA_ReadOnly);
                    //Dataset ds = Gdal.Open(f.file, Access.GA_ReadOnly);
                    double[] adfGeoTransform = new double[6];
                    ds.GetGeoTransform(adfGeoTransform);                   
                    // transformedPoint[0] = x, transformedPoint[1] = y ??? or vice versa??? seems that examples are inconsistent
                    int x = (int)((transformedPoint[1 - swapXY] - adfGeoTransform[0]) / adfGeoTransform[1]);
                    int y = (int)((transformedPoint[0 + swapXY] - adfGeoTransform[3]) / adfGeoTransform[5]);
                    int bandCount = ds.RasterCount;
                    var band = ds.GetRasterBand(1);
                    var dataType = band.DataType;
                    double noData; int hasNoData;
                    band.GetNoDataValue(out noData, out hasNoData);
                    double value = 0;
                    if (dataType == DataType.GDT_Byte) {
                        Byte[] bandValues = new Byte[bandCount];
                        ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                        value = bandValues[0];
                    } else if (dataType == DataType.GDT_UInt16 || dataType == DataType.GDT_Int16) {
                        short[] bandValues = new short[bandCount];
                        ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                        value = bandValues[0];
                    } else if (dataType == DataType.GDT_Int32) {
                        int[] bandValues = new int[bandCount];  
                        ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                        value = bandValues[0];
                    } else {
                        throw new Exception("Unknown data type");
                    }

                    if (value == noData) {
                        f.value = null;
                    } else { 
                        if (formulaIndex >= 0) {
                            f.value = (decimal?)functions[formulaIndex](value);
                        } else {
                            f.value = (decimal?)(value / scaleFactor);
                        }
                    }
                    //f.value = bandValues[0];

                    if (p.decimalsForStats != null && f.value != null) {
                        f.value = (decimal?) Math.Round((decimal)f.value, (int)p.decimalsForStats);
                    }
                    ds.Dispose();
                }
                ret = JsonConvert.SerializeObject(p.sources);
                return ret;
            } catch (Exception e) {
                return Helper.CreateError(_logger, JsonConvert.SerializeObject(e));
            }
        }

        /**
            * Convert Gets history for a given point
            * @param {PointForHistory} p - point
            * @returns {string} - history
        */
        public async Task UpdateAttributesForPoints (string taskId, string url, DataTable tg, DataRow arow) {
            string ret = "";
            if (taskId != null) WebSocketHandler.Start(taskId);
            try {
                
                GdalInit();
                double scaleFactor;
                int formulaIndex;
                findScaleFactorAndFormulaIndex(arow["scale_factor"].ToString(), out scaleFactor, out formulaIndex);
                
                // open raster file
                if (url.StartsWith("http")) url = "/vsicurl/" + url;

                Dataset ds = Gdal.Open(url, Access.GA_ReadOnly);
                double[] adfGeoTransform = new double[6];
                ds.GetGeoTransform(adfGeoTransform);
                
                double noData; int hasNoData;
                int bandCount = ds.RasterCount;
                var band = ds.GetRasterBand(1);
                var dataType = band.DataType;
                band.GetNoDataValue(out noData, out hasNoData);

                int i = 0;
                foreach (DataRow grow in tg.Rows) {
                    if (taskId != null && !WebSocketHandler.Notify(taskId, ++i, "")) break;  // if client closed connection
                    //int srid = (int)grow["srid"];

                    if ((int)grow["id"] == 63239) {
                        int xx = 1;
                    }

               

                    try {
                        double[] transformedPoint = TransformPoint(4326, 3035, (double)grow["lon"], (double)grow["lat"]);

                        int swapXY = 0;

                        // transformedPoint[0] = x, transformedPoint[1] = y ??? or vice versa??? seems that examples are inconsistent
                        int x = (int)((transformedPoint[1 - swapXY] - adfGeoTransform[0]) / adfGeoTransform[1]);
                        int y = (int)((transformedPoint[0 + swapXY] - adfGeoTransform[3]) / adfGeoTransform[5]);

                        if (x >= 0 && x <= ds.RasterXSize && y >= 0 && y <= ds.RasterYSize) {

                            decimal? value = null;

                            if (dataType == DataType.GDT_Byte) {
                                Byte[] bandValues = new Byte[bandCount];
                                ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                                value = bandValues[0];
                            } else if (dataType == DataType.GDT_UInt16 || dataType == DataType.GDT_Int16) {
                                short[] bandValues = new short[bandCount];
                                ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                                value = bandValues[0];
                            } else if (dataType == DataType.GDT_Int32) {
                                int[] bandValues = new int[bandCount];
                                ds.GetRasterBand(1).ReadRaster(x, y, 1, 1, bandValues, 1, 1, 0, 0);
                                value = bandValues[0];
                            } else {
                                throw new Exception("Unknown data type");
                            }

                            if ((int)value != noData) {
                                if (formulaIndex >= 0) {
                                    value = (decimal?)functions[formulaIndex]((double)value);
                                } else {
                                    value = (decimal?)value / (decimal)scaleFactor;
                                }
                            }
                            string start = null, tcs = arow["temporal_coverage_start"].ToString();
                            if (tcs.Length == 4) {
                                start = tcs + "-01-01";
                            } else if (tcs.Length == 8) {
                                start = tcs.Substring(0, 4) + "-" + tcs.Substring(4, 2) + "-" + tcs.Substring(6, 2);
                            } else {
                                start = tcs;
                            }
                            object depthId = arow["depth_id"];
                            int? depthIdParam = depthId is DBNull || depthId == null || depthId.ToString() == "{}" ? (int?)null : Convert.ToInt32(depthId);
                            int? intValue = ((int)value == noData ? null : ((int)value));
                            //if (intValue != null) {
                            //    Debug.WriteLine($"Point {(int)grow["id"]}, Lat {(double)grow["lat"]}  Lon {(double)grow["lon"]} : Value:{intValue}");
                            //}
                            //DateTime d = (DateTime)start;
                            string res = await _db.ExecuteAsync("data.update_attribute_description", new { PointId = (int)grow["id"], IndicatorId = (int)arow["indicator_id"], DataSourceId = 0, Date = start, Value = intValue, DepthId = depthIdParam }, _options.ConnectionString);
                            if (Helper.IsError(res)) {
                                WebSocketHandler.Finish(taskId, res);
                            }
                        }
                    } catch (Exception e) {
                        ret = Helper.CreateError(_logger, $"Point {(int)grow["id"]}, Lat {(double)grow["lat"]}  Lon {(double)grow["lon"]} :" + JsonConvert.SerializeObject(e));
                        WebSocketHandler.Finish(taskId, ret);
                    }
                }
                ds.Dispose();
                if (taskId != null) WebSocketHandler.Finish(taskId, "Done");
            } catch (Exception e) {
                ret = Helper.CreateError(_logger, JsonConvert.SerializeObject(e));
                WebSocketHandler.Finish(taskId, ret);
            }
        }

        /**
        * Get metadata for a GeoTIFF file
        * @param {string} geoTiffPath - path to GeoTIFF file
        * @returns {Metadata} - metadata
        */
        public Metadata GetGeoTiffMetadata(string geoTiffPath) {
            GdalInit();
            Dataset ds = Gdal.Open(geoTiffPath, Access.GA_ReadOnly);
            if (ds == null) {
                throw new Exception("Failed to open the GeoTIFF file.");
            }

            double[] adfGeoTransform = new double[6];
            ds.GetGeoTransform(adfGeoTransform);

            Metadata metadata = new Metadata {
                MinLongitude = adfGeoTransform[0],
                MaxLatitude = adfGeoTransform[3],
                MaxLongitude = adfGeoTransform[0] + adfGeoTransform[1] * ds.RasterXSize,
                MinLatitude = adfGeoTransform[3] + adfGeoTransform[5] * ds.RasterYSize
            };

            ds.Dispose();

            return metadata;
        }


        /**
        * Transform pixel coordinates to geographic coordinates
        * @param {double[]} GT - GeoTransform
        * @param {int} xPixel - x pixel
        * @param {int} yPixel - y pixel
        * @param {double} xGeo - x coordinate
        * @param {double} yGeo - y coordinate
        */
        void geoFromPixel(double[] GT, int xPixel, int yPixel, out double xGeo, out double yGeo) {
            xGeo = GT[0] + xPixel * GT[1] + yPixel * GT[2];
            yGeo = GT[3] + xPixel * GT[4] + yPixel * GT[5];
        }

        /**
        * Transform geographic coordinates to pixel coordinates
        * @param {double[]} GT - GeoTransform
        * @param {double} xGeo - x coordinate
        * @param {double} yGeo - y coordinate
        * @param {int} xPixel - x pixel
        * @param {int} yPixel - y pixel
        */
        void pixelFromGeo(double[] GT, double xGeo, double yGeo, out int xPixel, out int yPixel) {
            xPixel = (int)Math.Round((xGeo - GT[0]) / GT[1]);
            yPixel = (int)Math.Round((yGeo - GT[3]) / GT[5]);
        }

        /**
        * Get statistics of a geometry or table of geometries
        * @param {string} file - path to GeoTIFF file
        * @param {string} geometry - geometry
        * @param {int} srid - SRID
        * @param {string} sscaleFactor - scale factor
        * @param {DataTable} tg - table of geometries
        * @param {DataRow} arow - row containing asset info
        * @returns {string} - return status
        */
        public async Task<string> CalcStatisticsForGeometries(string taskId, string file, string geometry, string sScaleFactor, DataTable tg, DataRow arow) {
            int formulaIndex;
            double scaleFactor;
            string wkt, ret = "";
            
            if (taskId != null) WebSocketHandler.Start(taskId);

            OSGeo.OGR.Driver gMemDriver = null;
            OSGeo.GDAL.Driver rMemDriver = null;          
            Dataset rds = null;
            Band rb = null;

            Geometry gm = null, g = null;
            SpatialReference tsr = null;
            Envelope envelope = null;
            DataSource gds = null;
            Layer gl = null;
            Dataset outputDataset = null;
            Band gb = null;

            try {
                GdalInit();

                findScaleFactorAndFormulaIndex(sScaleFactor, out scaleFactor, out formulaIndex);

                tsr = new SpatialReference("");
                tsr.ImportFromEPSG(3035);
                tsr.ExportToWkt(out wkt, null);

                gMemDriver = Ogr.GetDriverByName("Memory");
                rMemDriver = Gdal.GetDriverByName("MEM");
                gds = gMemDriver.CreateDataSource("", null);
                gl = gds.CreateLayer("polygons", tsr, wkbGeometryType.wkbMultiPolygon, null);

                var featureDefn = gl.GetLayerDefn();
                var feature = new Feature(featureDefn);

                // open raster file
                if (file.StartsWith("http")) file = "/vsicurl/" + file;
                rds = Gdal.Open(file, Access.GA_ReadOnly);

                rb = rds.GetRasterBand(1);
                rb.GetNoDataValue(out double noDataValue, out int hasNoDataValue);

                double[] rGeoTransform = new double[6];
                rds.GetGeoTransform(rGeoTransform);
                int rasterCellSize = (int)rGeoTransform[1];

                DataRow grow = null;

                var type = rb.DataType;

                // iterate rows if table was passed
                for (int i = 0; i < (tg != null ? tg.Rows.Count : 1); i++) {


                    string comment = null;

                    if (tg != null) {
                        grow = tg.Rows[i];
                        geometry = grow["geometry"].ToString();
                        comment = grow["name"].ToString();
                        Console.WriteLine(i.ToString() + " " + comment);
                    }

                    if (taskId != null && !WebSocketHandler.Notify(taskId, i + 1, comment)) break;  // if client closed connection

                    // create and transform geometry
                    gm = Ogr.CreateGeometryFromJson(geometry);
                    if (gm.GetSpatialReference().GetAuthorityCode(null) != "3035") {    // todo, assuming geotiffs are in 3035
                        gm.TransformTo(tsr);
                    }

                    Dictionary<int, int> freq = new Dictionary<int, int>();

                    for (int ig = 0; ig < gm.GetGeometryCount(); ig++) {

                        g = gm.GetGeometryRef(ig);

                        envelope = new Envelope();
                        g.GetEnvelope(envelope);

                        int x_res = Convert.ToInt32((envelope.MaxX - envelope.MinX) / rasterCellSize);
                        int y_res = Convert.ToInt32((envelope.MaxY - envelope.MinY) / rasterCellSize);

                        if (x_res > 0 && y_res > 0) {
                            feature.SetGeometry(g);
                            gl.CreateFeature(feature);

                            outputDataset = rMemDriver.Create("", x_res, y_res, 1, DataType.GDT_Byte, null);
                            outputDataset.SetGeoTransform(new double[] { envelope.MinX, rasterCellSize, 0.0, envelope.MaxY, 0.0, -rasterCellSize });
                            outputDataset.SetProjection(wkt);

                            gb = outputDataset.GetRasterBand(1);
                            Gdal.RasterizeLayer(outputDataset, 1, new int[] { 1 }, gl, IntPtr.Zero, IntPtr.Zero, 1, new double[] { 1 }, null, null, "Raster conversion");

                            int x, y;
                            pixelFromGeo(rGeoTransform, envelope.MinX, envelope.MinY, out x, out y);

                            if (x >= 0 && x + x_res <= rds.RasterXSize && y >= 0 && y + y_res <= rds.RasterYSize) {
                                if (type == DataType.GDT_Byte || type == DataType.GDT_Int8) {
                                    Byte[] ga = new Byte[x_res * y_res];
                                    Byte[] ra = new Byte[x_res * y_res];
                                    gb.ReadRaster(0, 0, x_res, y_res, ga, x_res, y_res, 0, 0);
                                    rb.ReadRaster(x, y, x_res, y_res, ra, x_res, y_res, 0, 0);
                                    CountForStatistics<Byte>(ga, ra, (Byte)noDataValue, freq);
                                    Array.Clear(ga);
                                    Array.Clear(ra);
                                } else if (type == DataType.GDT_Int16 || type == DataType.GDT_UInt16) {
                                    Int16[] ga = new Int16[x_res * y_res];
                                    Int16[] ra = new Int16[x_res * y_res];
                                    gb.ReadRaster(0, 0, x_res, y_res, ga, x_res, y_res, 0, 0);
                                    rb.ReadRaster(x, y, x_res, y_res, ra, x_res, y_res, 0, 0);
                                    CountForStatistics<Int16>(ga, ra, (Int16)noDataValue, freq);
                                    Array.Clear(ga);
                                    Array.Clear(ra);
                                } else if (type == DataType.GDT_Int32) {  
                                    Int32[] ga = new Int32[x_res * y_res];
                                    Int32[] ra = new Int32[x_res * y_res];
                                    gb.ReadRaster(0, 0, x_res, y_res, ga, x_res, y_res, 0, 0);
                                    rb.ReadRaster(x, y, x_res, y_res, ra, x_res, y_res, 0, 0);
                                    CountForStatisticsP<Int32>(ga, ra, (Int32)noDataValue, freq);
                                    Array.Clear(ga);
                                    Array.Clear(ra);
                                } else {
                                    throw new Exception("Unknown data type: " + type.ToString());
                                }
                            }
                            gl.DeleteFeature(feature.GetFID());
                        }
                        envelope.Dispose();               
                    }

                    object o = CreateStatistics(freq, formulaIndex, scaleFactor);

                    if (tg != null) {  // table was passed, update in database
                        JObject jsonObject = JObject.FromObject(o); 
                        JObject statObject = (JObject)jsonObject["stat"];

                        if (statObject == null) {
                            statObject = new JObject();
                        }
                        statObject["depth_id"] = arow["depth_id"].ToString();
                        statObject["time_span"] = arow["time_span"].ToString();
                        statObject["data_source_id"] = 0;
                        statObject["parent_id"] = (int)grow["parent_id"];
                        statObject["indicator_id"] = (int)arow["indicator_id"];
                        statObject["geometry_type_id"] = (int)grow["geometry_type_id"];
                        
                        statObject["x"] = jsonObject["x"];
                        statObject["y"] = jsonObject["y"];

                        var stat = statObject.ToString();
                        await _db.ExecuteAsync("data.update_geometry_statistics", new { Json = stat }, _options.ConnectionString);
                    } else {
                        ret = JsonConvert.SerializeObject(o);
                    }

                    freq.Clear();               
                    outputDataset.Dispose();
                    gb.Dispose();
                    gm.Dispose();
  
                }
                if (taskId != null) WebSocketHandler.Finish(taskId, "Done");

            } catch (Exception e) {
                ret = Helper.CreateError(_logger, JsonConvert.SerializeObject(e));
                WebSocketHandler.Finish(taskId, ret);

            } finally {
                
                gMemDriver?.Dispose();
                rMemDriver?.Dispose();
                rds?.Dispose();
                rb?.Dispose();

                gm?.Dispose();
                tsr?.Dispose();
                envelope?.Dispose();

                gds?.Dispose();
                gl?.Dispose();
                outputDataset?.Dispose();
                gb?.Dispose();

            }
            return ret;
        }       
    }
}
