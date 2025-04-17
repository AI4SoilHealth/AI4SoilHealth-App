namespace PublicAPI.Specific.Models {
    /**
        * Point model
        @module Point
*/

    /**
        * Source model
*/
    public class Source {
        public string file { get; set; }
        public string label { get; set; }
        public decimal? value { get; set; }
    }

    /**
        * Point model
*/
    public class PointForHistory {
        public double x { get; set; }
        public double y { get; set; }
        public int srid { get; set; }
        public string scaleFactor { get; set; }
        public List<Source> sources { get; set; }
        public int? decimalsForStats { get; set; }
    }
}
