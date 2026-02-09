using StructureInditexOrderFile;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Inditex.ZaraHangtagKids.Tests
{
    public class LabelSchemaRegistryTests
    {
        [Fact]
        public void ExternalSchema_UsaBaseFieldsDerivadosDePropiedades()
        {
            var expected = BuildExpectedBaseFields();

            var actual = LabelSchemaRegistry.ExternalSchema.BaseFields
                .Select(field => (field.Header, field.Path))
                .ToList();

            Assert.Equal(expected, actual);
        }

        private static IReadOnlyList<(string Header, string Path)> BuildExpectedBaseFields()
        {
            return new List<(string Header, string Path)>
            {
                ("ProductionOrderNumber", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.PONumber))),
                ("Campaign", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.Campaign))),
                ("Brand", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.BrandRfid))),
                ("Section", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.SectionRfid))),
                ("ProductType", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.ProductTypeRfid))),
                ("Model", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.ModelRfid))),
                ("Quality", BuildPath(nameof(InditexOrderData.POInformation), nameof(Poinformation.QualityRfid))),
                ("Color", BuildPath(nameof(Color), nameof(Color.ColorRfid))),
                ("Size", BuildPath(nameof(Size), nameof(Size.SizeRfid))),
                ("Quantity", BuildPath(nameof(Size), nameof(Size.Qty))),
                ("LabelReference", BuildPath(nameof(Label), nameof(Label.Reference)))
            };
        }

        private static string BuildPath(string parent, string child)
        {
            return $"{parent}.{child}";
        }
    }
}
