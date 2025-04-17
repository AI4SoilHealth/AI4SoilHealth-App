namespace PublicAPI.Common.Extensions {
    public static class StringExtensions {
        public static string ReplaceFirst(this string input, string oldValue, string newValue) {
            int index = input.IndexOf(oldValue);
            if (index == -1)
                return input; // If oldValue is not found, return the original string
            return input.Substring(0, index) + newValue + input.Substring(index + oldValue.Length);
        }
    }
}