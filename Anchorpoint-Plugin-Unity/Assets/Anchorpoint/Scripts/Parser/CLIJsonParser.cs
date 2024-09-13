using UnityEngine;
using Newtonsoft.Json;

namespace AnchorPoint.Parser
{
    public static class CLIJsonParser
    {
        public static T ParseJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse JSON: {ex.Message}");
                return default;
            }
        }
    }
}