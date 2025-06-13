using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace Anchorpoint.Parser
{
    /// <summary>
    /// Provides utility methods for parsing CLI JSON output into C# objects.
    /// Includes sanitization of improperly escaped Unicode characters (e.g. \xHH to \uHHHH).
    /// </summary>
    public static class CLIJsonParser
    {
        /// <summary>
        /// Attempts to deserialize the provided JSON string into an object of type T.
        /// Sanitizes the JSON string first to ensure compatibility with the Newtonsoft parser.
        /// Logs and returns default(T) if deserialization fails.
        /// </summary>
        public static T ParseJson<T>(string json)
        {
            try
            {
                json = SanitizeJsonString(json);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse JSON: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Replaces invalid \xHH escape sequences in a JSON string with proper \uHHHH format.
        /// This ensures valid Unicode handling for Newtonsoft's JSON parser.
        /// </summary>
        private static string SanitizeJsonString(string json)
        {
            // Replace \xHH with \u00HH to allow for proper Unicode parsing
            return Regex.Replace(json, @"\\x([0-9A-Fa-f]{2})", match =>
            {
                string hexValue = match.Groups[1].Value;
                int intValue = Convert.ToInt32(hexValue, 16);
                return $"\\u{intValue:X4}";
            });
        }
    }
}