using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
namespace PublicAPI.Common.Services {
    /**
    * Icon helper class   
    @module Icons
    */
    public static class Emojis {
        public static string EmojiList = null;
        public static async Task<string> GetEmojis() {
            if (EmojiList == null) {
                EmojiList = await File.ReadAllTextAsync("emojis.json");
            }
            return EmojiList;
        }

        /**
        * Refresh the list of icons nad store it to a file
        */
        public static async Task RefreshEmojis() {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://raw.githubusercontent.com/amurani/unicode-emoji-list/master/full-emoji-list.json");
            var data = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(data);
            var reducedJsonObject = new JObject();
            foreach (var rootItem in jsonObject) {
                string rootKey = rootItem.Key;
                JArray emojiArray = (JArray)rootItem.Value;
                var reducedArray = new JArray();
                foreach (var item in emojiArray) {
                    var reducedItem = new JObject {
                        { "code", item["code"] },
                        { "description", item["description"].ToString().ToLower() }
                    };
                    reducedArray.Add(reducedItem);
                }
                reducedJsonObject[rootKey] = reducedArray;
            }
            EmojiList = JsonConvert.SerializeObject(reducedJsonObject);
            //store to file
            File.WriteAllText("emojis.json", EmojiList);
        }
    }
}
