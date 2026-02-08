using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Moq;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using Service.Contracts.OrderImages;
using StructureInditexOrderFile;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class ImageManagementServiceTests
    {
        [Fact]
        public async Task ProcessOrderImagesAsync_WhenNewImage_InsertsAndMarksPending()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrlAsync("https://example.com/asset.png"))
                .ReturnsAsync((ImageAssetRecord)null);
            repository.Setup(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()))
                .ReturnsAsync(1);

            var downloader = new Mock<IImageDownloader>();
            downloader.Setup(d => d.DownloadAsync("https://example.com/asset.png"))
                .ReturnsAsync(new DownloadedImage
                {
                    Content = Encoding.UTF8.GetBytes("img"),
                    ContentType = "image/png"
                });

            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.DesignEmails", ""))
                .Returns("design@example.com");
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.EmailSubject", "Nuevas imágenes pendientes de validar"))
                .Returns("subject");
            var log = new Mock<IAppLog>();

            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);
            var order = BuildOrder("https://example.com/asset.png");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.True(result.RequiresApproval);
            repository.Verify(repo => repo.InsertAsync(It.Is<ImageAssetRecord>(record => record.Status == ImageAssetStatus.Nuevo)), Times.Once);
            mailService.Verify(ms => ms.Enqueue("design@example.com", "subject", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOrderImagesAsync_WhenHashMatches_DoesNotInsert()
        {
            var repository = new Mock<IImageAssetRepository>();
            var hash = ComputeHash("img");
            repository.Setup(repo => repo.GetLatestByUrlAsync("https://example.com/asset.png"))
                .ReturnsAsync(new ImageAssetRecord
                {
                    ID = 10,
                    Url = "https://example.com/asset.png",
                    Hash = hash,
                    Status = ImageAssetStatus.InFont,
                    IsLatest = true
                });

            var downloader = new Mock<IImageDownloader>();
            downloader.Setup(d => d.DownloadAsync("https://example.com/asset.png"))
                .ReturnsAsync(new DownloadedImage
                {
                    Content = Encoding.UTF8.GetBytes("img"),
                    ContentType = "image/png"
                });

            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();

            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);
            var order = BuildOrder("https://example.com/asset.png");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.False(result.RequiresApproval);
            repository.Verify(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()), Times.Never);
            repository.Verify(repo => repo.MarkObsoleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOrderImagesAsync_WhenHashDiffers_MarksObsoleteAndInsertsUpdated()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrlAsync("https://example.com/asset.png"))
                .ReturnsAsync(new ImageAssetRecord
                {
                    ID = 10,
                    Url = "https://example.com/asset.png",
                    Hash = ComputeHash("old"),
                    Status = ImageAssetStatus.InFont,
                    IsLatest = true
                });
            repository.Setup(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()))
                .ReturnsAsync(11);

            var downloader = new Mock<IImageDownloader>();
            downloader.Setup(d => d.DownloadAsync("https://example.com/asset.png"))
                .ReturnsAsync(new DownloadedImage
                {
                    Content = Encoding.UTF8.GetBytes("new"),
                    ContentType = "image/png"
                });

            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.DesignEmails", ""))
                .Returns("design@example.com");
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.EmailSubject", "Nuevas imágenes pendientes de validar"))
                .Returns("subject");
            var log = new Mock<IAppLog>();

            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);
            var order = BuildOrder("https://example.com/asset.png");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.True(result.RequiresApproval);
            repository.Verify(repo => repo.MarkObsoleteAsync(10), Times.Once);
            repository.Verify(repo => repo.InsertAsync(It.Is<ImageAssetRecord>(record => record.Status == ImageAssetStatus.Actualizado)), Times.Once);
        }

        private static InditexOrderData BuildOrder(string url)
        {
            return new InditexOrderData
            {
                assets = new[]
                {
                    new Asset
                    {
                        name = "Icono RFID",
                        type = "url",
                        value = url
                    }
                }
            };
        }

        private static string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
