using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using PublicAPI.Common.Services;
using System.Text.RegularExpressions;
using System.IO;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json;
using System.Text;

namespace PublicAPI.Specific.Controllers {
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]

    /**
        * TextTree controller (import data from https://www.catalogueoflife.org/data/download)
        @module TextTreeController
*/
    public class TextTreeController : ControllerBase {
        private readonly AppOptions _options;
        private readonly Db _db;

        public TextTreeController(IOptions<AppOptions> options, Db db) {
            _options = options.Value;
            _db = db;
        }

        const string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Convert string to integer (Base-36)
        private static long StringToInt(string input) {
            long result = 0;
            foreach (char c in input) {
                result = result * 36 + Base36Chars.IndexOf(c);
            }
            return result;
        }

        private static int? AddToDic(Dictionary<string, int> d, string key) {
            int id;
            if (key == null) return null;
            if (d.ContainsKey(key)) {
                id = d[key];
            } else {
                id = d.Count + 1;
                d.Add(key, id);
            }
            return id;
        }

        [HttpGet("ImportTextTree")]
        public async Task<ActionResult> ImportTextTree() {
            long[] levels = new long[32];
            Dictionary<string, int> ranks = new Dictionary<string, int>();
            Dictionary<string, int> codes = new Dictionary<string, int>();

            string pattern = @"^(.*?)(?:\s*\[[^\]]+\])*\s*\[([^\]]+)\]\s*\{(.*)\}$";
            Regex regex = new Regex(pattern);
            //string keyValuePattern = @"(?<=^|\s)([A-Z]+)=([^\s]+)";
            string keyValuePattern = @"(?<=^|\s)([A-Z]+)=([^\s]+)";

            Regex regexKV = new Regex(keyValuePattern);

            string filePath = "C:\\Users\\Vedran\\Downloads\\248538ba-2c99-4ca6-8e03-9eae472f9cd5\\dataset-306706.txtree";
            int n = 0, i;
            string ret;
            StringBuilder sb = new StringBuilder(); sb.Append("[");
            foreach (var line in System.IO.File.ReadLines(filePath)) {
                if (++n % 10000 == 0) {
                    Console.WriteLine(n);
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                    string js = sb.ToString();
                    ret = await _db.ExecuteAsync("data.update_taxa", new { Json = js }, _options.ConnectionString);
                    if (Helper.IsError(ret)) {
                        return BadRequest(ret);
                    }
                    sb = new StringBuilder();
                    sb.Append("[");
                }
                for (i = 0; i < line.Length; i++) {
                    if (line[i] != ' ') break;
                }
                int level = i / 2;

                
                Match match = regex.Match(line);
                if (match.Success) {
                    string name = match.Groups[1].Value.Trim().Replace("\"", "'");
                    string rank = match.Groups[2].Value.Trim();
                    string keyValuePart = match.Groups[3].Value;

                    bool synonym = false, uncertain = false, extinct = false;

                    for (i = 0; i < name.Length; i++) {
                        if (char.IsLetter(name[i]) || name[i] == '\'' || name[i] == '×') break;
                        if (name[i] == '=') synonym = true;
                        else if (name[i] == '?') uncertain = true;
                        else if (name[i] == '†') extinct = true;
                        else {
                            Console.WriteLine("Unknown symbol " + name[i]);
                        }
                    }
                    name = name.Substring(i);

                    string id = null, code = null, link = null, remarks = null, reference = null, pub = null, vern = null, env = null, chrono = null;
                    MatchCollection keyValueMatches = regexKV.Matches(keyValuePart);
                    foreach (Match kvp in keyValueMatches) {
                        string key = kvp.Groups[1].Value.Split('=')[0];
                        string value = kvp.Groups[2].Value;

                        if (key == "ID" && id == null) id = value;
                        else if (key == "CODE") code = value;
                        else if (key == "LINK") link = value.Replace("\"", "'");
                        else if (key == "REMARKS") remarks = value.Replace("\"", "'");
                        else if (key == "REF") reference = value.Replace("\"", "'");
                        else if (key == "PUB") reference = value.Replace("\"", "'");
                        else if (key == "VERN") vern = value.Replace("\"", "'");
                        else if (key == "ENV") env = value.Replace("\"", "'");
                        else if (key == "CHRONO") chrono = value.Replace("\"", "'");
                        else
                            Console.WriteLine("Unknown key: " + key);

                       // Console.WriteLine($"Key: {key}, Value: {value}");
                    }


                    long? parent = null;
                    if (level > 0) {
                        parent = levels[level - 1];
                    }

                    long nid = StringToInt(id);
                    levels[level] = nid;

                    int? irank = AddToDic(ranks, rank);



                    int? icode = AddToDic(codes, code);

                    // create json of existing attributes
                    StringBuilder attributes = new StringBuilder();
                    attributes.Append("{");
                    attributes.Append($"\"id_text\":\"{id}\",");
                    attributes.Append($"\"latin_name\":\"{name}\",");
                    attributes.Append($"\"taxa_level_id\":{irank},");
                    if (synonym) attributes.Append("\"synonym\":true,");
                    if (uncertain) attributes.Append("\"uncertain\":true,");
                    if (extinct) attributes.Append("\"extinct\":true,");
                    if (icode != null) attributes.Append($"\"taxa_code_id\": {icode},");
                    if (link != null) attributes.Append($"\"link\":\"{link}\",");
                    if (remarks != null) attributes.Append($"\"remarks\":\"{remarks}\",");
                    if (reference != null) attributes.Append($"\"reference\":\"{reference}\",");
                    if (pub != null) attributes.Append($"\"pub\":\"{pub}\",");
                    if (vern != null) attributes.Append($"\"vern\":\"{vern}\",");
                    if (env != null) attributes.Append($"\"env\":\"{env}\",");
                    if (chrono != null) attributes.Append($"\"chrono\":\"{chrono}\",");
                    if (parent != null) attributes.Append($"\"parent_id\":{parent},");
                    attributes.Append($"\"id\":{nid}");
                    //if (attributes.Length > 1) attributes = attributes.Substring(0, attributes.Length - 1));
                    attributes.Append("},");
                    sb.Append(attributes);
                    //var ret = await _db.ExecuteAsync("data.update_taxa", new { Json = attributes }, _options.ConnectionString);
                    //int x = 1;

                } else {
                    Console.WriteLine(line);

                } 

            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append("]");
            ret = await _db.ExecuteAsync("data.update_taxa", new { Json = sb.ToString() }, _options.ConnectionString);
            if (Helper.IsError(ret)) {
                return BadRequest(ret);
            }


            string json = JsonConvert.SerializeObject(codes);
            ret = await _db.ExecuteAsync("data.update_taxa_code", new { Json = json }, _options.ConnectionString);
            if (Helper.IsError(ret)) {
                return BadRequest(ret);
            }

            json = JsonConvert.SerializeObject(ranks);
            ret = await _db.ExecuteAsync("data.update_taxa_level", new { Json = json }, _options.ConnectionString);
            if (Helper.IsError(ret)) {
                return BadRequest(ret);
            }

            return Ok();
        }

    }
}
