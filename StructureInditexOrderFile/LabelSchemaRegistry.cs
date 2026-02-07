using System;
using System.Collections.Generic;
using System.Linq;

namespace StructureInditexOrderFile
{
    public sealed class FieldDefinition
    {
        public FieldDefinition(string header, string path)
        {
            Header = header ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public string Header { get; }
        public string Path { get; }
    }

    public sealed class LabelSchemaDefinition
    {
        public LabelSchemaDefinition(IEnumerable<FieldDefinition> baseFields, IEnumerable<string> components, IEnumerable<string> assets)
        {
            BaseFields = (baseFields ?? Array.Empty<FieldDefinition>()).ToList().AsReadOnly();
            Components = (components ?? Array.Empty<string>()).ToList().AsReadOnly();
            Assets = (assets ?? Array.Empty<string>()).ToList().AsReadOnly();
        }

        public IReadOnlyList<FieldDefinition> BaseFields { get; }
        public IReadOnlyList<string> Components { get; }
        public IReadOnlyList<string> Assets { get; }
    }

    public static class LabelSchemaRegistry
    {
        public const string ExternalPluginType = "EXTERNAL";
        public const string InternalPluginType = "INTERNAL";

        private static readonly string[] ExternalLabelPrefixes = { "HPZ", "WTZ", "ADZ", "RED", "BLU" };
        private static readonly string[] InternalLabelPrefixes = { "WLZ", "WPZ", "PLZ", "OTZ" };

        private static readonly IReadOnlyList<FieldDefinition> DefaultBaseFields = new[]
        {
            new FieldDefinition("ProductionOrderNumber", "POInformation.productionOrderNumber"),
            new FieldDefinition("Campaign", "POInformation.campaign"),
            new FieldDefinition("Brand", "POInformation.brand"),
            new FieldDefinition("Section", "POInformation.section"),
            new FieldDefinition("ProductType", "POInformation.productType"),
            new FieldDefinition("Model", "POInformation.model"),
            new FieldDefinition("Quality", "POInformation.quality"),
            new FieldDefinition("Color", "Color.color"),
            new FieldDefinition("Size", "Size.size"),
            new FieldDefinition("Quantity", "Size.qty"),
            new FieldDefinition("LabelReference", "Label.reference")
        };

        private static readonly LabelSchemaDefinition ExternalSchemaDefinition = new LabelSchemaDefinition(
            DefaultBaseFields,
            new[]
            {
                "BuyerGroup_Icon",
                "Colour",
                "Model",
                "PRICE_UK_CURRENCY",
                "PRICE_UK_VALUE",
                "PRICE_USA_CURRENCY",
                "PRICE_USA_VALUE",
                "QR_product",
                "Quality",
                "REFERENCE_BARCODE",
                "SIZE_MEASURE_AGE",
                "SIZE_MEASURE_AGE_UNIT",
                "SIZE_MEASURE_HEIGHT_CM",
                "SIZE_SERIES_AGE",
                "Blue label",
                "Blue label currency",
                "Red label",
                "Red label currency",
                "Icono RFID",
                "SIZE_BR_BRASIL_VALUE",
                "SIZE_EUR_VALUE",
                "SIZE_IT_VALUE",
                "SIZE_MEX_VALUE",
                "SIZE_SERIE",
                "SIZE_UK_VALUE",
                "SIZE_USA_VALUE",
                "PRICE_BR_CURRENCY",
                "PRICE_BR_VALUE"
            },
            new[]
            {
                "Icono RFID"
            });

        private static readonly LabelSchemaDefinition InternalSchemaDefinition = new LabelSchemaDefinition(
            DefaultBaseFields,
            Array.Empty<string>(),
            Array.Empty<string>());

        private static readonly IReadOnlyDictionary<string, LabelSchemaDefinition> SchemaByPluginType =
            new Dictionary<string, LabelSchemaDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                { ExternalPluginType, ExternalSchemaDefinition },
                { InternalPluginType, InternalSchemaDefinition }
            };

        public static LabelSchemaDefinition ExternalSchema => ExternalSchemaDefinition;

        public static bool TryResolveSchema(
            string pluginType,
            IEnumerable<string> labelReferences,
            out LabelSchemaDefinition schema,
            out string resolvedBy)
        {
            resolvedBy = null;
            schema = null;

            if(!string.IsNullOrWhiteSpace(pluginType))
            {
                var trimmed = pluginType.Trim();
                if(SchemaByPluginType.TryGetValue(trimmed, out schema))
                {
                    resolvedBy = "pluginType";
                    return true;
                }

                var prefix = trimmed.Length >= 3 ? trimmed.Substring(0, 3).ToUpperInvariant() : trimmed.ToUpperInvariant();
                if(ExternalLabelPrefixes.Contains(prefix))
                {
                    schema = ExternalSchemaDefinition;
                    resolvedBy = "pluginTypePrefix";
                    return true;
                }

                if(InternalLabelPrefixes.Contains(prefix))
                {
                    schema = InternalSchemaDefinition;
                    resolvedBy = "pluginTypePrefix";
                    return true;
                }
            }

            if(labelReferences != null)
            {
                var prefixes = labelReferences
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Length >= 3 ? r.Substring(0, 3).ToUpperInvariant() : r.ToUpperInvariant())
                    .ToList();

                var hasExternal = prefixes.Any(p => ExternalLabelPrefixes.Contains(p));
                var hasInternal = prefixes.Any(p => InternalLabelPrefixes.Contains(p));

                if(hasExternal && !hasInternal)
                {
                    schema = ExternalSchemaDefinition;
                    resolvedBy = "labelReferences";
                    return true;
                }

                if(hasInternal && !hasExternal)
                {
                    schema = InternalSchemaDefinition;
                    resolvedBy = "labelReferences";
                    return true;
                }
            }

            return false;
        }
    }
}
