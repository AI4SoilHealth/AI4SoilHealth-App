using System.Data;
using OSGeo.GDAL;
using OSGeo.OGR;
using MaxRev.Gdal.Core;
using System.Numerics;

namespace PublicAPI.Specific.Services { 
/**
    * Library for GDAL related operations   
    @module GdalServiceLib
*/

    public class Metadata {
        public double MinLongitude { get; set; }
        public double MinLatitude { get; set; }
        public double MaxLongitude { get; set; }
        public double MaxLatitude { get; set; }
    }

    public partial class GdalService {

        /**
        * Initialize GDAL
        */
        public static void GdalInit() {
            GdalBase.ConfigureAll();
            Gdal.SetConfigOption("GDAL_HTTP_UNSAFESSL", "YES");
            //Gdal.AllRegister();
        }

        /**
        * Calculate the average pixel value of a raster dataset within a polygon geometry
        * @param ds: The raster dataset
        * @param polygonGeometry: The polygon geometry
        * @return: The average pixel value
        */
        public static double CalculateAveragePixelValue(Dataset ds, Geometry polygonGeometry) {           Band band = ds.GetRasterBand(1); // Assuming you're working with the first band
            double pixelValue = 0;
            return pixelValue;
        }

        
        // List of supported transformation algorithms
        public static string[] formulas = {
            "Math.exp(x/10.)-1"
        };

        /**
        * Implementation of the exp1 transformation algorithm
        * @param x: The input value
        * @return: The transformed value
        */
        public static double exp1(double x) { 
            return Math.Exp(x / 10) - 1;
        }

        /**
        * Find the index of a formula in the list of supported formulas
        * @param formula: The formula
        * @return: The index of the formula
        */
        public static int findFormulaIndex(string formula) {
            for (int i = 0; i < formulas.Length; i++) {
                if (formulas[i] == formula) {
                    return i;
                }
            }
            return -1;
        }

        /**
        * Find the scale factor and formula index for a given formula
        * @param formula: The formula
        * @param scaleFactor: The scale factor
        * @param formulaIndex: The formula index
        */
        public static void findScaleFactorAndFormulaIndex (string formula, out double scaleFactor, out int formulaIndex) { 
            scaleFactor = 1;
            formulaIndex = -1;
            if (formula != null) {
                if (formula.Contains("x")) {
                    for (int i = 0; i < formulas.Length; i++) {
                        if (formulas[i] == formula) {
                            formulaIndex = i;
                            return;
                        }
                    }
                    throw new Exception("Formula not found");
                } else {
                    scaleFactor = Double.Parse(formula);
                }
            }
        }

        // List of supported transformation functions
        public static Func<double, double>[] functions = { exp1 };

        /**
        * Calculate the median value of a an array of values
        * @param xArr: The array of values
        * @param yArr: The array of frequencies
        * @param totalCount: The total count
        * @return: The median value
        */
        static double CalculateMedian<T>(T[] xArr, int[] yArr, int totalCount)
        where T : INumber<T>  {
            bool isEvenCount = totalCount % 2 == 0;
            int middleIndex1 = totalCount / 2;
            int middleIndex2 = isEvenCount ? middleIndex1 + 1 : middleIndex1;
            T middle1 = FindNthValue(xArr, yArr, middleIndex1);
            T middle2 = FindNthValue(xArr, yArr, middleIndex2);
            return Convert.ToDouble( isEvenCount ? (middle1 + middle2) / T.CreateChecked((float)2) : middle1);
        }

        /**
        * Find the Nth value in an array
        * @param xArr: The array of values
        * @param yArr: The array of frequencies
        * @param n: The index
        * @return: The Nth value
        */
        static T FindNthValue<T>(T[] xArr, int[] yArr, int n) {
            int cumulativeCount = 0;

            // Iterate through the array to find the Nth value
            for (int i = 0; i < xArr.Length; i++) {
                cumulativeCount += yArr[i];
                if (cumulativeCount >= n) {
                    return xArr[i];
                }
            }

            // This should not happen if n is a valid index
            throw new ArgumentException("Invalid index");
        }


        void CountForStatistics<T>(T[] ga, T[] ra, T noDataValue, Dictionary<int, int> freq) 
          where T : INumber<T> {
            T zero = T.CreateChecked(0);
            int v;
            for (int i = 0; i < ga.Length; i++) {
                if (ga[i] != zero && ra[i] != noDataValue) {
                    v = int.CreateChecked(ra[i]);
                    if (freq.ContainsKey(v)) {
                        freq[v]++;
                    } else { 
                        freq[v] = 1; 
                    }
                }
            }
        }

        void CountForStatisticsP<T>(T[] ga, T[] ra, T noDataValue, Dictionary<int, int> freq)
            where T : INumber<T> {

            T zero = T.CreateChecked(0);
            var localDictionaries = new ThreadLocal<Dictionary<int, int>>(() => new Dictionary<int, int>(), true);

            Parallel.For(0, ga.Length, i =>
            {
                var localFreq = localDictionaries.Value!;
                if (ga[i] != zero && ra[i] != noDataValue) {
                    int v = int.CreateChecked(ra[i]);
                    if (localFreq.ContainsKey(v))
                        localFreq[v]++;
                    else
                        localFreq[v] = 1;
                }
            });

            // Merge local results into the main dictionary
            lock (freq) {
                foreach (var localFreq in localDictionaries.Values) {
                    foreach (var kvp in localFreq) {
                        if (freq.ContainsKey(kvp.Key))
                            freq[kvp.Key] += kvp.Value;
                        else
                            freq[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        

        /**
        * Create statistics for a raster dataset
        * @param ga: The geometry array
        * @param ra: The raster array
        * @param noDataValue: The no data value
        * @param hasNoDataValue: The flag indicating if the no data value is present
        * @param formulaIndex: The formula index
        * @param scaleFactor: The scale factor
        * @return: The statistics
        */
        object CreateStatistics(Dictionary<int, int> freq, int formulaIndex, double scaleFactor) { 

            double[] xArr = null; int[] yArr = null;
            double min = 0, max = 0;
            int count = 0, frequency;
            double mean = 0, m2 = 0, value, delta, delta2, stdev=0, variance;
            double? median = null, q1 = null, q3 = null;

            if (freq.Count > 0) {

                var sortedFreq = freq.OrderBy(kv => kv.Key).ToArray();
                xArr = sortedFreq.Select(kv => Convert.ToDouble(kv.Key)).ToArray();
                yArr = sortedFreq.Select(kv => kv.Value).ToArray();
                int n = 0;
                for (int i = 0; i < xArr.Length; i++) {
                    if (formulaIndex >= 0) {
                        xArr[i] = Math.Round(functions[formulaIndex](xArr[i]), 2);
                    } else {
                        xArr[i] = Math.Round(xArr[i] / scaleFactor, 3);
                    }
                    n += yArr[i];
                }

                min = xArr[0];
                max = xArr[xArr.Length - 1];

                for (int i = 0; i < xArr.Length; i++) {
                    value = xArr[i];
                    frequency = yArr[i];
                    count += frequency;
                    delta = value - mean;
                    mean += delta * yArr[i] / count;
                    delta2 = value - mean;
                    m2 += delta * delta2 * yArr[i];

                    if (q1 == null && count >= n / 4) {
                        q1 = value;
                    }

                    if (median == null && count >= n / 2) {
                        median = value;
                    }

                    if (q3 == null && count >= 3 * n / 4) {
                        q3 = value;
                    }

                }

                variance = m2 / (count - 1);
                stdev = Math.Sqrt(variance);

            }
            var o = new { stat = new { mean, min, max, stdev, n = count, median, q1, q3 }, x = xArr, y = yArr };
            return o;
        }
    }
}
