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
            public static readonly string Color = BuildPath(nameof(Color), nameof(Color.ColorRfid));
            public static readonly string Size = BuildPath(nameof(Size), nameof(Size.SizeRfid));
            public static readonly string Quantity = BuildPath(nameof(Size), nameof(Size.Qty));
            public static readonly string LabelReference = BuildPath(nameof(Label), nameof(Label.Reference));
        }

        public static IReadOnlyList<InditexFieldDefinition> DefaultBaseFields { get; } = BuildDefaultBaseFields();

        private static IReadOnlyList<InditexFieldDefinition> BuildDefaultBaseFields()
        {
            var fields = new[]
            {
                new InditexFieldDefinition("ProductionOrderNumber", Paths.ProductionOrderNumber),
                new InditexFieldDefinition("Campaign", Paths.Campaign),
                new InditexFieldDefinition("Brand", Paths.Brand),
                new InditexFieldDefinition("Section", Paths.Section),
                new InditexFieldDefinition("ProductType", Paths.ProductType),
                new InditexFieldDefinition("Model", Paths.Model),
                new InditexFieldDefinition("Quality", Paths.Quality),
                new InditexFieldDefinition("Color", Paths.Color),
                new InditexFieldDefinition("Size", Paths.Size),
                new InditexFieldDefinition("Quantity", Paths.Quantity),
                new InditexFieldDefinition("LabelReference", Paths.LabelReference)
            };

            return fields.ToList().AsReadOnly();
        }

        private static string BuildPath(string parent, string child)
        {
            return $"{parent}.{child}";
        }
    }
}
