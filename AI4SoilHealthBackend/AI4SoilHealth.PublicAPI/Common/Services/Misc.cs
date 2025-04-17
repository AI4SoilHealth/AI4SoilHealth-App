using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace PublicAPI.Common.Services {

    /**
        * Miscellaneous helper class   
        @module Misc
*/

    public class Misc {

        private readonly ILogger<Db> _logger;
        private readonly IDistributedCache _cache;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly AppOptions _options;
        private readonly Db _db;

        public Misc(ILogger<Db> logger, IDistributedCache cache, CurrentUserInfo currentUserInfo, IOptions<AppOptions> options, Db db) {
            _logger = logger;
            _cache = cache;
            _currentUserInfo = currentUserInfo;
            _options = options.Value;
            _db = db;
        }

        /**
         * Translate a list of keys in a table
         * @param keys: The list of keys
         * @param targetLang: The target language
         * @param tableName: The table name
         * @param parentKeyName: The parent key name
         * @param fieldName: The field name
         * @param taskId: The task ID
         */
        public async Task Translate(int[] keys, int targetLang, string tableName,
            string parentKeyName, string fieldName, string taskId) {
            if (taskId != null) WebSocketHandler.Start(taskId);
            int n = 0;

            string sourceLangS = "en";
            string targetLangS = await _db.QueryScalarAsync("SELECT LEFT(tag,2) FROM meta.lang WHERE id = @Id", new { Id = targetLang }, _options.ConnectionString, false, CommandType.Text);

            using var connection = _db.CreateConnection(_options.ConnectionString);
            connection.Open();

            for (int i = 0; i < keys.Length; i++) {
                var val = await _db.QueryScalarAsync("meta.get_for_translate", new { Id = keys[i], TableName = tableName, ParentKeyName = parentKeyName }, _options.ConnectionString);
                var translation = await TranslateString(sourceLangS, targetLangS, val, false);
                translation = translation.Replace("'", "''");
                try {
                    await connection.ExecuteAsync("meta.translate_from_app", new
                    {
                        LangId = targetLang,
                        TableName = tableName,
                        ParentKeyName = parentKeyName,
                        ParentKeyValue = keys[i],
                        FieldName = fieldName,
                        FieldValue = translation
                    }, commandType: CommandType.StoredProcedure);
                } catch (Exception e) {
                    _logger.LogError(e, "Error translating " + e.Message);
                }
                if (taskId != null && !WebSocketHandler.Notify(taskId, ++n)) break;
            }
            if (taskId != null) WebSocketHandler.Finish(taskId, "Translated");
        }

        /**
            * Translate a string
            * @param sourceLang: The source language
            * @param targetLang: The target language
            * @param text: The text to translate
            * @param asJson: Return the result as JSON
            * @return: The translated text
            */
        public async Task<string> TranslateString(string sourceLang, string targetLang, string text, bool asJson = true) {
            if (int.TryParse(sourceLang, out int n)) {
                sourceLang = await _db.QueryScalarAsync("SELECT LEFT(tag,2) FROM meta.lang WHERE id = @Id", new { Id = n }, _options.ConnectionString, false, CommandType.Text);
            }
            if (int.TryParse(targetLang, out n)) {
                targetLang = await _db.QueryScalarAsync("SELECT LEFT(tag,2) FROM meta.lang WHERE id = @Id", new { Id = n }, _options.ConnectionString, false, CommandType.Text); ;
            }

            if (targetLang == null) targetLang = sourceLang == "en" ? "hr" : "en";

            string content = $"{{q:\"{text}\", format:\"text\", source: \"{sourceLang}\", target: \"{targetLang}\"}}";

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://translation.googleapis.com/language/translate/v2?key={_options.GoogleAPI}"),
                Content = new StringContent(content)
            };
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            JObject o = JObject.Parse(responseBody);
            string ret = "";
            if (o.ContainsKey("error")) {
                ret = Helper.CreateError(_logger, o["error"]["message"].Value<string>());
            } else if (asJson) {
                ret = $"{{ \"text\" : \"{o["data"]["translations"][0]["translatedText"].Value<string>()}\"}}";
            } else {
                ret = o["data"]["translations"][0]["translatedText"].Value<string>();
            }
            return ret;
        }
    }
}
