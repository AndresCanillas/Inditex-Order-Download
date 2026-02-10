using Inidtex.ZaraExterlLables;
using Moq;
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

            var expectedLabels = CountLabelsExcludingPiggybacks(orderData.labels);
            var expectedSizes = orderData.POInformation.Colors.Sum(c => c.Sizes.Length);
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

            var row = FindRow(rows, header, labelReference: "HPZKALL0032", size: "18", color: "711");

            var qrIndex = Array.IndexOf(header, "QR_product");
            var colorIndex = Array.IndexOf(header, "Colour");
            var assetIndex = Array.LastIndexOf(header, "Icono RFID");
            var buyerGroupIndex = Array.IndexOf(header, "BuyerGroup_Icon");

            Assert.True(qrIndex >= 0);
            Assert.True(colorIndex >= 0);
            Assert.True(assetIndex >= 0);
            Assert.True(buyerGroupIndex >= 0);

            var expectedQr = "Por resolver";
            var expectedColor = GetComponentValue(orderData, "Colour", "711");
            var expectedAsset = "rfid_alarm";
            var expectedBuyerGroup = "BABY_GIRL";

            Assert.Equal(expectedQr, row[qrIndex]);
            Assert.Equal(expectedColor, row[colorIndex]);
            Assert.Equal(expectedAsset, row[assetIndex]);
            Assert.Equal(expectedBuyerGroup, row[buyerGroupIndex]);
        }

        [Fact]
        public void LoadData_ConcatenaReferenciaYPiggybacksEnLineaHPZ()
        {
            var orderData = LoadSampleOrder();
            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var labelIndex = Array.IndexOf(header, "LabelReference");
            Assert.True(labelIndex >= 0);

            Assert.DoesNotContain(rows, row => row[labelIndex] == "BLUE_LABEL");
            Assert.DoesNotContain(rows, row => row[labelIndex] == "RED_LABEL");
            Assert.Contains(rows, row => row[labelIndex] == "HPZKALL0032");
        }

        [Fact]
        public void LoadData_CuandoSoloBluePiggyback_ConcatenaUnoYCopiaDatos()
        {
            var orderData = BuildOrderWithBluePiggyback();
            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZBLUE0011", size: "40", color: "123");
            var blueIndex = Array.IndexOf(header, "Blue label");
            var assetIndex = Array.LastIndexOf(header, "Icono RFID");

            Assert.True(blueIndex >= 0);
            Assert.True(assetIndex >= 0);
            Assert.Equal("22,95", row[blueIndex]);
            Assert.Equal("rfid_alarm", row[assetIndex]);
        }

        [Fact]
        public void LoadData_CuandoSoloRedPiggyback_RegistraError()
        {
            var orderData = BuildOrderWithRedPiggybackOnly();
            var log = new Mock<Services.Core.ILogService>();

            JsonToTextConverter.LoadData(orderData, log.Object, labelType: LabelSchemaRegistry.ExternalPluginType);

            log.Verify(
                logger => logger.LogWarning(It.Is<string>(message => message.Contains("RED_LABEL"))),
                Times.Once);
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

        [Fact]
        public void LoadData_QuoteaValoresConDelimitador()
        {
            var orderData = BuildOrderWithDelimitedComponentValue();

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);

            Assert.True(lines.Length > 1, "Se esperaba un header y al menos una fila de datos.");

            var dataLine = lines[1];
            Assert.Contains("\"Value;With;Semicolon\"", dataLine);
        }

        [Fact]
        public void LoadData_QuoteaValoresConComillas()
        {
            var orderData = BuildOrderWithQuotedComponentValue();

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);

            Assert.True(lines.Length > 1, "Se esperaba un header y al menos una fila de datos.");

            var dataLine = lines[1];
            Assert.Contains("\"Value \"\"Quoted\"\"\"", dataLine);
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
            return line.Split(';');
        }

        private static int CountLabelsExcludingPiggybacks(IEnumerable<Label> labels)
        {
            var total = 0;
            foreach(var label in labels ?? Array.Empty<Label>())
            {
                if (IsPiggyback(label?.Reference))
                    continue;

                total++;
                total += CountChildLabels(label?.ChildrenLabels);
            }

            return total;
        }

        private static int CountChildLabels(IEnumerable<Childrenlabel> children)
        {
            if (children == null)
                return 0;

            var total = 0;
            foreach (var child in children)
            {
                if (child == null || IsPiggyback(child.Reference))
                    continue;

                total++;
                if (child.ChildrenLabels == null)
                    continue;

                foreach (var nested in child.ChildrenLabels)
                {
                    if (nested is Childrenlabel nestedChild)
                        total += CountChildLabels(new[] { nestedChild });
                    else if (nested is Newtonsoft.Json.Linq.JObject nestedObject)
                        total += CountChildLabels(new[] { nestedObject.ToObject<Childrenlabel>() });
                }
            }

            return total;
        }

        private static bool IsPiggyback(string reference)
        {
            return string.Equals(reference, "BLUE_LABEL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reference, "RED_LABEL", StringComparison.OrdinalIgnoreCase);
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
            var component = orderData.ComponentValues.First(c => c.Name == componentName);
            if(component.ValueMap is Dictionary<string, string> map)
                return map[key];

            var mapObj = (Newtonsoft.Json.Linq.JObject)component.ValueMap;
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
                    PONumber = "PO-CHILD",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section = "SEC",
                    ProductType_Text = "TYPE",
                    ModelRfid = 100,
                    QualityRfid = 200,
                    Colors = new[]
                    {
                        new Color
                        {
                            ColorRfid = 123,
                            Sizes = new[]
                            {
                                new Size { SizeRfid = 40, Size_Qty = 1 }
                            }
                        }
                    }
                },
                Assets = new[]
                {
                    new Asset { Name = "Icono RFID", Value = "RFID-CHILD" }
                },
                ComponentValues = Array.Empty<Componentvalue>(),
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZPARENT001",
                        Components = Array.Empty<string>(),
                        Assets = Array.Empty<string>(),
                        ChildrenLabels = new[]
                        {
                            new Childrenlabel
                            {
                                Reference = "HPZCHILD001",
                                Components = Array.Empty<string>(),
                                Sssets = new[] { "Icono RFID" },
                                ChildrenLabels = null
                            }
                        }
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithBluePiggyback()
        {
            return new InditexOrderData
            {
                POInformation = new Poinformation
                {
                    PONumber = "PO-BLUE",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section = "SEC",
                    ProductType_Text = "TYPE",
                    ModelRfid = 100,
                    QualityRfid = 200,
                    Colors = new[]
                    {
                        new Color
                        {
                            ColorRfid = 123,
                            Sizes = new[]
                            {
                                new Size { SizeRfid = 40, Size_Qty = 1 }
                            }
                        }
                    }
                },
                Assets = new[]
                {
                    new Asset { Name = "Icono RFID", Value = "https://static.inditex.com/rfid_alarm.png?ts=1" }
                },
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "MODEL_QUALITY",
                        Name = "Blue label",
                        ValueMap = new Dictionary<string, string> { ["100/200"] = "22,95" }
                    }
                },
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZBLUE001",
                        Components = Array.Empty<string>(),
                        Assets = Array.Empty<string>(),
                        ChildrenLabels = new[]
                        {
                            new Childrenlabel
                            {
                                Reference = "BLUE_LABEL",
                                Components = new[] { "Blue label" },
                                Sssets = new[] { "Icono RFID" },
                                ChildrenLabels = Array.Empty<object>()
                            }
                        }
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithRedPiggybackOnly()
        {
            return new InditexOrderData
            {
                POInformation = new Poinformation
                {
                    PONumber = "PO-RED",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section = "SEC",
                    ProductType_Text = "TYPE",
                    ModelRfid = 100,
                    QualityRfid = 200,
                    Colors = new[]
                    {
                        new Color
                        {
                            ColorRfid = 123,
                            Sizes = new[]
                            {
                                new Size { SizeRfid = 40, Size_Qty = 1 }
                            }
                        }
                    }
                },
                Assets = Array.Empty<Asset>(),
                ComponentValues = Array.Empty<Componentvalue>(),
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZRED001",
                        Components = Array.Empty<string>(),
                        Assets = Array.Empty<string>(),
                        ChildrenLabels = new[]
                        {
                            new Childrenlabel
                            {
                                Reference = "RED_LABEL",
                                Components = new[] { "Red label" },
                                Sssets = Array.Empty<string>(),
                                ChildrenLabels = Array.Empty<object>()
                            }
                        }
                    }
                }
            };
        }
        private static InditexOrderData BuildOrderWithDelimitedComponentValue()
        {
            return new InditexOrderData
            {
                POInformation = new Poinformation
                {
                    PONumber = "PO-DELIM",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section = "SEC",
                    ProductType_Text = "TYPE",
                    ModelRfid = 100,
                    QualityRfid = 200,
                    Colors = new[]
                    {
                        new Color
                        {
                            ColorRfid = 711,
                            Sizes = new[]
                            {
                                new Size { SizeRfid = 18, Size_Qty = 1 }
                            }
                        }
                    }
                },
                Assets = Array.Empty<Asset>(),
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "COLOR",
                        Name = "Colour",
                        ValueMap = new Dictionary<string, string>
                        {
                            { "711", "Value;With;Semicolon" }
                        }
                    }
                },
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZKALL0032",
                        Components = new[] { "Colour" },
                        Assets = Array.Empty<string>()
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithQuotedComponentValue()
        {
            return new InditexOrderData
            {
                POInformation = new Poinformation
                {
                    PONumber = "PO-QUOTES",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section = "SEC",
                    ProductType_Text = "TYPE",
                    ModelRfid = 100,
                    QualityRfid = 200,
                    Colors = new[]
                    {
                        new Color
                        {
                            ColorRfid = 711,
                            Sizes = new[]
                            {
                                new Size { SizeRfid = 18, Size_Qty = 1 }
                            }
                        }
                    }
                },
                Assets = Array.Empty<Asset>(),
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "COLOR",
                        Name = "Colour",
                        ValueMap = new Dictionary<string, string>
                        {
                            { "711", "Value \"Quoted\"" }
                        }
                    }
                },
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZKALL0032",
                        Components = new[] { "Colour" },
                        Assets = Array.Empty<string>()
                    }
                }
            };
        }
    }
}
