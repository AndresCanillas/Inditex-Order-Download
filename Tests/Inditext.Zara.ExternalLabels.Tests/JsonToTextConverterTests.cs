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

            var output = JsonToTextConverter.LoadData(orderData);
            var lines = SplitLines(output);

            Assert.True(lines.Length > 1, "Se esperaba un header y al menos una fila de datos.");

            var header = SplitCsvLine(lines[0]);
            Assert.Contains("ProductionOrderNumber", header);
            Assert.Contains("LabelReference", header);
            Assert.Contains("Component:QR_product", header);
            Assert.Contains("Asset:Icono RFID", header);

            var expectedLabels = CountLabels(orderData.labels);
            var expectedSizes = orderData.POInformation.colors.Sum(c => c.sizes.Length);
            var expectedRows = expectedLabels * expectedSizes;

            Assert.Equal(expectedRows + 1, lines.Length);
        }

        [Fact]
        public void LoadData_ResuelveComponentesYAssetsPorItem()
        {
            var orderData = LoadSampleOrder();
            var output = JsonToTextConverter.LoadData(orderData);
            var lines = SplitLines(output);
            var header = SplitCsvLine(lines[0]);

            var rows = lines.Skip(1)
                .Select(line => SplitCsvLine(line))
                .ToList();

            var row = FindRow(rows, header, labelReference: "HPZKALL003", size: "18", color: "711");

            var qrIndex = Array.IndexOf(header, "Component:QR_product");
            var colorIndex = Array.IndexOf(header, "Component:Colour");
            var assetIndex = Array.IndexOf(header, "Asset:Icono RFID");

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
    }
}
