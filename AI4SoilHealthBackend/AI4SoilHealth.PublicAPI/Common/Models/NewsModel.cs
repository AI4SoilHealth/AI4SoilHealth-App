namespace PublicAPI.Common.Models {
    /**
        * Model class for news
        @module NewsModel
    */

    /**
        * Model class for news
    */
    public class News {
        public int id { get; set; }
        public string title { get; set; }
        public string text { get; set; }
        public string extended_text { get; set; }
        public DateTime time_modified { get; set; }
        public DateTime time_created { get; set; }
        public DateTime time_updated { get; set; }
        public string user_modified { get; set; }

    }
}
