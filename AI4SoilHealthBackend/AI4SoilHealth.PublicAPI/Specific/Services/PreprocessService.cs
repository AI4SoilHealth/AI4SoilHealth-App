using PublicAPI.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using PublicAPI.Common.Services;
using System.Runtime.Intrinsics.X86;
using System.Net.Http.Headers;

namespace PublicAPI.Specific.Services {
    /**
    * Helper library for various preprocessing tasks   
    @module PreprocessServiced
    */
    public class PreprocessService {

        //private static string[] names = {
        //    "spectra", "spectraTable"
        //};

        //public static Action<string>  [] Preprocessors = { Spectra, SpectraTable };

        //public static void Preprocess(string name, ref string jsonData) {
        //    for (int i = 0; i < names.Length; i++) {
        //        if (names[i] == name) {
        //            Preprocessors[i](jsonData);
        //        }
        //    }
        //}

        private static ILogger<PreprocessService> _logger;
        public PreprocessService(ILogger<PreprocessService> logger) {
            _logger = logger;
        }

        /**
         * Preprocess the data
         * @param name: The name of the preprocess function
         * @param jsonData: The JSON data
         */
        public static void Preprocess(string name, ref string jsonData, ref string jsonColumns) {
            if (name == null) {
                return;
            } else if (name == "Spectra") {
                Spectra(ref jsonData, ref jsonColumns);
            } else if (name == "SpectraTable") {
                SpectraTable(ref jsonData, ref jsonColumns);
            } else if (name == "SpectraJoinReflectances") {
                SpectraJoinReflectances(ref jsonData, ref jsonColumns);
            } else if (name == "ReduceStats") {
                ReduceStats (ref jsonData, ref jsonColumns);
            }
        }


        /**
         * Preprocess the spectra data as a table
         * @param jsonData: The JSON data
         */
        public static void SpectraTable(ref string jsonData, ref string jsonColumns) {
            // abandoned for very slow performance in Quasar table, logic moved to export on client side
            try {
                Frugal o = JsonConvert.DeserializeObject<Frugal>(jsonData);
                var stream = new StringWriter();
                using var writer = new JsonTextWriter(stream);

                if (o.data.Count == 0) {
                    jsonData = "[]";
                    return;
                }
                writer.WriteStartObject();

                writer.WritePropertyName("attributes");
                writer.WriteStartArray();
                Helper.WriteAttribute(writer, "date", "string");

                JArray wavelengths = (JArray)o.data[0][1];

                foreach (var item in wavelengths) {
                    Helper.WriteAttribute(writer, item.ToString(), "float");
                }
                writer.WriteEndArray();

                writer.WritePropertyName("data");
                writer.WriteStartArray();
                foreach (var row in o.data) {
                    writer.WriteStartArray();
                    writer.WriteValue(row[0]);
                    JArray values = (JArray)row[2];
                    foreach (var item in values) {
                        writer.WriteValue(item);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                jsonData = stream.ToString();
            } catch (Exception ex) {
                jsonData = Helper.CreateError(null, ex.Message);
            }
        }

        /**
         * Preprocess the spectra data
         * @param jsonData: The JSON data
         */
        public static void Spectra(ref string jsonData, ref string jsonColumns) {

            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);
            var stream = new StringWriter();
            using var writer = new JsonTextWriter(stream);

            writer.WriteStartArray();
            foreach (var row in data) {
                bool first = true;
                List<int> wavelengths = new List<int>();
                List<float> reflectances = new List<float>();
                writer.WriteStartObject();
                foreach (var att in row) {
                    if (first) {
                        first = false;
                        writer.WritePropertyName("id");
                        writer.WriteValue(att.Value.ToString());
                    } else {
                        wavelengths.Add(int.Parse(att.Key));
                        reflectances.Add(float.Parse(att.Value.ToString()));
                    }
                }
                writer.WritePropertyName("wavelengths");
                writer.WriteStartArray();
                foreach (var w in wavelengths) {
                    writer.WriteValue(w);
                }
                writer.WriteEndArray();
                writer.WritePropertyName("values");
                writer.WriteStartArray();
                foreach (var r in reflectances) {
                    writer.WriteValue(r);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            jsonData = stream.ToString();
        }

        /**
            * Preprocess the spectra data by joining reflectances into a single column
            * @param jsonData: The JSON data
        */
        public static void SpectraJoinReflectances(ref string jsonData, ref string jsonColumns) {

            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);
            var stream = new StringWriter();
            using var writer = new JsonTextWriter(stream);

            writer.WriteStartArray();
            foreach (var row in data) {
                bool first = true;
                writer.WriteStartObject();
                List<float> reflectances = new List<float>();
                foreach (var att in row) {
                    if (first) {
                        first = false;
                        writer.WritePropertyName("id");
                        writer.WriteValue(att.Value.ToString());
                    } else {
                        reflectances.Add(float.Parse(att.Value.ToString()));
                    }
                }
                writer.WritePropertyName("reflectances");
                writer.WriteValue("{" + string.Join(",", reflectances) + "}");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            jsonData = stream.ToString();

            JArray a = new JArray();
            a.Add(new JObject { { "col_name", "id" }, {"col_type", "string" } });
            a.Add(new JObject { { "col_name", "reflectances" }, {"col_type", "string" } });
            jsonColumns = a.ToString();
        }


        public static void ReduceStats(ref string jsonData, ref string jsonColumns) {
            return;

            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);

            var stream = new StringWriter();
            using var writer = new JsonTextWriter(stream);

            writer.WriteStartArray();
            foreach (var row in data) {
                bool first = true;
                writer.WriteStartObject();
                List<float> reflectances = new List<float>();
                foreach (var att in row) {
                    if (first) {
                        first = false;
                        writer.WritePropertyName("id");
                        writer.WriteValue(att.Value.ToString());
                    } else {
                        reflectances.Add(float.Parse(att.Value.ToString()));
                    }
                }
                writer.WritePropertyName("reflectances");
                writer.WriteValue("{" + string.Join(",", reflectances) + "}");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            jsonData = stream.ToString();

            JArray a = new JArray();
            a.Add(new JObject { { "col_name", "id" }, { "col_type", "string" } });
            a.Add(new JObject { { "col_name", "reflectances" }, { "col_type", "string" } });
            jsonColumns = a.ToString();
        }
    }
}
