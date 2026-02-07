using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inidtex.ZaraExterlLables
{
    public static partial class JsonToTextConverter
    {
        private static string
           BuildDataLine(
           InditexOrderData orderData,
           int color,
           Size size,
           LabelDefinition label,
           HeaderDefinition headerDefinition,
           IReadOnlyDictionary<string, Componentvalue> componentLookup,
           IReadOnlyDictionary<string, string> assetLookup)
        {
            var fields = new List<string>();

            foreach (var baseField in headerDefinition.BaseFields)
            {
                fields.Add(ResolveBaseFieldValue(baseField, orderData, color, size, label));
            }

            foreach (var componentName in headerDefinition.Components)
            {
                if (label.HasComponent(componentName))
                {
                    componentLookup.TryGetValue(componentName, out var componentValue);
                    var value = ResolveComponentValue(componentValue, orderData, color, size.size);
                    fields.Add(NormalizeComponentOrAssetValue(componentName, value));
                }
                else
                {
                    fields.Add(string.Empty);
                }
            }

            foreach (var assetName in headerDefinition.Assets)
            {
                if (label.HasAsset(assetName))
                {
                    assetLookup.TryGetValue(assetName, out var assetValue);
                    fields.Add(NormalizeComponentOrAssetValue(assetName, assetValue ?? string.Empty));
                }
                else
                {
                    fields.Add(string.Empty);
                }
            }

            return string.Join(Delimeter.ToString(), fields.Select(EscapeCsvValue));
        }
        private static IReadOnlyDictionary<string, Componentvalue> BuildComponentLookup(IEnumerable<Componentvalue> componentValues)
        {
            var map = new Dictionary<string, Componentvalue>(StringComparer.OrdinalIgnoreCase);
            if (componentValues == null)
                return map;

            foreach (var component in componentValues)
            {
                if (string.IsNullOrWhiteSpace(component?.name))
                    continue;

                if (!map.ContainsKey(component.name))
                {
                    map.Add(component.name, component);
                }
            }

            return map;
        }

        private static IReadOnlyDictionary<string, string> BuildAssetLookup(IEnumerable<Asset> assets)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (assets == null)
                return map;

            foreach (var asset in assets)
            {
                if (string.IsNullOrWhiteSpace(asset?.name))
                    continue;

                if (!map.ContainsKey(asset.name))
                {
                    map.Add(asset.name, asset.value ?? string.Empty);
                }
            }

            return map;
        }

        private static HeaderDefinition BuildHeaderDefinition(IEnumerable<LabelDefinition> labels, string labelType, ILogService log)
        {
            var labelList = labels?.ToList() ?? new List<LabelDefinition>();
            if (LabelSchemaRegistry.TryResolveSchema(
                labelType,
                labelList.Select(label => label.Reference),
                out var schema,
                out var resolvedBy))
            {
                log?.LogMessage($"JsonToTextConverter: schema resuelto por {resolvedBy} para tipo '{labelType ?? "N/A"}'.");
                return new HeaderDefinition(schema.BaseFields, schema.Components, schema.Assets);
            }

            log?.LogMessage("JsonToTextConverter: no se pudo resolver esquema fijo, usando componentes/assets del pedido.");

            var components = new List<string>();
            var assets = new List<string>();
            var componentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var label in labelList)
            {
                foreach (var component in label.Components)
                {
                    if (componentSet.Add(component))
                        components.Add(component);
                }

                foreach (var asset in label.Assets)
                {
                    if (assetSet.Add(asset))
                        assets.Add(asset);
                }
            }

            return new HeaderDefinition(LabelSchemaRegistry.ExternalSchema.BaseFields, components, assets);
        }

        private static string BuildHeaderLine(HeaderDefinition header)
        {
            var fields = header.BaseFields.Select(field => field.Header).ToList();
            fields.AddRange(header.Components.Select(component => component));
            fields.AddRange(header.Assets.Select(asset => asset));

            return string.Join(Delimeter.ToString(), fields.Select(EscapeCsvValue));
        }

        private static string EscapeCsvValue(string value)
        {
            if (value == null)
                return string.Empty;

            var mustQuote = value.Contains(Delimeter.ToString()) || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!mustQuote)
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }


    }
}
