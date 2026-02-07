using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JsonColor
{
    public static class JsonToTextConverter
    {
        public static string LoadData(InditexOrderData orderData, ILogService log = null, IConnectionManager connMng = null, int projectID = 0, string labelType = null)
        {
            if (orderData == null)
                throw new ArgumentNullException(nameof(orderData));
            if (orderData.POInformation == null)
                throw new ArgumentException("POInformation no puede ser nulo.", nameof(orderData));

            var labels = FlattenLabels(orderData.labels).ToList();
            var headerDefinition = BuildHeaderDefinition(labels);
            var headerLine = BuildHeaderLine(headerDefinition, labelType);

            var sb = new StringBuilder();
            sb.AppendLine(headerLine);

            var componentLookup = BuildComponentLookup(orderData.componentValues);
            var assetLookup = BuildAssetLookup(orderData.assets);

            foreach (var color in orderData.POInformation.colors ?? Array.Empty<Color>())
            {
                foreach (var size in color.sizes ?? Array.Empty<Size>())
                {
                    foreach (var label in labels)
                    {
                        var line = BuildDataLine(orderData, color.color, size, label, headerDefinition, componentLookup, assetLookup);
                        sb.AppendLine(line);
                    }
                }
            }

            return sb.ToString();
        }

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
            var fields = new List<string>
            {
                orderData.POInformation.productionOrderNumber,
                orderData.POInformation.campaign,
                orderData.POInformation.brand,
                orderData.POInformation.section,
                orderData.POInformation.productType,
                orderData.POInformation.model.ToString(CultureInfo.InvariantCulture),
                orderData.POInformation.quality.ToString(CultureInfo.InvariantCulture),
                color.ToString(CultureInfo.InvariantCulture),
                size.size.ToString(CultureInfo.InvariantCulture),
                size.qty.ToString(CultureInfo.InvariantCulture),
                label.Reference
            };

            foreach (var componentName in headerDefinition.Components)
            {
                if (label.HasComponent(componentName))
                {
                    componentLookup.TryGetValue(componentName, out var componentValue);
                    fields.Add(ResolveComponentValue(componentValue, orderData, color, size.size));
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
                    fields.Add(assetValue ?? string.Empty);
                }
                else
                {
                    fields.Add(string.Empty);
                }
            }

            return string.Join(ClientDefinitions.delimeter.ToString(), fields.Select(EscapeCsvValue));
        }

        private static string ResolveComponentValue(Componentvalue componentValue, InditexOrderData orderData, int color, int size)
        {
            if (componentValue == null || componentValue.valueMap == null)
                return string.Empty;

            var key = BuildValueMapKey(componentValue.groupKey, orderData, color, size);
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var valueMap = NormalizeValueMap(componentValue.valueMap);
            return valueMap.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static string BuildValueMapKey(string groupKey, InditexOrderData orderData, int color, int size)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                return null;

            switch (groupKey.Trim().ToUpperInvariant())
            {
                case "MODEL_QUALITY":
                    return $"{orderData.POInformation.model}/{orderData.POInformation.quality}";
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

        private static HeaderDefinition BuildHeaderDefinition(IEnumerable<LabelDefinition> labels)
        {
            var components = new List<string>();
            var assets = new List<string>();
            var componentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var label in labels)
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

            return new HeaderDefinition(components, assets);
        }

        private static string BuildHeaderLine(HeaderDefinition header, string labelType)
        {
            //Codex: dependiendo el tipo de etieta tendremos unos componte base que debe aparacer siempre en el mismo ordern, los agruparemos por etiquetas externas e internas con lo cual tedremos un plugin para cada grupo, agrupoados por:
            // -External:
            //HPZ => Hangtag Price - Zara - Man/Woman/Kids/ Woman (LINGERIE)
            //WTZ => WAIST TAG - Zara - MAN / WOMAN / KIDS
            //ADZ => ADDITIONAL HT, ENVELOPE - Zara - MAN / WOMAN / KIDS / COLLAR STAYS

            // -Internal: 
            //WLZ => Woven Label - Zara - Man/Woman
            //WPZ => Woven Printed - Zara - Man/Woman
            //PLZ => TEXTRACE - Zara - Man/Woman
            //OTZ => TERMO - Zara - Man/Woman
            // la lista de componentes debe ser un dll conpartida entre los dos tipo de plugins, la idea es poder recuperar del json los componentes que correspondan a cada tipo de plugin, por ejemplo, el componente "Price" solo debería aparecer solo en el plugin de external ya que solo esta en las etiquetas HPZ,
            // etc. De esta forma, cada plugin tendría una lista de componentes base que se incluirían siempre en el mismo orden en el archivo de salida.

            //Codex: para determinar que conmpontes estaran en cada en el headers de cada pluguin, es necesario analizar los json de pedidos que estan en la carpeta de OrderFiles (15536_05987_I25_NNO_ZARANORTE, 30049_06462_I25_CBO_ZARANORTE,54709_05039_I25_SRA_ZARASUR de momento solo tendremos HTZ, en el futuro agregareamos más fichero con otrso tipos de referencias), identificar que componentes aparecen en cada tipo de etiqueta sgun las 3 primera letras de su referencia (HPZ, WTZ, ADZ, WLZ, WPZ, PLZ, OTZ) y
            //crear una estructura compartida entre los plugins donde se definan los componentes base para cada tipo de etiqueta. Esta estructura podría ser una clase estática con propiedades o campos que representen cada componente, o un archivo de configuración que se cargue en tiempo de ejecución. Lo importante es que esta estructura sea compartida entre los plugins para garantizar la consistencia y facilitar el mantenimiento a futuro.  


            var fields = new List<string>
            {
                "ProductionOrderNumber",
                "Campaign",
                "Brand",
                "Section",
                "ProductType",
                "Model",
                "Quality",
                "Color",
                "Size",
                "Quantity",
                "LabelReference"
            };

            //Codex:Los atributos fijos en el json (como los listado en fields) debemos recuperarlos siempre pos reflexion usando las extructuras compartidas
            //entre los plugins (StructureInditexOrderFile.NetFramework, parecido en el contexto del MangoJsonColor),
            //de esta forma si en el futuro se añaden nuevos atributos al json,
            //si no son relevantes para el plugin no haría falta ni tocar el código, y si son relevantes pero no están en la estructura base,
            //se podrían recuperar por reflexión sin necesidad de modificar la estructura base ni el código de los plugins.

            fields.AddRange(header.Components.Select(component => component));
            fields.AddRange(header.Assets.Select(asset => asset));

            return string.Join(ClientDefinitions.delimeter.ToString(), fields.Select(EscapeCsvValue));
        }

        private static IEnumerable<LabelDefinition> FlattenLabels(IEnumerable<Label> labels)
        {
            if (labels == null)
                yield break;

            foreach (var label in labels)
            {
                if (label == null)
                    continue;

                yield return new LabelDefinition(
                    label.reference,
                    label.components ?? Array.Empty<string>(),
                    label.assets ?? Array.Empty<string>());

                foreach (var child in FlattenChildren(label.childrenLabels))
                {
                    yield return child;
                }
            }
        }

        private static IEnumerable<LabelDefinition> FlattenChildren(IEnumerable<Childrenlabel> children)
        {
            if (children == null)
                yield break;

            foreach (var child in children)
            {
                if (child == null)
                    continue;

                yield return new LabelDefinition(
                    child.reference,
                    child.components ?? Array.Empty<string>(),
                    Array.Empty<string>());

                if (child.childrenLabels == null)
                    continue;

                foreach (var nested in child.childrenLabels)
                {
                    if (nested is Childrenlabel nestedChild)
                    {
                        foreach (var nestedLabel in FlattenChildren(new[] { nestedChild }))
                            yield return nestedLabel;
                    }
                    else if (nested is JObject nestedObject)
                    {
                        var nestedLabel = nestedObject.ToObject<Childrenlabel>();
                        foreach (var nestedLabelDefinition in FlattenChildren(new[] { nestedLabel }))
                            yield return nestedLabelDefinition;
                    }
                }
            }
        }

        private static string EscapeCsvValue(string value)
        {
            if (value == null)
                return string.Empty;

            var mustQuote = value.Contains(ClientDefinitions.delimeter.ToString()) || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!mustQuote)
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private sealed class LabelDefinition
        {
            public LabelDefinition(string reference, IEnumerable<string> components, IEnumerable<string> assets)
            {
                Reference = reference ?? string.Empty;
                Components = (components ?? Array.Empty<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                Assets = (assets ?? Array.Empty<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            }

            public string Reference { get; }
            public IReadOnlyList<string> Components { get; }
            public IReadOnlyList<string> Assets { get; }

            public bool HasComponent(string componentName)
            {
                return Components.Contains(componentName, StringComparer.OrdinalIgnoreCase);
            }

            public bool HasAsset(string assetName)
            {
                return Assets.Contains(assetName, StringComparer.OrdinalIgnoreCase);
            }
        }

        private sealed class HeaderDefinition
        {
            public HeaderDefinition(IReadOnlyList<string> components, IReadOnlyList<string> assets)
            {
                Components = components ?? Array.Empty<string>();
                Assets = assets ?? Array.Empty<string>();
            }

            public IReadOnlyList<string> Components { get; }
            public IReadOnlyList<string> Assets { get; }
        }
    }
}
