namespace PublicAPI.Specific.Models {

    /**
            * UploadedFile model
            @module UploadedFileModel
*/

    /**
        * UploadedFileModel model
*/
    public class UploadedFileModel {
        public int id { get; set; }
        public int simple_observation_id { get; set; }
        public string name { get; set; }

        public string file_name { get; set; }

        public string extension { get; set; }

        public string mime_type { get; set; }
        public int? compass { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }

    }
}
