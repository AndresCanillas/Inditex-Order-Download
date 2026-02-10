using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inidtex.ZaraExterlLables
{
    public static partial class JsonToTextConverter
    {
        private const string BluePiggybackReference = "BLUE_LABEL";
        private const string RedPiggybackReference = "RED_LABEL";
        private const string QrProductComponentName = "QR_product";
        private const char Delimeter = ';';

        public static string LoadData(InditexOrderData orderData, ILogService log = null, IConnectionManager connMng = null, int projectID = 0, string labelType = null)
        {
            if (orderData == null)
                throw new ArgumentNullException(nameof(orderData));
            if (orderData.POInformation == null)
                throw new ArgumentException("POInformation no puede ser nulo.", nameof(orderData));

            var labels = LabelDefinitionBuilder.Build(orderData.labels, log).ToList();
            var headerDefinition = BuildHeaderDefinition(labels, labelType, log);
            var headerLine = BuildHeaderLine(headerDefinition);

            var sb = new StringBuilder();
            sb.AppendLine(headerLine);

            var componentLookup = BuildComponentLookup(orderData.ComponentValues);
            var assetLookup = BuildAssetLookup(orderData.Assets);

            foreach (var color in orderData.POInformation.Colors ?? Array.Empty<Color>())
            {
                foreach (var size in color.Sizes ?? Array.Empty<Size>())
                {
                    foreach (var label in labels)
                    {
                        var line = BuildDataLine(orderData, color.ColorRfid, size, label, headerDefinition, componentLookup, assetLookup);
                        sb.AppendLine(line);
                    }
                }
            }

            return sb.ToString();
        }

       

        private static string ResolveComponentValue(Componentvalue componentValue, InditexOrderData orderData, int color, int size)
        {
            if (componentValue == null || componentValue.ValueMap == null)
                return string.Empty;

            var key = BuildValueMapKey(componentValue.GroupKey, orderData, color, size);
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var valueMap = NormalizeValueMap(componentValue.ValueMap);
            return valueMap.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static string BuildValueMapKey(string groupKey, InditexOrderData orderData, int color, int size)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                return null;

            switch (groupKey.Trim().ToUpperInvariant())
            {
                case "MODEL_QUALITY":
                    return $"{orderData.POInformation.ModelRfid}/{orderData.POInformation.QualityRfid}";
                case "COLOR":
                    return color.ToString(CultureInfo.InvariantCulture);
                case "SIZE":
                    return size.ToString(CultureInfo.InvariantCulture);
                case "COLOR_SIZE":
                    return $"{color}/{size}";
                default:
                    return null;
            }
        }

        private static string ResolveBaseFieldValue(
            InditexFieldDefinition field,
            InditexOrderData orderData,
            int color,
            Size size,
            LabelDefinition label)
        {
            if(field == null)
                return string.Empty;

            if(string.Equals(field.Path, InditexOrderSchema.Paths.Color, StringComparison.OrdinalIgnoreCase))
                return color.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, InditexOrderSchema.Paths.Size, StringComparison.OrdinalIgnoreCase))
                return size.SizeRfid.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, InditexOrderSchema.Paths.Quantity, StringComparison.OrdinalIgnoreCase))
                return size.Size_Qty.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, InditexOrderSchema.Paths.LabelReference, StringComparison.OrdinalIgnoreCase))
                return label.Reference;

            return ResolveByReflection(orderData, field.Path);
        }

        private static string ResolveByReflection(object instance, string path)
        {
            if(instance == null || string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var segments = path.Split('.');
            object current = instance;

            foreach(var segment in segments)
            {
                if(current == null)
                    return string.Empty;

                var type = current.GetType();
                var property = type.GetProperty(segment, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                if(property == null)
                    return string.Empty;

                current = property.GetValue(current);
            }

            return Convert.ToString(current, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static IReadOnlyDictionary<string, string> NormalizeValueMap(object valueMap)
        {
            if (valueMap == null)
                return new Dictionary<string, string>();

            if (valueMap is JObject jObject)
            {
                return jObject.Properties()
                    .ToDictionary(p => p.Name, p => p.Value?.ToString() ?? string.Empty);
            }

            if (valueMap is IDictionary<string, object> objectMap)
            {
                return objectMap.ToDictionary(k => k.Key, v => Convert.ToString(v.Value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            if (valueMap is IDictionary<string, string> stringMap)
            {
                return new Dictionary<string, string>(stringMap);
            }

            return new Dictionary<string, string>();
        }

        private static string NormalizeComponentOrAssetValue(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (string.Equals(name, QrProductComponentName, StringComparison.OrdinalIgnoreCase))
                return "Por resolver";

            if (UriHelper.IsUrl(value))
                return UriHelper.ExtractFileNameWithoutExtension(value);

            return value;
        }

        
    }
}
