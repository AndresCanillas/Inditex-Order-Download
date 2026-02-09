using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.OrderImages;
using StructureInditexOrderFile;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IImageAssetRepository repository;
        private readonly IImageDownloader downloader;
        private readonly IMailService mailService;
        private readonly IAppConfig config;
        private readonly IAppLog log;

        public ImageManagementService(
            IImageAssetRepository repository,
            IImageDownloader downloader,
            IMailService mailService,
            IAppConfig config,
            IAppLog log)
        {
            this.repository = repository;
            this.downloader = downloader;
            this.mailService = mailService;
            this.config = config;
            this.log = log;
        }

        public async Task<ImageProcessingResult> ProcessOrderImagesAsync(InditexOrderData order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var result = new ImageProcessingResult();
            var assets = ExtractUrlAssets(order).ToList();

            foreach (var asset in assets)
            {
                var downloaded = await downloader.DownloadAsync(asset.Value);
                var hash = ComputeHash(downloaded.Content);
                var latest = await repository.GetLatestByUrlAsync(asset.Value);

                if (latest == null)
                {
                    await repository.InsertAsync(BuildRecord(asset, downloaded, hash, ImageAssetStatus.Nuevo, true));
                    MarkPending(result, asset.Value);
                    continue;
                }

                if (string.Equals(latest.Hash, hash, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await repository.MarkObsoleteAsync(latest.ID);
                await repository.InsertAsync(BuildRecord(asset, downloaded, hash, ImageAssetStatus.Actualizado, true));
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

        private IEnumerable<Asset> ExtractUrlAssets(InditexOrderData order)
        {
            return order.Assets?.Where(asset => string.Equals(asset.Type, "url", StringComparison.OrdinalIgnoreCase))
                ?? Enumerable.Empty<Asset>();
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
            sb.AppendLine("<p>Se detectaron nuevas imágenes o imágenes actualizadas:</p>");
            sb.AppendLine("<ul>");
            foreach (var url in urls.Distinct())
                sb.AppendLine($"<li>{url}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Por favor, validar e incorporar en la fuente.</p>");
            return sb.ToString();
        }
    }
}
