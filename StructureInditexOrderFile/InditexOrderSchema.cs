using System;
using System.Collections.Generic;
using System.Linq;

namespace StructureInditexOrderFile
{
    public static class InditexOrderSchema
    {
        public static class Paths
        {
            public static readonly string ProductionOrderNumber = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.PONumber));
            public static readonly string Campaign = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.Campaign));
            public static readonly string Brand = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.BrandRfid));
            public static readonly string Section = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.SectionRfid));
            public static readonly string ProductType = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.ProductTypeRfid));
            public static readonly string Model = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.ModelRfid));
            public static readonly string Quality = BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.QualityRfid));
            public static readonly string Color = BuildPath(nameof(StructureInditexOrderFile.Color), nameof(StructureInditexOrderFile.Color.ColorRfid));
            public static readonly string Size = BuildPath(nameof(StructureInditexOrderFile.Size), nameof(StructureInditexOrderFile.Size.SizeRfid));
            public static readonly string Quantity = BuildPath(nameof(StructureInditexOrderFile.Size), nameof(StructureInditexOrderFile.Size.Qty));
            public static readonly string LabelReference = BuildPath(nameof(Label), nameof(Label.Reference));
        }

        public static IReadOnlyList<InditexFieldDefinition> DefaultBaseFields { get; } = BuildDefaultBaseFields();

        private static IReadOnlyList<InditexFieldDefinition> BuildDefaultBaseFields()
        {
            var fields = new[]
            {
                new InditexFieldDefinition(nameof(Poinformation.PONumber), Paths.ProductionOrderNumber),
                new InditexFieldDefinition(nameof(Poinformation.Campaign), Paths.Campaign),
                new InditexFieldDefinition(nameof(Poinformation.BrandRfid), Paths.Brand),
                new InditexFieldDefinition(nameof(Poinformation.SectionRfid), Paths.Section),
                new InditexFieldDefinition(nameof(Poinformation.ProductTypeRfid), Paths.ProductType),
                new InditexFieldDefinition(nameof(Poinformation.ModelRfid), Paths.Model),
                new InditexFieldDefinition(nameof(Poinformation.QualityRfid), Paths.Quality),
                new InditexFieldDefinition(nameof(Color.ColorRfid), Paths.Color),
                new InditexFieldDefinition(nameof(Size.SizeRfid), Paths.Size),
                new InditexFieldDefinition(nameof(Size.Qty), Paths.Quantity),
                new InditexFieldDefinition(nameof(Label.Reference), Paths.LabelReference)
            };

            return fields.ToList().AsReadOnly();
        }

        private static string BuildPath(string parent, string child)
        {
            return $"{parent}.{child}";
        }
    }
}
