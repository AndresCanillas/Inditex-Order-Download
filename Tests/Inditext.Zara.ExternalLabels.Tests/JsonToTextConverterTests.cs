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
            var expectedSizes = orderData.ProductionOrder.Colors.Sum(c => c.Sizes.Length);
            var expectedRows = expectedLabels * expectedSizes;

            Assert.Equal(expectedRows + 1, lines.Length);
        }

        [Fact]
        public void LoadData_ResuelveComponentesYAssetsPorItem()
        {
            var orderData = BuildOrderWithNewComponentNames();
            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);

            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZNEW0012", size: "18", color: "711");

            var qrIndex = Array.IndexOf(header, "PRODUCT_QR");
            var colorIndex = Array.IndexOf(header, "PRODUCT_COLOR");
            var assetIndex = Array.LastIndexOf(header, "ICON_RFID");
            var buyerGroupIndex = Array.IndexOf(header, "ICON_BUYER_GROUP");

            Assert.True(qrIndex >= 0);
            Assert.True(colorIndex >= 0);
            Assert.True(assetIndex >= 0);
            Assert.True(buyerGroupIndex >= 0);

            Assert.Equal("Por resolver", row[qrIndex]);
            Assert.Equal("711", row[colorIndex]);
            Assert.Equal("rfid_alarm", row[assetIndex]);
            Assert.Equal("BABY_GIRL", row[buyerGroupIndex]);
        }


        [Fact]
        public void LoadData_CuandoJsonV26UsaNombresNuevos_ResuelveComponentes()
        {
            var orderData = LoadOrderFromWebApiTests("14185_08574_V26 NEW.json");

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var rows = lines.Skip(1)
                .Select(SplitCsvLine)
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZCALL0042", size: "5", color: "401");

            var buyerGroupIndex = Array.IndexOf(header, "ICON_BUYER_GROUP");
            var barcodeIndex = Array.IndexOf(header, "PRODUCT_BARCODE");
            var qrIndex = Array.IndexOf(header, "PRODUCT_QR");
            var eurSizeIndex = Array.IndexOf(header, "SIZE_GEOGRAPHIC_EUR");

            Assert.True(buyerGroupIndex >= 0);
            Assert.True(barcodeIndex >= 0);
            Assert.True(qrIndex >= 0);
            Assert.True(eurSizeIndex >= 0);

            Assert.Equal("GLOBAL_BABY_BOY", row[buyerGroupIndex]);
            Assert.Equal("08574801401059", row[barcodeIndex]);
            Assert.Equal("08574801401059", row[qrIndex]);
            Assert.Equal("XL", row[eurSizeIndex]);
        }

        [Fact]
        public void LoadData_CuandoExisteProductBarcode_PRODUCTQRUsaMismoValorSegunValueMap()
        {
            var orderData = new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-QR-BARCODE",
                    Campaign = "V26",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                    new Componentvalue { GroupKey = "SIZE", Name = "PRODUCT_QR", ValueMap = new Dictionary<string, string> { ["18"] = "https://example.com/qr.svg" } },
                    new Componentvalue { GroupKey = "SIZE", Name = "PRODUCT_BARCODE", ValueMap = new Dictionary<string, string> { ["18"] = "1234567890123" } }
                },
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZQRCODE001",
                        Components = new[] { "PRODUCT_QR", "PRODUCT_BARCODE" },
                        Assets = Array.Empty<string>()
                    }
                }
            };

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var row = SplitCsvLine(lines[1]);

            var qrIndex = Array.IndexOf(header, "PRODUCT_QR");
            var barcodeIndex = Array.IndexOf(header, "PRODUCT_BARCODE");

            Assert.True(qrIndex >= 0);
            Assert.True(barcodeIndex >= 0);
            Assert.Equal("1234567890123", row[barcodeIndex]);
            Assert.Equal("1234567890123", row[qrIndex]);
        }


        [Fact]
        public void LoadData_CuandoAssetUsaNombreNuevoIconRfid_ResuelveColumna()
        {
            var orderData = BuildOrderWithRfidAssetAlias();

            var output = JsonToTextConverter.LoadData(orderData, labelType: LabelSchemaRegistry.ExternalPluginType);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);
            var row = SplitCsvLine(lines[1]);

            var assetIndex = Array.IndexOf(header, "ICON_RFID");
            Assert.True(assetIndex >= 0);
            Assert.Equal("rfid_alias", row[assetIndex]);
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
            var blueIndex = Array.IndexOf(header, "PRICE_BLUE_VALUE");
            var assetIndex = Array.LastIndexOf(header, "ICON_RFID");

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
            var assetIndex = Array.LastIndexOf(header, "ICON_RFID");

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
            var path = ResolvePath("Plugins", "Zara", "OrderFiles", "14313_14801_V26.json");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<InditexOrderData>(json);
        }


        private static InditexOrderData LoadOrderFromWebApiTests(string fileName)
        {
            var path = ResolvePath("OrderDownloadWebApi", "TestOrders", fileName);
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<InditexOrderData>(json);
        }

        private static string ResolvePath(params string[] segments)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.GetFullPath(Path.Combine(baseDir, "../../../../../"));
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



        private static InditexOrderData BuildOrderWithNewComponentNames()
        {
            return new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-NEW-001",
                    Campaign = "V26",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                Assets = new[]
                {
                    new Asset { Name = "ICON_RFID", Value = "https://static.inditex.com/rfid_alarm.png" }
                },
                ComponentValues = new[]
                {
                    new Componentvalue { GroupKey = "COLOR", Name = "PRODUCT_COLOR", ValueMap = new Dictionary<string, string> { ["711"] = "711" } },
                    new Componentvalue { GroupKey = "MODEL_QUALITY", Name = "ICON_BUYER_GROUP", ValueMap = new Dictionary<string, string> { ["100/200"] = "BABY_GIRL" } },
                    new Componentvalue { GroupKey = "SIZE", Name = "PRODUCT_QR", ValueMap = new Dictionary<string, string> { ["18"] = "https://example.com/qr_product_uuid_99999.svg" } }
                },
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZNEW001",
                        Components = new[] { "PRODUCT_COLOR", "ICON_BUYER_GROUP", "PRODUCT_QR" },
                        Assets = new[] { "ICON_RFID" },
                        ChildrenLabels = new[]
                        {
                            new Childrenlabel
                            {
                                Reference = "BLUE_LABEL",
                                Components = Array.Empty<string>(),
                                Sssets = Array.Empty<string>(),
                                ChildrenLabels = Array.Empty<object>()
                            },
                            new Childrenlabel
                            {
                                Reference = "RED_LABEL",
                                Components = Array.Empty<string>(),
                                Sssets = Array.Empty<string>(),
                                ChildrenLabels = Array.Empty<object>()
                            }
                        }
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithRfidAssetAlias()
        {
            return new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-ASSET-ALIAS",
                    Campaign = "V26",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                    new Asset { Name = "ICON_RFID", Value = "https://static.inditex.com/assets/rfid_alias.png" }
                },
                ComponentValues = Array.Empty<Componentvalue>(),
                labels = new[]
                {
                    new Label
                    {
                        Reference = "HPZASSET001",
                        Components = Array.Empty<string>(),
                        Assets = new[] { "ICON_RFID" },
                        ChildrenLabels = Array.Empty<Childrenlabel>()
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithChildAsset()
        {
            return new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-CHILD",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                    new Asset { Name = "ICON_RFID", Value = "RFID-CHILD" }
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
                                Sssets = new[] { "ICON_RFID" },
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
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-BLUE",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                    new Asset { Name = "ICON_RFID", Value = "https://static.inditex.com/rfid_alarm.png?ts=1" }
                },
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "MODEL_QUALITY",
                        Name = "PRICE_BLUE_VALUE",
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
                                Components = new[] { "PRICE_BLUE_VALUE" },
                                Sssets = new[] { "ICON_RFID" },
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
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-RED",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                                Components = new[] { "PRICE_RED_VALUE" },
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
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-DELIM",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                        Name = "PRODUCT_COLOR",
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
                        Components = new[] { "PRODUCT_COLOR" },
                        Assets = Array.Empty<string>()
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithQuotedComponentValue()
        {
            return new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    PONumber = "PO-QUOTES",
                    Campaign = "C1",
                    Brand_Text = "Z",
                    Section_Text = "SEC",
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
                        Name = "PRODUCT_COLOR",
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
                        Components = new[] { "PRODUCT_COLOR" },
                        Assets = Array.Empty<string>()
                    }
                }
            };
        }
    }
}
