using Inidtex.ZaraExterlLables;
using Newtonsoft.Json;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Inditex.ZaraHangtagKids.Tests
{
    public class JsonToTextConverterTests
    {
        [Fact]
        public void LoadData_GeneraHeaderYFilasEsperadas()
        {
            var orderData = LoadSampleOrder();

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);

            Assert.True(lines.Length > 1, "Se esperaba un header y al menos una fila de datos.");

            var header = SplitCsvLine(lines[0]);
            var expectedHeader = BuildExpectedHeader(LabelSchemaRegistry.ExternalSchema);
            Assert.Equal(expectedHeader, header);

            var expectedLabels = CountLabels(orderData.labels);
            var expectedSizes = orderData.POInformation.colors.Sum(c => c.sizes.Length);
            var expectedRows = expectedLabels * expectedSizes;

            Assert.Equal(expectedRows + 1, lines.Length);
        }

        [Fact]
        public void LoadData_ResuelveComponentesYAssetsPorItem()
        {
            var orderData = LoadSampleOrder();
            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);

            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZKALL003", size: "18", color: "711");

            var qrIndex = Array.IndexOf(header, "QR_product");
            var colorIndex = Array.IndexOf(header, "Colour");
            var assetIndex = Array.LastIndexOf(header, "Icono RFID");

            Assert.True(qrIndex >= 0);
            Assert.True(colorIndex >= 0);
            Assert.True(assetIndex >= 0);

            var expectedQr = GetComponentValue(orderData, "QR_product", "18");
            var expectedColor = GetComponentValue(orderData, "Colour", "711");
            var expectedAsset = orderData.assets.First(a => a.name == "Icono RFID").value;

            Assert.Equal(expectedQr, row[qrIndex]);
            Assert.Equal(expectedColor, row[colorIndex]);
            Assert.Equal(expectedAsset, row[assetIndex]);
        }

        [Fact]
        public void LoadData_IncluyeAssetsDeChildrenLabels()
        {
            var orderData = BuildOrderWithChildAsset();

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZCHILD001", size: "40", color: "123");
            var assetIndex = Array.LastIndexOf(header, "Icono RFID");

            Assert.True(assetIndex >= 0);
            Assert.Equal("RFID-CHILD", row[assetIndex]);
        }

        private static InditexOrderData LoadSampleOrder()
        {
            var path = ResolvePath("Plugins", "Zara", "OrderFiles", "15536_05987_I25_NNO_ZARANORTE.json");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<InditexOrderData>(json);
        }

        private static string ResolvePath(params string[] segments)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.GetFullPath(Path.Combine(baseDir, "../../../../"));
            return Path.Combine(path, Path.Combine(segments));
        }

        private static string[] SplitLines(string text)
        {
            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] SplitCsvLine(string line)
        {
            return line.Split(ClientDefinitions.delimeter);
        }

        private static int CountLabels(IEnumerable<Label> labels)
        {
            var total = 0;
            foreach(var label in labels ?? Array.Empty<Label>())
            {
                total++;
                if(label?.childrenLabels != null)
                {
                    total += label.childrenLabels.Length;
                }
            }

            return total;
        }

        private static string[] FindRow(
            IReadOnlyList<string[]> rows,
            string[] header,
            string labelReference,
            string size,
            string color)
        {
            var labelIndex = Array.IndexOf(header, "LabelReference");
            var sizeIndex = Array.IndexOf(header, "Size");
            var colorIndex = Array.IndexOf(header, "Color");

            foreach(var row in rows)
            {
                if(row[labelIndex] == labelReference && row[sizeIndex] == size && row[colorIndex] == color)
                    return row;
            }

            throw new InvalidOperationException("No se encontró una fila que cumpla los criterios de búsqueda.");
        }

        private static string GetComponentValue(InditexOrderData orderData, string componentName, string key)
        {
            var component = orderData.componentValues.First(c => c.name == componentName);
            if(component.valueMap is Dictionary<string, string> map)
                return map[key];

            var mapObj = (Newtonsoft.Json.Linq.JObject)component.valueMap;
            return mapObj[key]?.ToString();
        }

        private static string[] BuildExpectedHeader(LabelSchemaDefinition schema)
        {
            var fields = schema.BaseFields.Select(field => field.Header).ToList();
            fields.AddRange(schema.Components);
            fields.AddRange(schema.Assets);

            return fields.ToArray();
        }

        private static InditexOrderData BuildOrderWithChildAsset()
        {
            return new InditexOrderData
            {
                POInformation = new Poinformation
                {
                    productionOrderNumber = "PO-CHILD",
                    campaign = "C1",
                    brand = "Z",
                    section = "SEC",
                    productType = "TYPE",
                    model = 100,
                    quality = 200,
                    colors = new[]
                    {
                        new Color
                        {
                            color = 123,
                            sizes = new[]
                            {
                                new Size { size = 40, qty = 1 }
                            }
                        }
                    }
                },
                assets = new[]
                {
                    new Asset { name = "Icono RFID", value = "RFID-CHILD" }
                },
                componentValues = Array.Empty<Componentvalue>(),
                labels = new[]
                {
                    new Label
                    {
                        reference = "HPZPARENT001",
                        components = Array.Empty<string>(),
                        assets = Array.Empty<string>(),
                        childrenLabels = new[]
                        {
                            new Childrenlabel
                            {
                                reference = "HPZCHILD001",
                                components = Array.Empty<string>(),
                                assets = new[] { "Icono RFID" },
                                childrenLabels = null
                            }
                        }
                    }
                }
            };
        }
    }
}
