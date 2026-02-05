using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JsonColor
{
    public static class LoadJsonConfig
    {
        public static bool GetIsOnlyColor()
        {
            return GetattributeJsonConfig("IsOnlyColor").ToObject<bool>();
        }
        public static string[] GetLabelsWithPiggyback()
        {
            var labelesWithPiggyback = GetattributeJsonConfig("LabelsWithPiggyback").ToString();
            return SplitAndTrim(labelesWithPiggyback);

        }
        public static List<SizeDescriptionRule> GetLabelsWithSizeDescriptionRule()
        {
            return GetattributeJsonConfig("LabelsWithSizeDescriptionRule").ToObject<List<SizeDescriptionRule>>();
           

        }
        public static string[] GetLabelsWithDiferentLayout()
        {
            var labelsWithDiferentLayout = GetattributeJsonConfig("LabelsWithDiferentLayout").ToString();
            return SplitAndTrim(labelsWithDiferentLayout);

        }

        public static IDictionary<string, string[]> GetLabelsWithPiggybackTypeRules()
        {
            var attribute = GetattributeJsonConfig("LabelsWithPiggybackTypeRules");

            if(attribute == null)
            {
                return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            }

            var rules = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach(var rule in attribute.OfType<JObject>())
            {
                var labelId = rule["LabelId"]?.ToString();
                var typePos = rule["TypePOs"]?.ToString();

                if(string.IsNullOrWhiteSpace(labelId) || string.IsNullOrWhiteSpace(typePos))
                {
                    continue;
                }

                var normalizedLabelId = labelId.Trim();
                var normalizedTypePos = SplitAndTrim(typePos);

                if(normalizedTypePos.Length == 0)
                {
                    continue;
                }

                rules[normalizedLabelId] = normalizedTypePos;
            }

            return rules;
        }


        private static JToken GetattributeJsonConfig(string arrtributeName)
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", "");
            var path = Path.Combine(baseDir, "Resources\\MangoJsonColorConfig.json");
            if(!File.Exists(path))
                throw new Exception($"Error not found File MangoJsonColorConfig.json in path {path} ");

            var jsonObject = JObject.Parse(File.ReadAllText(path));
            return jsonObject[arrtributeName];

        }
        private static string[] SplitAndTrim(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .ToArray();
        }
    }
    public class SizeDescriptionRule
    {
        public string LabelId { get; set; }
        public string SizeId { get; set; }
    }
}
