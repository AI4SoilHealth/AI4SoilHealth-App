namespace PublicAPI.Specific.Models {
    public class AIModel {
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string method { get; set; }
        public string body { get; set; }    

        public string answer { get; set; }
    }
}
