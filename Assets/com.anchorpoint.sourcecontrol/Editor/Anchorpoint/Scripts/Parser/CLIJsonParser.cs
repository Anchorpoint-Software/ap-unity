using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace Anchorpoint.Parser
{
    public static class CLIJsonParser
    {
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

        private static string SanitizeJsonString(string json)
        {
            return Regex.Replace(json, @"\\x([0-9A-Fa-f]{2})", match =>
            {
                string hexValue = match.Groups[1].Value;
                int intValue = Convert.ToInt32(hexValue, 16);
                return $"\\u{intValue:X4}";
            });
        }
    }
}