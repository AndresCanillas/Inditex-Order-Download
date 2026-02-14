using Newtonsoft.Json.Linq;
using OrderDonwLoadService.Services;
using Service.Contracts;
using Service.Contracts.Database;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class QrProductSyncService : IQrProductSyncService
    {
        private const string QrProductComponentName = "PRODUCT_QR";
        private const string ProductBarcodeComponentName = "PRODUCT_BARCODE";
        private const string UserConfig = "DownloadServices.PrintCentralCredentials.User";
        private const string PasswordConfig = "DownloadServices.PrintCentralCredentials.Password";

        private readonly IPrintCentralService printCentralService;
        private readonly IImageDownloader downloader;
        private readonly IAppConfig config;
        private readonly IAppLog log;
        private readonly IImageAssetRepository imageAssetRepository;

        public QrProductSyncService(
            IPrintCentralService printCentralService,
            IImageDownloader downloader,
            IAppConfig config,
            IAppLog log,
            IImageAssetRepository imageAssetRepository)
        {
            this.printCentralService = printCentralService;
            this.downloader = downloader;
            this.config = config;
            this.log = log;
            this.imageAssetRepository = imageAssetRepository;
        }

        public async Task SyncAsync(InditexOrderData order)
        {
            if (order == null)
                return;

            var projectId = await imageAssetRepository.ResolveProjectId(order.ProductionOrder?.Campaign);
            if (projectId == null)
                return;

            var credentials = ResolvePrintCredentials();
            if (credentials == null)
                return;

            var qrAssets = ExtractQrProductAssets(order).ToList();
            if (qrAssets.Count == 0)
                return;

            await printCentralService.LoginAsync("/", credentials.Item1, credentials.Item2);
            try
            {
                foreach (var qrAsset in qrAssets)
                {
                    var barcode = ResolveBarcode(qrAsset);
                    if (string.IsNullOrWhiteSpace(barcode))
                        continue;

                    if (await printCentralService.ProjectImageExistsAsync(projectId ?? 0, barcode))
                        continue;

                    var downloaded = await downloader.DownloadAsync(qrAsset.Url);
                    await printCentralService.UploadProjectImageAsync(projectId ?? 0, barcode, downloaded.Content, BuildQrFileName(barcode, qrAsset.Url));
                }
            }
            finally
            {
                await printCentralService.LogoutAsync();
            }
        }

        private Tuple<string, string> ResolvePrintCredentials()
        {
            var user = config.GetValue<string>(UserConfig, null);
            var password = config.GetValue<string>(PasswordConfig, null);
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                log.LogMessage("ImageManagement: PrintCentral credentials are missing for PRODUCT_QR synchronization.");
                return null;
            }

            return Tuple.Create(user, password);
        }

        private static string ResolveBarcode(QrProductAsset qrAsset)
        {
            if (!string.IsNullOrWhiteSpace(qrAsset.Barcode))
                return qrAsset.Barcode;

            return ExtractBarcodeFromQrUrl(qrAsset.Url);
        }

        private static IEnumerable<QrProductAsset> ExtractQrProductAssets(InditexOrderData order)
        {
            if (order.ComponentValues == null)
                return Enumerable.Empty<QrProductAsset>();

            var barcodesByKey = ExtractBarcodesByValueMapKey(order.ComponentValues);
            var assetsByUrl = new Dictionary<string, QrProductAsset>(StringComparer.OrdinalIgnoreCase);

            foreach (var component in order.ComponentValues)
            {
                if (component == null || !string.Equals(component.Name, QrProductComponentName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var urlEntry in ExtractImageUrlsFromValueMap(component.ValueMap))
                {
                    if (assetsByUrl.ContainsKey(urlEntry.Value))
                        continue;

                    barcodesByKey.TryGetValue(urlEntry.Path, out var barcode);
                    assetsByUrl.Add(urlEntry.Value, new QrProductAsset(urlEntry.Value, barcode));
                }
            }

            return assetsByUrl.Values;
        }

        private static IDictionary<string, string> ExtractBarcodesByValueMapKey(IEnumerable<Componentvalue> components)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var component in components)
            {
                if (component == null || !string.Equals(component.Name, ProductBarcodeComponentName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var entry in ExtractStringValuesFromValueMap(component.ValueMap))
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && !result.ContainsKey(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
                        result.Add(entry.Key, entry.Value.Trim());
                }
            }

            return result;
        }

        private static IEnumerable<ValueMapEntry> ExtractImageUrlsFromValueMap(object valueMap)
        {
            return ExtractStringValuesFromValueMap(valueMap)
                .Where(x => IsImageUrl(x.Value))
                .Select(x => new ValueMapEntry(x.Path, x.Value));
        }

        private static IEnumerable<ValueMapEntry> ExtractStringValuesFromValueMap(object valueMap)
        {
            if (valueMap == null)
                return Enumerable.Empty<ValueMapEntry>();

            var token = valueMap as JToken ?? JToken.FromObject(valueMap);
            var values = new List<ValueMapEntry>();
            TraverseToken(token, string.Empty, values);
            return values;
        }

        private static void TraverseToken(JToken token, string currentPath, ICollection<ValueMapEntry> values)
        {
            if (token == null)
                return;

            if (token.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)token).Properties())
                {
                    var childPath = string.IsNullOrWhiteSpace(currentPath) ? property.Name : $"{currentPath}.{property.Name}";
                    TraverseToken(property.Value, childPath, values);
                }

                return;
            }

            if (token.Type == JTokenType.Array)
            {
                var index = 0;
                foreach (var child in token.Children())
                {
                    TraverseToken(child, $"{currentPath}[{index}]", values);
                    index++;
                }

                return;
            }

            if (token.Type != JTokenType.String)
                return;

            var value = token.Value<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;

            values.Add(new ValueMapEntry(currentPath, value));
        }

        private static bool IsImageUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var extension = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            switch (extension.ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                case ".webp":
                case ".svg":
                case ".tif":
                case ".tiff":
                    return true;
                default:
                    return false;
            }
        }

        private static string ExtractBarcodeFromQrUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return null;

            var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore == fileNameWithoutExtension.Length - 1)
                return null;

            return fileNameWithoutExtension.Substring(lastUnderscore + 1);
        }

        private static string BuildQrFileName(string barcode, string url)
        {
            var extension = ".svg";
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var parsedExtension = Path.GetExtension(uri.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(parsedExtension))
                    extension = parsedExtension;
            }

            return $"{barcode}{extension}";
        }

        private class QrProductAsset
        {
            public string Url { get; }
            public string Barcode { get; }

            public QrProductAsset(string url, string barcode)
            {
                Url = url;
                Barcode = barcode;
            }
        }

        private class ValueMapEntry
        {
            public string Path { get; }
            public string Value { get; }

            public ValueMapEntry(string path, string value)
            {
                Path = path;
                Value = value;
            }
        }
    }
}
