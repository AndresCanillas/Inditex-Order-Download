using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inidtex.ZaraExterlLables
{
    public static partial class JsonToTextConverter
    {

        private static partial class LabelDefinitionBuilder
        {
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
