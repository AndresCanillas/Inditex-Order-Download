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
                ("ProductionOrderNumber", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.PONumber))),
                ("Campaign", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.Campaign))),
                ("Brand", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.Brand_Text))),
                ("Section", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.Section_Text))),
                ("ProductType", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.ProductType_Text))),
                ("Model", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.ModelRfid))),
                ("Quality", BuildPath(nameof(InditexOrderData.ProductionOrder), nameof(ProductionOrder.QualityRfid))),
                ("Color", BuildPath(nameof(Color), nameof(Color.ColorRfid))),
                ("Size", BuildPath(nameof(Size), nameof(Size.SizeRfid))),
                ("Quantity", BuildPath(nameof(Size), nameof(Size.Size_Qty))),
                ("LabelReference", BuildPath(nameof(Label), nameof(Label.Reference)))
            };
        }

        private static string BuildPath(string parent, string child)
        {
            return $"{parent}.{child}";
        }
    }
}
