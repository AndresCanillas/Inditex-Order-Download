using Newtonsoft.Json.Linq;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inidtex.ZaraExterlLables
{
    public static partial class JsonToTextConverter
    {
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

        private static partial class LabelDefinitionBuilder
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
        }
    }
}
