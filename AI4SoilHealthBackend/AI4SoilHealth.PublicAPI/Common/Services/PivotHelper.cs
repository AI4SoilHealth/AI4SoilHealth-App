using Newtonsoft.Json;
using System.Data;
using System.Net;

namespace PublicAPI.Common.Services {
    /**
    * Helper library for various operations   
    @module Helper
    */

    public static class PivotHelper {


        public static void SerializePivotRow(JsonWriter jsonWriter, object[] row, int?[] cols, Dictionary<string, object> rowSums, Dictionary<string, PivotColumn> answers) {
            jsonWriter.WriteStartArray();
            for (int i = 0; i < row.Length; i++) {
                if (cols[i] != null) {
                    jsonWriter.WriteValue(row[i]);
                }
            }
            SerializeRestOfPivotRow(jsonWriter, rowSums, answers);
            jsonWriter.WriteEndArray();
        }

        public static void SerializeRestOfPivotRow(JsonWriter writer, Dictionary<string, object> rowSums, Dictionary<string, PivotColumn> answers) {
            foreach (var pair in rowSums) {
                //writer.WritePropertyName(answers[pair.Key].Value);
                if (pair.Value == null) {
                    writer.WriteValue("");
                } else {
                    writer.WriteValue(pair.Value);
                }
            }
        }

        public class PivotColumn {
            public string Value { get; set; }
            public string DataType { get; set; }
            public int Decimals { get; set; }
        }

        /** 
         * Prepares a dictionary of pivot columns
         * @param a: The reader
         * @return: The dictionary
         */
        public static Dictionary<string, PivotColumn> PreparePivotColumnsDictionary(IDataReader a) {

            Dictionary<string, PivotColumn> pivotColumnsDictionary = new Dictionary<string, PivotColumn>();
            string key;
            while (a.Read()) {
                key = a[0].ToString();
                pivotColumnsDictionary[key] = new PivotColumn { Value = a[1].ToString(), DataType = a[2].ToString(), Decimals = (int)a[3] };
            }
            a.Close();
            return pivotColumnsDictionary;
        }

        public static string Pivot(string aggregate, IDataReader r, Dictionary<string, PivotColumn> pivotColumnsDictionary, int[] colIndices, int pivotColIndex, int pivotValueIndex) {

            Dictionary<string, object> sums = new Dictionary<string, object>();

            StringWriter stringWriter = new StringWriter();
            JsonWriter jsonWriter = new JsonTextWriter(stringWriter);


            jsonWriter.WriteStartObject();
            object[] prev = null;
            int?[] cols = null;
            string key;
            bool first = true;
            int n = 0;
            while (r.Read()) {
                if (first) {
                    // first row, collect column names to pivot around
                    first = false;
                    n = r.FieldCount;
                    prev = new object[n];

                    for (int j = 0; j < n; j++) {
                        prev[j] = r[j];
                    }

                    // write attributes
                    jsonWriter.WritePropertyName("attributes");

                    jsonWriter.WriteStartArray();

                    cols = new int?[n];

                    for (int i = 0; i < n; i++) {
                        if (i != pivotValueIndex && i != pivotColIndex) {
                            Helper.WriteAttribute(jsonWriter, r.GetName(i), r.GetDataTypeName(i));
                            cols[i] = i;
                        }
                    }
                    foreach (var rs in pivotColumnsDictionary) {
                        if (aggregate == "values") {
                            sums[rs.Key] = "";
                        } else {
                            sums[rs.Key] = 0;
                        }
                        Helper.WriteAttribute(jsonWriter, rs.Value.Value, rs.Value.DataType, rs.Value.Decimals);
                    }
                    jsonWriter.WriteEndArray();

                    jsonWriter.WritePropertyName("data");
                    jsonWriter.WriteStartArray();


                } else {
                    //did some of the columns change?
                    for (int i = 0; i < colIndices.Length; i++) {
                        if (r[colIndices[i]].ToString() != prev[colIndices[i]].ToString()) {
                            SerializePivotRow(jsonWriter, prev, cols, sums, pivotColumnsDictionary);
                            foreach (var rs in pivotColumnsDictionary) {
                                if (aggregate == "values") {
                                    sums[rs.Key] = "";
                                } else {
                                    sums[rs.Key] = 0;
                                }
                            }
                            for (int j = 0; j < n; j++) {
                                prev[j] = r[j];
                            }
                            break;
                        }
                    }
                }

                key = r.GetInt32(pivotColIndex).ToString();
                if (!r.IsDBNull(pivotValueIndex)) {
                    if (aggregate == "count") {
                        sums[key] = (int)sums[key] + 1;
                    } else if (aggregate == "sum") {
                        sums[key] = (int)sums[key] + r.GetInt32(pivotValueIndex);
                    } else {
                        sums[key] = r.GetString(pivotValueIndex);
                    }
                }
            }
            if (prev == null) {
                return Helper.CreateError("No data", null);
            }
            SerializePivotRow(jsonWriter, prev, cols, sums, pivotColumnsDictionary);
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
            r.Close();
            return stringWriter.ToString();
        }

        public static void SerializeMultiPivotRow(JsonWriter jsonWriter, object[] row, KeyValuePair<int, string>[] cols, Dictionary<string, int> rowSums, Dictionary<string, string> answers) {
            jsonWriter.WriteStartObject();
            for (int i = 0; i < cols.Length; i++) {
                jsonWriter.WritePropertyName(cols[i].Value);
                jsonWriter.WriteValue(row[cols[i].Key]);
            }
            SerializeRestOfMultiPivotRow(jsonWriter, rowSums, answers);
            jsonWriter.WriteEndObject();
        }

        public static void SerializeRestOfMultiPivotRow(JsonWriter writer, Dictionary<string, int> rowSums, Dictionary<string, string> answers) {
            foreach (var pair in rowSums) {
                writer.WritePropertyName(answers[pair.Key]);
                //if (pair.Value == null || pair.Value == 0) {
                if (pair.Value == 0) {
                    writer.WriteValue(0);
                } else {
                    writer.WriteValue(pair.Value);
                }
            }
        }

        public static Dictionary<string, string> PrepareMultiPivotColumnsDictionary(IDataReader a, int[] pivotColumnsKeys, int[] pivotColumnsNames) {

            Dictionary<string, string> pivotColumnsDictionary = new Dictionary<string, string>();

            string key, value;
            while (a.Read()) {
                key = a.GetInt32(pivotColumnsKeys[0]).ToString();
                for (int i = 1; i < pivotColumnsKeys.Length; i++) {
                    if (!a.IsDBNull(pivotColumnsKeys[i])) {
                        key += "/" + a.GetInt32(pivotColumnsKeys[i]).ToString();
                    }
                }
                value = a.GetString(pivotColumnsNames[0]);
                for (int i = 1; i < pivotColumnsNames.Length; i++) {
                    if (!a.IsDBNull(pivotColumnsNames[i])) {
                        value += "/" + a.GetString(pivotColumnsNames[i]);
                    }
                }
                pivotColumnsDictionary[key] = value;
            }
            a.Close();
            return pivotColumnsDictionary;
        }

        public static string MultiPivot(IDataReader r, Dictionary<string, string> pivotColumnsDictionary, int[] colIndices, int[] resultIndices, int pivotCol) {

            Dictionary<string, int> sums = new Dictionary<string, int>();

            StringWriter stringWriter = new StringWriter();
            JsonWriter jsonWriter = new JsonTextWriter(stringWriter);

            jsonWriter.WriteStartArray();
            object[] prev = new object[r.FieldCount];

            KeyValuePair<int, string>[] cols = new KeyValuePair<int, string>[colIndices.Length];
            string key;

            while (r.Read()) {
                if (prev[0] == null) {
                    for (int i = 0; i < cols.Length; i++) {
                        cols[i] = new KeyValuePair<int, string>(colIndices[i], r.GetName(colIndices[i]));
                    }
                }
                if (prev[0] == null || r.GetInt32(0) != (int)prev[0]) {
                    if (prev[0] != null) {
                        SerializeMultiPivotRow(jsonWriter, prev, cols, sums, pivotColumnsDictionary);
                    }
                    foreach (var rs in pivotColumnsDictionary) {
                        sums[rs.Key] = 0;
                    }
                    for (int i = 0; i < r.FieldCount; i++) {
                        prev[i] = r[i];
                    }
                }
                if (r.IsDBNull(resultIndices[0])) {
                    continue;
                }
                key = r.GetInt32(resultIndices[0]).ToString();
                for (int i = 1; i < resultIndices.Length; i++) {
                    if (!r.IsDBNull(resultIndices[i])) {
                        key = r.GetInt32(resultIndices[i]).ToString() + "/" + key;
                    }
                }
                if (!r.IsDBNull(pivotCol)) {
                    sums[key] += r.GetInt32(pivotCol);
                }
            }
            SerializeMultiPivotRow(jsonWriter, prev, cols, sums, pivotColumnsDictionary);
            jsonWriter.WriteEndArray();
            r.Close();

            return stringWriter.ToString();
        }


    }
}
