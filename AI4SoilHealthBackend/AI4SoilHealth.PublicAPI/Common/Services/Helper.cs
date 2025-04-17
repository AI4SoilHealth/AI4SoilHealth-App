using Newtonsoft.Json;
using System.Data;
using System.IO.Compression;
using System.Net;
using System.Text;
using AnyAscii;
using Newtonsoft.Json.Linq;

namespace PublicAPI.Common.Services {
    /**
    * Helper library for various operations   
    @module Helper
    */

    public static class Helper {

        /**
            * Compresses a string
            * @param str: The string to compress
            * @return: The compressed string
            */
        public static byte[] Zip(string str) {
            var bytes = Encoding.UTF8.GetBytes(str);
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using var gs = new GZipStream(mso, CompressionMode.Compress);
            msi.CopyTo(gs);

            return mso.ToArray();
        }

        /**
            * Decompresses a byte array
            * @param bytes: The byte array to decompress
            * @return: The decompressed string
        */
        public static string Unzip(byte[] bytes) {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using var gs = new GZipStream(msi, CompressionMode.Decompress);
            gs.CopyTo(mso);

            return Encoding.UTF8.GetString(mso.ToArray());
        }

        /**
         * Compresses a memory stream as a zip archive
         * @param inputstream - Input stream
         * @param entryName - File name
         * @return {MemoryStream} - Archive as MemoryStream
         */
        public static MemoryStream CreateZippedMemoryStream(MemoryStream inputStream, string entryName) {
            var outputStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, true)) {
                var zipEntry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);

                using (var entryStream = zipEntry.Open()) {
                    inputStream.Position = 0; // Ensure input stream is at the beginning
                    inputStream.CopyTo(entryStream); // Copy input stream data to the zip entry stream
                }
            }

            outputStream.Position = 0; // Reset the position of the output stream before returning
            return outputStream;
        }
        public static MemoryStream ExtractFromZippedMemoryStream(MemoryStream zippedStream, string entryName) {
            var outputStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(zippedStream, ZipArchiveMode.Read, true)) {
                var zipEntry = zipArchive.GetEntry(entryName);
                if (zipEntry == null) {
                    throw new FileNotFoundException($"The entry '{entryName}' was not found in the ZIP archive.");
                }

                using (var entryStream = zipEntry.Open()) {
                    entryStream.CopyTo(outputStream); // Copy the data from the ZIP entry to the output stream
                }
            }

            outputStream.Position = 0; // Reset the position of the output stream before returning
            return outputStream;
        }

        /**
            * Checks if a string contains an error
            * @param s: The string to check
            * @return: True if the string contains an error, false otherwise
            */
        public static bool IsError(string s) {
            return s.Contains("\"error\":");
        }

        /**
            * Creates a message
            * @param milisec: The time in milliseconds
            * @return: The message
            */
        public static string CreateMessage(long milisec) {
            return JsonConvert.SerializeObject(new { message = $"Akcija uspješno obavljena u {milisec} ms" });
        }

        /**
        * Creates a message
        * @param text: The text
        * @return: The message
        */
        public static string CreateMessage(string text) {
            if (IsError(text)) return text;
            return JsonConvert.SerializeObject(new { message = text.Replace("\"", "") });
        }

        private static string ComposeError(string message) {
            if (message.StartsWith("23503")) {
                if (!message.Contains("insert or update")) {   // 2503 is also used for insert or update, show original message in that case
                    message = "Data can not be deleted because it is used elsewhere.";
                }
                //            } else if (message.StartsWith("23505")) {
                //                message = "Data with the same unique constraint already exists.";
            } else {
                message = message.Replace("P0001: ", "");
            }
            return message;
        }

        /**
        * Creates an error message
        * @param logger: The logger
        * @param message: The message
        * @param errToLog: The error to log
        * @return: The error message
        */
        public static string CreateError(ILogger logger, string message, Exception errToLog = null) {
            logger.LogError(errToLog, message);
            return JsonConvert.SerializeObject(new { error = ComposeError(message) });
        }

        /**
            * Creates an error message
            * @param message: The message
            * @param errToLog: The error to log
            * @return: The error message
        */
        public static string CreateError(string message, Exception errToLog = null) {
            return JsonConvert.SerializeObject(new { error = ComposeError(message) });
        }

        /**
        * Handles an exception
        * @param logger: The logger
        * @param ex: The exception
        * @param errExtension: The error extension
        * @return: The error message
        */
        public static string HandleException(ILogger logger, Exception ex, string errExtension = "") {
            string msg = ex.Message;
            bool doLog = true;
            //if (ex is NpgsqlException sqlEx) {
            //if (sqlEx.Class == 18 || sqlEx.State == 255) {  // proprietary FER error
            //    msg += " " + errExtension;
            //    doLog = false;
            //} else {
            //    if (sqlEx.Number == 547) {
            //        var match = Regex.Match(sqlEx.Message, "CHECK constraint \"([^\"]+)\"");
            //        if (match.Success) {
            //            msg = $"Nije uspjelo spremanje zbog provjere {match.Groups[1]?.Value}.";
            //        } else {
            //            match = Regex.Match(sqlEx.Message, "table \"([^\"]+)\"");
            //            msg = $"Ovom akcijom bio bi narušen referencijalni integritet zbog veze s tablicom {match.Groups[1]?.Value}.";//"Nije moguće obrisati zapis na koji se povezuju drugi zapisi";
            //        }
            //        doLog = false;
            //    } else if (sqlEx.Number == 2601) {
            //        msg = "Već postoji zapis s istim ključem";
            //        doLog = false;
            //    }
            //}
            //}
            if (doLog) {
                return CreateError(logger, ex.Message, ex);
            } else {
                return JsonConvert.SerializeObject(new { error = msg });
            }
        }

        /**
        * Serializes reader that returns SELECT ... FOR JSON
        * @param reader: The reader
        * @return: The serialized JSON
        */
        public static string SerializeJsonReader(IDataReader reader) {
            StringBuilder s = new StringBuilder();
            while (reader.Read()) {
                var ret = reader[0].ToString();
                s.Append(ret);
            }
            return s.ToString();
        }

        /**
        * Serializes reader that returns one or more recordsets with SELECT ...       
        * @param reader: The reader
        * @param multiple: True if multiple recordsets are expected
        * @param camelCase: True if attributes should be converted to camelCase
        * @return: The serialized JSON
        */
        public static string SerializeReader(IDataReader reader, bool multiple = false, bool camelCase = false, HashSet<string> keys = null, string keyExpr = null, string[] tableNames = null, bool excelReader = false) {
            var stream = new StringWriter();
            using var writer = new JsonTextWriter(stream);
            int n = 1; bool firstRecord; string name;
            if (multiple) writer.WriteStartObject();
            do {
                if (multiple) {
                    if (tableNames != null) {
                        writer.WritePropertyName($"{tableNames[n - 1]}");
                    } else {
                        writer.WritePropertyName($"Table{n}");
                    }
                    n++;
                }
                writer.WriteStartArray();
                firstRecord = true;
                List<string> names = new List<string>();
                while (reader.Read()) {
                    if (!excelReader || !firstRecord) writer.WriteStartObject();
                    for (int i = 0; i < reader.FieldCount; i++) {
                        
                        if (firstRecord) { // collect names when on first record
                            if (excelReader) {
                                name = reader.GetValue(i).ToString();
                                name = name.Replace(" ", "").Transliterate();
                            } else {
                                name = reader.GetName(i);
                            }
                            if (camelCase) name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                            names.Add(name);
                        }
                        if (keys != null) {
                            if (names[i] == keyExpr) {
                                keys.Add(reader.GetValue(i).ToString());
                            }
                        }
                        if (excelReader && firstRecord) continue;

                        writer.WritePropertyName(names[i]);
                        if (!reader.IsDBNull(i)) {
                            writer.WriteValue(reader.GetValue(i));
                        } else {
                            writer.WriteNull();
                        }

                    }
                    if (!excelReader || !firstRecord) writer.WriteEndObject();
                    firstRecord = false;
                }
                writer.WriteEndArray();
            } while (reader.NextResult());
            if (multiple) writer.WriteEndObject();
            return stream.ToString();
        }

        /**
        * Serializes an attribute
        * @param writer: The writer
        * @param name: The name
        * @param type: The type
        */
        public static void WriteAttribute(JsonWriter writer, string name, string type, int? decimals = null) {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(name);
            writer.WritePropertyName("type");
            writer.WriteValue(type);
            if (decimals != null) {
                writer.WritePropertyName("decimals");
                writer.WriteValue(decimals);
            }
            writer.WriteEndObject();
        }

        /**
        * Serializes reader that returns a recordset int the frugal format
        * @param reader: The reader
        * @return: The serialized JSON
        */
        public static string SerializeReaderFrugal(IDataReader reader) {
            StringWriter stringWriter = new StringWriter();
            JsonWriter jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("attributes");
            jsonWriter.WriteStartArray();

            for (int i = 0; i < reader.FieldCount; i++) {
                WriteAttribute(jsonWriter, reader.GetName(i), reader.GetDataTypeName(i));
            }
            jsonWriter.WriteEndArray();

            jsonWriter.WritePropertyName("data");
            jsonWriter.WriteStartArray();
            while (reader.Read()) {
                jsonWriter.WriteStartArray();

                for (int i = 0; i < reader.FieldCount; i++) {
                    if (reader[i] is Array) {
                        jsonWriter.WriteStartArray();
                        foreach (var el in reader[i] as Array) {
                            jsonWriter.WriteValue(el);
                        }
                        jsonWriter.WriteEndArray();
                    } else {
                        jsonWriter.WriteValue(reader[i]);
                    }
                }

                jsonWriter.WriteEndArray();
            }
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();
            return stringWriter.ToString();
        }

        /**
        * Serializes a single record
        * @param reader: The reader
        * @param camelCase: True if attributes should be converted to camelCase
        * @param returnAttributesIfEmpty: True if attributes should be returned even if the record is empty
        * @return: The serialized JSON
        */
        public static string SerializeSingle(IDataReader reader, bool camelCase = false, bool returnAttributesIfEmpty = false) {
            var stream = new StringWriter();
            using var writer = new JsonTextWriter(stream);
            writer.WriteStartObject();
            if (reader.Read()) {
                for (int i = 0; i < reader.FieldCount; i++) {
                    string name = reader.GetName(i);
                    if (camelCase) name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                    writer.WritePropertyName(name);
                    if (!reader.IsDBNull(i)) {
                        writer.WriteValue(reader.GetValue(i));
                    } else {
                        writer.WriteNull();
                    }
                }
            } else if (returnAttributesIfEmpty) {
                for (int i = 0; i < reader.FieldCount; i++) {
                    string name = reader.GetName(i);
                    if (camelCase) name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                    writer.WritePropertyName(name);
                    writer.WriteNull();
                }
            }
            writer.WriteEndObject();
            return stream.ToString();
        }

        /**
        * Deserializes a grid level action (containing array of @rows)
        * @param values: The values
        * @param obj: The object
        * @param rows: The rows
        */
        public static void GetRows(string values, out Dictionary<string, object> obj, out List<Dictionary<string, object>> rows) {
            obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            var s = JsonConvert.SerializeObject(obj["@rows"]);
            rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(s);
        }

        public static int? GetLineForJsonPath(JObject jsonObject, string propertyPath)
        {
            try {
                JToken? token = jsonObject.SelectToken(propertyPath);
                if (token == null) {
                    throw new ArgumentException(
                        $"Property path '{propertyPath}' does not exist in the provided JSON."
                    );
                }
                if (token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo()) {
                    return lineInfo.LineNumber;
                }
                return null;
            } catch (Exception ex) {
                throw new InvalidOperationException(
                    $"An error occurred while parsing json for the Property path '{propertyPath}'.<br>Error message: {ex.Message}",
                    ex
                );
            }
        }

        #region Http helper functions

        /**
        * Sends a POST request
        * @param client: The client
        * @param url: The URL
        * @param content: The content
        * @return: The response
        */
        public static async Task<Dictionary<string, object>> PostAsync(HttpClient client, string url, FormUrlEncodedContent content) {
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
        }

        /**
        * Sends a GET request
        * @param client: The client
        * @param url: The URL
        * @return: The response
        */
        public static async Task<Dictionary<string, object>> GetObjectAsync(HttpClient client, string url) {
            var response = await client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
        }

        /**
        * Sends a GET request and returns a list
        * @param client: The client
        * @param url: The URL
        * @return: The response
        */
        public static async Task<List<Dictionary<string, object>>> GetListAsync(HttpClient client, string url) {
            var response = await client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseString);
        }

        /**
        * Sends a GET request and returns a string
        * @param client: The client
        * @param url: The URL
        * @return: The response
        */
        public static async Task<Stream> GetStreamAsync(HttpClient client, string url) {
            var response = await client.GetAsync(url);
            var responseStream = await response.Content.ReadAsStreamAsync();
            return responseStream;
        }

        /**
        * Sends a generic request and returns a string
        * @param client: The client
        * @param method: The method
        * @param url: The URL
        * @param content: The content
        * @return: The response
        */
        public static async Task<string> RequestAsync(HttpClient client, HttpMethod method, string url, FormUrlEncodedContent content) {
            var req = new HttpRequestMessage(method, url)
            {
                Content = content,
                Version = HttpVersion.Version10
            };
            var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(responseStream);
            string res = await reader.ReadToEndAsync();
            return res;
        }

        //public static async Task DownloadFileAsync(string url, string filePath) {
        //    using (WebClient webClient = new WebClient()) {
        //        webClient.DownloadProgressChanged += (s, e) =>
        //        {
        //            Console.WriteLine($"Download progress: {e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive} bytes)");
        //        };

        //        webClient.DownloadFileCompleted += (s, e) =>
        //        {
        //            if (e.Error != null)
        //                Console.WriteLine($"Error: {e.Error.Message}");
        //            else
        //                Console.WriteLine("Download complete!");
        //        };
        //        await webClient.DownloadFileTaskAsync(new Uri(url), filePath);
        //    }
        //}

        public static async Task DownloadFileAsync(string url, string outputFilePath) {
            const int BUFFER_SIZE = 1024 * 1024;

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead)) {
                response.EnsureSuccessStatusCode();
                int bytesTotal = 0;
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                Console.WriteLine("Size: " + fileSize.ToString());
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, true)) {
                    var buffer = new byte[BUFFER_SIZE];
                    int bytesRead;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BUFFER_SIZE)) > 0) {
                        bytesTotal += bytesRead;
                        //Console.WriteLine(bytesTotal);
                        await outputFileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }
        }

        #endregion

    }
}
