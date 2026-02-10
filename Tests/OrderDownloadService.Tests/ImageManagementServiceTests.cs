using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
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

        [Fact]
        public async Task ProcessOrderImagesAsync_WhenComponentValueContainsImageUrl_InsertsAndMarksPending()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrlAsync("https://example.com/from-component.png"))
                .ReturnsAsync((ImageAssetRecord)null);
            repository.Setup(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()))
                .ReturnsAsync(1);

            var downloader = new Mock<IImageDownloader>();
            downloader.Setup(d => d.DownloadAsync("https://example.com/from-component.png"))
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
            var order = BuildOrderWithComponentValue("https://example.com/from-component.png");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.True(result.RequiresApproval);
            downloader.Verify(d => d.DownloadAsync("https://example.com/from-component.png"), Times.Once);
            repository.Verify(repo => repo.InsertAsync(It.Is<ImageAssetRecord>(record => record.Url == "https://example.com/from-component.png")), Times.Once);
        }

        [Fact]
        public async Task ProcessOrderImagesAsync_WhenComponentValueContainsNonImageUrl_IgnoresIt()
        {
            var repository = new Mock<IImageAssetRepository>();
            var downloader = new Mock<IImageDownloader>();
            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();

            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);
            var order = BuildOrderWithComponentValue("https://example.com/api/orders");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.False(result.RequiresApproval);
            downloader.Verify(d => d.DownloadAsync(It.IsAny<string>()), Times.Never);
            repository.Verify(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOrderImagesAsync_WhenComponentValueMapContainsImageUrl_InsertsAndMarksPending()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrlAsync("https://example.com/nested/logo.jpeg"))
                .ReturnsAsync((ImageAssetRecord)null);
            repository.Setup(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()))
                .ReturnsAsync(1);

            var downloader = new Mock<IImageDownloader>();
            downloader.Setup(d => d.DownloadAsync("https://example.com/nested/logo.jpeg"))
                .ReturnsAsync(new DownloadedImage
                {
                    Content = Encoding.UTF8.GetBytes("img"),
                    ContentType = "image/jpeg"
                });

            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.DesignEmails", ""))
                .Returns("design@example.com");
            config.Setup(c => c.GetValue("DownloadServicesWeb.ImageManagement.EmailSubject", "Nuevas imágenes pendientes de validar"))
                .Returns("subject");
            var log = new Mock<IAppLog>();

            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);
            var order = BuildOrderWithNestedComponentValueMap("https://example.com/nested/logo.jpeg");

            var result = await service.ProcessOrderImagesAsync(order);

            Assert.True(result.RequiresApproval);
            downloader.Verify(d => d.DownloadAsync("https://example.com/nested/logo.jpeg"), Times.Once);
            repository.Verify(repo => repo.InsertAsync(It.IsAny<ImageAssetRecord>()), Times.Once);
        }

        [Fact]
        public void AreOrderImagesReady_WhenNoAssets_ReturnsTrue()
        {
            var repository = new Mock<IImageAssetRepository>();
            var downloader = new Mock<IImageDownloader>();
            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();
            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);

            var order = new InditexOrderData { Assets = new Asset[0] };
            var path = WriteTempOrder(order);

            try
            {
                var result = service.AreOrderImagesReady(path);
                Assert.True(result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AreOrderImagesReady_WhenAllInFont_ReturnsTrue()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrl("https://example.com/asset.png"))
                .Returns(new ImageAssetRecord { Status = ImageAssetStatus.InFont });
            var downloader = new Mock<IImageDownloader>();
            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();
            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);

            var order = BuildOrder("https://example.com/asset.png");
            var path = WriteTempOrder(order);

            try
            {
                var result = service.AreOrderImagesReady(path);
                Assert.True(result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AreOrderImagesReady_WhenPending_ReturnsFalse()
        {
            var repository = new Mock<IImageAssetRepository>();
            repository.Setup(repo => repo.GetLatestByUrl("https://example.com/asset.png"))
                .Returns(new ImageAssetRecord { Status = ImageAssetStatus.Nuevo });
            var downloader = new Mock<IImageDownloader>();
            var mailService = new Mock<IMailService>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();
            var service = new ImageManagementService(repository.Object, downloader.Object, mailService.Object, config.Object, log.Object);

            var order = BuildOrder("https://example.com/asset.png");
            var path = WriteTempOrder(order);

            try
            {
                var result = service.AreOrderImagesReady(path);
                Assert.False(result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static InditexOrderData BuildOrder(string url)
        {
            return new InditexOrderData
            {
                Assets = new[]
                {
                    new Asset
                    {
                        Name = "Icono RFID",
                        Type = "url",
                        Value = url
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithComponentValue(string value)
        {
            return new InditexOrderData
            {
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "images",
                        Name = "component-image",
                        Type = "string",
                        ValueMap = value
                    }
                }
            };
        }

        private static InditexOrderData BuildOrderWithNestedComponentValueMap(string url)
        {
            return new InditexOrderData
            {
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "images",
                        Name = "component-image",
                        Type = "string",
                        ValueMap = new
                        {
                            es = new
                            {
                                mobile = url
                            }
                        }
                    }
                }
            };
        }

        private static string WriteTempOrder(InditexOrderData order)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            var json = JsonConvert.SerializeObject(order);
            File.WriteAllText(path, json);
            return path;
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
