using Newtonsoft.Json.Linq;
using Service.Contracts;
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
    public static class JsonToTextConverter
    {
        private const string BluePiggybackReference = "BLUE_LABEL";
        private const string RedPiggybackReference = "RED_LABEL";
        private const string QrProductComponentName = "QR_product";

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

        private static HeaderDefinition BuildHeaderDefinition(IEnumerable<LabelDefinition> labels, string labelType, ILogService log)
        {
            var labelList = labels?.ToList() ?? new List<LabelDefinition>();
            if(LabelSchemaRegistry.TryResolveSchema(
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


            var fields = header.BaseFields.Select(field => field.Header).ToList();

            //Codex:Los atributos fijos en el json (como los listado en fields) debemos recuperarlos siempre pos reflexion usando las extructuras compartidas
            //entre los plugins (StructureInditexOrderFile.NetFramework, parecido en el contexto del MangoInidtex.ZaraExterlLables),
            //de esta forma si en el futuro se añaden nuevos atributos al json,
            //si no son relevantes para el plugin no haría falta ni tocar el código, y si son relevantes pero no están en la estructura base,
            //se podrían recuperar por reflexión sin necesidad de modificar la estructura base ni el código de los plugins.

            fields.AddRange(header.Components.Select(component => component));
            fields.AddRange(header.Assets.Select(asset => asset));

            return string.Join(ClientDefinitions.delimeter.ToString(), fields.Select(EscapeCsvValue));
        }

        private static string EscapeCsvValue(string value)
        {
            return Rfc4180Writer.QuoteValue(value, ClientDefinitions.delimeter);
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

            if(string.Equals(field.Path, "Color.color", StringComparison.OrdinalIgnoreCase))
                return color.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, "Size.size", StringComparison.OrdinalIgnoreCase))
                return size.size.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, "Size.qty", StringComparison.OrdinalIgnoreCase))
                return size.qty.ToString(CultureInfo.InvariantCulture);

            if(string.Equals(field.Path, "Label.reference", StringComparison.OrdinalIgnoreCase))
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

        private static class UriHelper
        {
            public static bool IsUrl(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                return Uri.TryCreate(value, UriKind.Absolute, out _);
            }

            public static string ExtractFileNameWithoutExtension(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return string.Empty;

                var path = value;
                if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    path = uri.AbsolutePath;
                else
                    path = value.Split('?')[0].Split('#')[0];

                var fileName = System.IO.Path.GetFileName(path);
                return System.IO.Path.GetFileNameWithoutExtension(fileName);
            }
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
            public HeaderDefinition(IReadOnlyList<InditexFieldDefinition> baseFields, IReadOnlyList<string> components, IReadOnlyList<string> assets)
            {
                BaseFields = baseFields ?? Array.Empty<InditexFieldDefinition>();
                Components = components ?? Array.Empty<string>();
                Assets = assets ?? Array.Empty<string>();
            }

            public IReadOnlyList<InditexFieldDefinition> BaseFields { get; }
            public IReadOnlyList<string> Components { get; }
            public IReadOnlyList<string> Assets { get; }
        }

        private static class LabelDefinitionBuilder
        {
            public static IEnumerable<LabelDefinition> Build(IEnumerable<Label> labels, ILogService log)
            {
                if (labels == null)
                    yield break;

                foreach (var label in labels)
                {
                    if (label == null || IsPiggyback(label.reference))
                        continue;

                    foreach (var definition in BuildFromLabel(label.reference, label.components, label.assets, label.childrenLabels, log))
                        yield return definition;
                }
            }

            private static IEnumerable<LabelDefinition> BuildFromLabel(
                string reference,
                IEnumerable<string> components,
                IEnumerable<string> assets,
                IEnumerable<Childrenlabel> children,
                ILogService log)
            {
                var childLabels = NormalizeChildren(children);
                var piggybackInfo = PiggybackInfo.FromChildren(reference, childLabels, log);

                var mergedComponents = MergeDistinct(components, piggybackInfo.Components);
                var mergedAssets = MergeDistinct(assets, piggybackInfo.Assets);
                var resolvedReference = piggybackInfo.HasBlue
                    ? $"{reference}{(piggybackInfo.HasRed ? 2 : 1)}"
                    : reference ?? string.Empty;

                yield return new LabelDefinition(resolvedReference, mergedComponents, mergedAssets);

                foreach (var child in childLabels.Where(child => !IsPiggyback(child.reference)))
                {
                    foreach (var childDefinition in BuildFromChild(child, log))
                        yield return childDefinition;
                }
            }

            private static IEnumerable<LabelDefinition> BuildFromChild(Childrenlabel child, ILogService log)
            {
                if (child == null || IsPiggyback(child.reference))
                    yield break;

                foreach (var definition in BuildFromChildData(child.reference, child.components, child.assets, child.childrenLabels, log))
                    yield return definition;
            }

            private static IEnumerable<LabelDefinition> BuildFromChildData(
                string reference,
                IEnumerable<string> components,
                IEnumerable<string> assets,
                IEnumerable<object> children,
                ILogService log)
            {
                var childLabels = NormalizeChildren(children);
                var piggybackInfo = PiggybackInfo.FromChildren(reference, childLabels, log);

                var mergedComponents = MergeDistinct(components, piggybackInfo.Components);
                var mergedAssets = MergeDistinct(assets, piggybackInfo.Assets);
                var resolvedReference = piggybackInfo.HasBlue
                    ? $"{reference}{(piggybackInfo.HasRed ? 2 : 1)}"
                    : reference ?? string.Empty;

                yield return new LabelDefinition(resolvedReference, mergedComponents, mergedAssets);

                foreach (var child in childLabels.Where(child => !IsPiggyback(child.reference)))
                {
                    foreach (var childDefinition in BuildFromChild(child, log))
                        yield return childDefinition;
                }
            }

            private static IReadOnlyList<Childrenlabel> NormalizeChildren(IEnumerable<Childrenlabel> children)
            {
                return children?.Where(child => child != null).ToList()
                    ?? new List<Childrenlabel>();
            }

            private static IReadOnlyList<Childrenlabel> NormalizeChildren(IEnumerable<object> children)
            {
                if (children == null)
                    return new List<Childrenlabel>();

                var normalized = new List<Childrenlabel>();
                foreach (var child in children)
                {
                    if (child is Childrenlabel childLabel)
                        normalized.Add(childLabel);
                    else if (child is JObject childObject)
                        normalized.Add(childObject.ToObject<Childrenlabel>());
                }

                return normalized;
            }

            private static IReadOnlyList<string> MergeDistinct(IEnumerable<string> first, IEnumerable<string> second)
            {
                var items = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                AddRange(first, items, seen);
                AddRange(second, items, seen);

                return items;
            }

            private static void AddRange(IEnumerable<string> values, ICollection<string> output, ISet<string> seen)
            {
                if (values == null)
                    return;

                foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    if (seen.Add(value))
                        output.Add(value);
                }
            }

            private static bool IsPiggyback(string reference)
            {
                return string.Equals(reference, BluePiggybackReference, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(reference, RedPiggybackReference, StringComparison.OrdinalIgnoreCase);
            }

            private sealed class PiggybackInfo
            {
                private PiggybackInfo(bool hasBlue, bool hasRed, IReadOnlyList<string> components, IReadOnlyList<string> assets)
                {
                    HasBlue = hasBlue;
                    HasRed = hasRed;
                    Components = components ?? Array.Empty<string>();
                    Assets = assets ?? Array.Empty<string>();
                }

                public bool HasBlue { get; }
                public bool HasRed { get; }
                public IReadOnlyList<string> Components { get; }
                public IReadOnlyList<string> Assets { get; }

                public static PiggybackInfo FromChildren(string parentReference, IEnumerable<Childrenlabel> children, ILogService log)
                {
                    if (children == null)
                        return new PiggybackInfo(false, false, Array.Empty<string>(), Array.Empty<string>());

                    var piggybacks = children
                        .Where(child => child != null)
                        .Where(child => IsPiggyback(child.reference))
                        .ToList();

                    var hasBlue = piggybacks.Any(child => string.Equals(child.reference, BluePiggybackReference, StringComparison.OrdinalIgnoreCase));
                    var hasRed = piggybacks.Any(child => string.Equals(child.reference, RedPiggybackReference, StringComparison.OrdinalIgnoreCase));

                    if (hasRed && !hasBlue)
                        log?.LogWarning($"Se recibió {RedPiggybackReference} sin {BluePiggybackReference} para la referencia {parentReference}.");

                    var components = piggybacks.SelectMany(child => child.components ?? Array.Empty<string>()).ToList();
                    var assets = piggybacks.SelectMany(child => child.assets ?? Array.Empty<string>()).ToList();

                    return new PiggybackInfo(hasBlue, hasRed, components, assets);
                }
            }
        }
    }
}
