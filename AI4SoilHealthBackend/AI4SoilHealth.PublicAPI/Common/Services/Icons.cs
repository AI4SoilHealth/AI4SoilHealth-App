using Newtonsoft.Json;
using System.IO;
namespace PublicAPI.Common.Services {
    /**
    * Icon helper class   
    @module Icons
    */
    public static class Icons {
        public static string IconList = null;
        public static async Task<string> GetIcons() {
            if (IconList == null) {
                IconList = await File.ReadAllTextAsync("icons.json");
            }
            return IconList;
        }

        /**
        * Refresh the list of icons nad store it to a file
        */
        public static async Task RefreshIcons() {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://raw.githubusercontent.com/google/material-design-icons/master/font/MaterialIcons-Regular.codepoints");
            var data = await response.Content.ReadAsStringAsync();
            var icons = data.Split('\n')
                            .Select(icon =>
                            {
                                var parts = icon.Split(' ');
                                var name = parts[0];
                                return new { label = $"<i class='material-icons'>{name}</i>&nbsp;{name}", value = name };
                            })
                            .ToList();
            IconList = JsonConvert.SerializeObject(icons);
            //store to file
            File.WriteAllText("icons.json", IconList);
            return;
        }
    }
}
