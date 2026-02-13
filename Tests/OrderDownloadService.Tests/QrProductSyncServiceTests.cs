using Moq;
using OrderDonwLoadService.Services;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using StructureInditexOrderFile;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class QrProductSyncServiceTests
    {
        [Fact]
        public async Task SyncAsync_WhenQrDoesNotExist_UploadsToPrintCentral()
        {
            var printCentral = new Mock<IPrintCentralService>();
            var downloader = new Mock<IImageDownloader>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();
            var repo = new Mock<IImageAssetRepository>();

            repo.Setup(r => r.ResolveProjectId("I25")).ReturnsAsync(123);
            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null)).Returns("user");
            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null)).Returns("pwd");

            printCentral.Setup(p => p.ProjectImageExistsAsync(123, "33419")).ReturnsAsync(false);
            downloader.Setup(d => d.DownloadAsync(It.Is<string>(u => u.Contains("_33419.svg"))))
                .ReturnsAsync(new DownloadedImage { Content = Encoding.UTF8.GetBytes("qr"), ContentType = "image/svg+xml" });

            var service = new QrProductSyncService(printCentral.Object, downloader.Object, config.Object, log.Object, repo.Object);
            var order = BuildOrderWithQrProduct("https://example.com/label-assets/qr_product_uuid_33419.svg?sig=123", "PRODUCT_QR", "I25");

            await service.SyncAsync(order);

            printCentral.Verify(p => p.LoginAsync("/", "user", "pwd"), Times.Once);
            printCentral.Verify(p => p.UploadProjectImageAsync(123, "33419", It.IsAny<byte[]>(), "33419.svg"), Times.Once);
            printCentral.Verify(p => p.LogoutAsync(), Times.Once);
        }


        [Fact]
        public async Task SyncAsync_WhenQrUsesProductQrName_UploadsToPrintCentral()
        {
            var printCentral = new Mock<IPrintCentralService>();
            var downloader = new Mock<IImageDownloader>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();
            var repo = new Mock<IImageAssetRepository>();

            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null)).Returns("user");
            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null)).Returns("pwd");
            repo.Setup(r => r.ResolveProjectId("V26")).ReturnsAsync(123);

            printCentral.Setup(p => p.ProjectImageExistsAsync(123, "33419")).ReturnsAsync(false);
            downloader.Setup(d => d.DownloadAsync(It.Is<string>(u => u.Contains("_33419.svg"))))
                .ReturnsAsync(new DownloadedImage { Content = Encoding.UTF8.GetBytes("qr"), ContentType = "image/svg+xml" });

            var service = new QrProductSyncService(printCentral.Object, downloader.Object, config.Object, log.Object, repo.Object);

            await service.SyncAsync(BuildOrderWithQrProduct("https://example.com/label-assets/qr_product_uuid_33419.svg?sig=123", "PRODUCT_QR", "V26"));

            printCentral.Verify(p => p.LoginAsync("/", "user", "pwd"), Times.Once);
            printCentral.Verify(p => p.UploadProjectImageAsync(123, "33419", It.IsAny<byte[]>(), "33419.svg"), Times.Once);
            printCentral.Verify(p => p.LogoutAsync(), Times.Once);
        }

        [Fact]
        public async Task SyncAsync_WhenQrAlreadyExists_DoesNotUpload()
        {
            var printCentral = new Mock<IPrintCentralService>();
            var downloader = new Mock<IImageDownloader>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();


            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null)).Returns("user");
            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null)).Returns("pwd");

            printCentral.Setup(p => p.ProjectImageExistsAsync(123, "33419")).ReturnsAsync(true);

            var repo = new Mock<IImageAssetRepository>();
            repo.Setup(r => r.ResolveProjectId("I25")).ReturnsAsync(123);

            var service = new QrProductSyncService(printCentral.Object, downloader.Object, config.Object, log.Object, repo.Object);
            var order = BuildOrderWithQrProduct("https://example.com/label-assets/qr_product_uuid_33419.svg?sig=123", "PRODUCT_QR", "I25");

            await service.SyncAsync(order);

            printCentral.Verify(p => p.UploadProjectImageAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
            downloader.Verify(d => d.DownloadAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SyncAsync_WhenCredentialsMissing_DoesNotLogin()
        {
            var printCentral = new Mock<IPrintCentralService>();
            var downloader = new Mock<IImageDownloader>();
            var config = new Mock<IAppConfig>();
            var log = new Mock<IAppLog>();


            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null)).Returns(string.Empty);
            config.Setup(c => c.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null)).Returns("pwd");

            var repo = new Mock<IImageAssetRepository>();
            repo.Setup(r => r.ResolveProjectId("I25")).ReturnsAsync(123);

            var service = new QrProductSyncService(printCentral.Object, downloader.Object, config.Object, log.Object, repo.Object);

            await service.SyncAsync(BuildOrderWithQrProduct("https://example.com/label-assets/qr_product_uuid_33419.svg?sig=123", "PRODUCT_QR", "I25"));

            printCentral.Verify(p => p.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static InditexOrderData BuildOrderWithQrProduct(string qrUrl, string componentName, string campaign)
        {
            return new InditexOrderData
            {
                ProductionOrder = new ProductionOrder
                {
                    Campaign = campaign
                },
                ComponentValues = new[]
                {
                    new Componentvalue
                    {
                        GroupKey = "COLOR_SIZE",
                        Name = componentName,
                        Type = "string",
                        ValueMap = new
                        {
                            A = qrUrl
                        }
                    }
                }
            };
        }
    }
}
