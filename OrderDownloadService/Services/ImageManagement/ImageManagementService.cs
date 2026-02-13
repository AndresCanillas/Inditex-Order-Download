using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.OrderImages;
using StructureInditexOrderFile;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class ImageManagementService : IImageManagementService
    {
        private const string QrProductComponentName = "PRODUCT_QR";

        private readonly IImageAssetRepository repository;
        private readonly IImageDownloader downloader;
        private readonly IMailService mailService;
        private readonly IAppConfig config;
        private readonly IAppLog log;
        private readonly IPrintCentralService printCentralService;
        private readonly IQrProductSyncService qrProductSyncService;

        public ImageManagementService(
            IImageAssetRepository repository,
            IImageDownloader downloader,
            IMailService mailService,
            IAppConfig config,
            IAppLog log,
            IQrProductSyncService qrProductSyncService
            ): this(repository, downloader, mailService, config, log, qrProductSyncService, null)
        {
        }

        public ImageManagementService(
            IImageAssetRepository repository,
            IImageDownloader downloader,
            IMailService mailService,
            IAppConfig config,
            IAppLog log,
            IQrProductSyncService qrProductSyncService,
            IPrintCentralService printCentralService)
        {
            this.repository = repository;
            this.downloader = downloader;
            this.mailService = mailService;
            this.config = config;
            this.log = log;
            this.qrProductSyncService = qrProductSyncService;
            this.printCentralService = printCentralService;
        }

        public async Task<ImageProcessingResult> ProcessOrderImagesAsync(InditexOrderData order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (qrProductSyncService != null)
                await qrProductSyncService.SyncAsync(order);

            var result = new ImageProcessingResult();
            var assets = ExtractUrlAssets(order).ToList();

            foreach (var asset in assets)
            {
                var downloaded = await downloader.DownloadAsync(asset.Value);
                var hash = ComputeHash(downloaded.Content);
                var latest = await repository.GetLatestByUrlAsync(asset.Value);

                if (latest == null)
                {
                    await repository.InsertAsync(BuildRecord(asset, downloaded, hash, ImageAssetStatus.New, true));
                    MarkPending(result, asset.Value);
                    continue;
                }

                if (string.Equals(latest.Hash, hash, StringComparison.OrdinalIgnoreCase))
                    continue;

                await repository.MarkObsoleteAsync(latest.ID);
                await repository.InsertAsync(BuildRecord(asset, downloaded, hash, ImageAssetStatus.Updated, true));
                MarkPending(result, asset.Value);
            }

            if (result.RequiresApproval)
                NotifyDesignTeam(result);

            return result;
        }

        public async Task<bool> AreOrderImagesReadyAsync(string orderFilePath)
        {
            return await Task.FromResult(AreOrderImagesReady(orderFilePath));
        }

        public bool AreOrderImagesReady(string orderFilePath)
        {
            if (string.IsNullOrWhiteSpace(orderFilePath))
                throw new ArgumentException("Order file path cannot be null or empty.", nameof(orderFilePath));
            if (!File.Exists(orderFilePath))
                throw new FileNotFoundException("Order file not found.", orderFilePath);

            var order = JsonConvert.DeserializeObject<InditexOrderData>(File.ReadAllText(orderFilePath));
            var assets = ExtractUrlAssets(order).ToList();
            if (assets.Count == 0)
                return true;

            foreach (var asset in assets)
            {
                var latest = repository.GetLatestByUrl(asset.Value);
                if (latest == null || latest.Status != ImageAssetStatus.InFont)
                    return false;
            }

            return true;
        }


        private Tuple<string, string> ResolvePrintCredentials()
        {
            var user = config.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null);
            var password = config.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null);

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                log.LogMessage("ImageManagement: PrintCentral credentials are missing for PRODUCT_QR synchronization.");
                return null;
            }

            return Tuple.Create(user, password);
        }

        

        private IEnumerable<Asset> ExtractQrProductAssets(InditexOrderData order)
        {
            if (order?.ComponentValues == null)
                return Enumerable.Empty<Asset>();

            var assets = new List<Asset>();
            foreach (var component in order.ComponentValues)
            {
                if (component == null || !string.Equals(component.Name, QrProductComponentName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var url in ExtractImageUrlsFromValueMap(component.ValueMap))
                {
                    assets.Add(new Asset
                    {
                        Name = component.Name,
                        Type = "url",
                        Value = url
                    });
                }
            }

            return assets
                .GroupBy(asset => asset.Value, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
        }

        private IEnumerable<Asset> ExtractUrlAssets(InditexOrderData order)
        {
            if (order == null)
                return Enumerable.Empty<Asset>();

            var assets = new List<Asset>();

            if (order.Assets != null)
            {
                assets.AddRange(order.Assets.Where(asset =>
                    asset != null
                    && string.Equals(asset.Type, "url", StringComparison.OrdinalIgnoreCase)
                    && IsImageUrl(asset.Value)));
            }

            if (order.ComponentValues != null)
            {
                foreach (var component in order.ComponentValues)
                {
                    if (component == null || string.Equals(component.Name, QrProductComponentName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var url in ExtractImageUrlsFromValueMap(component.ValueMap))
                    {
                        assets.Add(new Asset
                        {
                            Name = component.Name,
                            Type = "url",
                            Value = url
                        });
                    }
                }
            }

            return assets
                .GroupBy(asset => asset.Value, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
        }

        private static IEnumerable<string> ExtractImageUrlsFromValueMap(object valueMap)
        {
            if (valueMap == null)
                return Enumerable.Empty<string>();

            var token = valueMap as JToken ?? JToken.FromObject(valueMap);
            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            TraverseToken(token, urls);

            return urls;
        }

        private static void TraverseToken(JToken token, ISet<string> urls)
        {
            if (token == null)
                return;

            if (token.Type == JTokenType.String)
            {
                var value = token.Value<string>();
                if (IsImageUrl(value))
                    urls.Add(value.Trim());
                return;
            }

            foreach (var child in token.Children())
                TraverseToken(child, urls);
        }

        private static string ExtractBarcodeFromQrUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var fileName = Path.GetFileName(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
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

        private static bool IsImageUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var path = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var extension = Path.GetExtension(path);
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

        private static string ComputeHash(byte[] content)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(content);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static ImageAssetRecord BuildRecord(
            Asset asset,
            DownloadedImage downloaded,
            string hash,
            ImageAssetStatus status,
            bool isLatest)
        {
            var now = DateTime.UtcNow;
            return new ImageAssetRecord
            {
                Name = asset.Name,
                Url = asset.Value,
                Hash = hash,
                ContentType = downloaded.ContentType,
                Content = downloaded.Content,
                Status = status,
                IsLatest = isLatest,
                CreatedDate = now,
                UpdatedDate = now
            };
        }

        private void MarkPending(ImageProcessingResult result, string url)
        {
            result.RequiresApproval = true;
            result.NewOrUpdatedUrls.Add(url);
        }

        private void NotifyDesignTeam(ImageProcessingResult result)
        {
            var recipients = config.GetValue<string>("DownloadServicesWeb.ImageManagement.DesignEmails", "");
            if (string.IsNullOrWhiteSpace(recipients))
            {
                log.LogMessage("ImageManagement: no design recipients configured.");
                return;
            }

            var subject = config.GetValue("DownloadServicesWeb.ImageManagement.EmailSubject",
                "Nuevas imágenes pendientes de validar");
            var body = BuildEmailBody(result.NewOrUpdatedUrls);
            try
            {
                mailService.Enqueue(recipients, subject, body);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
            }
        }

        private static string BuildEmailBody(IEnumerable<string> urls)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Se detectaron imágenes nuevas o actualizadas pendientes de validar en fuente:");
            sb.AppendLine();
            foreach (var url in urls.Distinct(StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"- {url}");
            sb.AppendLine();
            sb.AppendLine("Por favor validar y marcar como InFont en PrintCentral.");
            return sb.ToString();
        }
    }
}
