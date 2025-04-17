namespace PublicAPI.Specific.Models
{
    /**
     * UploadedJsonErrors model
     * @module UploadedJsonErrorsModel
    */

    public class UploadedJsonErrorsModel
    {
        public string file_name { get; set; }
        public int? row { get; set; }
        public string type { get; set; }
        public string error { get; set; }
        public string function { get; set; }
        public string line_number { get; set; }
        public string json_path { get; set; }
        public string sql_query { get; set; }
        public string show_table { get; set; }
        public string show_table_api { get; set; }
    }
}
