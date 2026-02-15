using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using OrderDonwLoadService.Model;
using OrderDonwLoadService.Synchronization;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using Xunit;
using OrderDonwLoadService.Services;

namespace OrderDownloadService.Tests
{
    public class OrderServicesTests
    {
        [Fact]
        public async Task FetchOrderAsync_SendsLabelRequestInBody()
        {
            var appConfig = new Mock<IAppConfig>();
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ApiUrl", It.IsAny<string>()))
                .Returns("https://api.example.com/");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.WorkDirectory", It.IsAny<string>()))
                .Returns(Path.Combine(Path.GetTempPath(), "workdir"));
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.TokenApiUrl", It.IsAny<string>()))
                .Returns("https://auth.inditex.com:443/");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ControllerToken", It.IsAny<string>()))
                .Returns("openam/oauth2/itxid/itxidmp/b2b/access_token");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.TokenScope", It.IsAny<string>()))
                .Returns("inditex");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ControllerLabels", It.IsAny<string>()))
                .Returns("api/v3/label-printing/supplier-data/search");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.MaxTrys", 2)).Returns(2);
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.SecondsToWait", 240d)).Returns(1d);

            var log = new Mock<IAppLog>();
            var apiCaller = new Mock<IApiCallerService>();
            var events = new Mock<IEventQueue>();
            var imageManagementService = new Mock<IImageManagementService>();

            apiCaller.Setup(a => a.GetToken(
                    "https://auth.inditex.com/openam/oauth2/itxid/itxidmp/b2b/access_token",
                    "User",
                    "Pass",
                    "inditex"))
                .ReturnsAsync(new AuthenticationResult { id_token = "token" });

            apiCaller.Setup(a => a.GetLabelOrders(
                    "api/v3/label-printing/supplier-data/search",
                    "12345",
                    "token",
                    It.Is<LabelOrderRequest>(rq =>
                        rq.ProductionOrderNumber == "30049" &&
                        rq.Campaign == "I25" &&
                        rq.SupplierCode == "12345")))
                .ReturnsAsync((StructureInditexOrderFile.InditexOrderData)null)
                .Verifiable();

            var service = new TestOrderServices(appConfig.Object, log.Object, apiCaller.Object, events.Object, imageManagementService.Object);

            var credential = new Credential { User = "User", Password = "Pass" };
            await service.ExecuteFetchOrderAsync(credential, "30049", "I25", "12345");

            apiCaller.Verify();
        }

        [Fact]
        public async Task GetOrder_WhenOrderIsNull_ReturnsNotFoundMessage()
        {
            var appConfig = new Mock<IAppConfig>();
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ApiUrl", It.IsAny<string>()))
                .Returns("https://api.example.com/");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.WorkDirectory", It.IsAny<string>()))
                .Returns(Path.Combine(Path.GetTempPath(), "workdir"));
            appConfig.Setup(cfg => cfg.GetValue<string>("DownloadServices.HistoryDirectory"))
                .Returns(Path.Combine(Path.GetTempPath(), "history"));

            var log = new Mock<IAppLog>();
            var apiCaller = new Mock<IApiCallerService>();
            var events = new Mock<IEventQueue>();
            var imageManagementService = new Mock<IImageManagementService>();

            var credentialsPath = Path.Combine(GetOrderServiceAssemblyDir(), "InditexCredentials.json");
            WriteCredentialsFile(credentialsPath);

            try
            {
                var service = new TestOrderServices(appConfig.Object, log.Object, apiCaller.Object, events.Object, imageManagementService.Object);

                var result = await service.GetOrder("123", "C1", "V1");

                Assert.Equal("Order number (123) not found in any queue.", result);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "123" &&
                    x.StepId == "search-order" &&
                    x.Status == "in-progress")), Times.Once);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "123" &&
                    x.StepId == "search-order" &&
                    x.Status == "failed")), Times.Once);
            }
            finally
            {
                if (File.Exists(credentialsPath))
                    File.Delete(credentialsPath);
            }
        }


        [Fact]
        public async Task GetOrder_WhenOrderIsFetched_PublishesDownloadOrderCompletedProgressEvent()
        {
            var appConfig = new Mock<IAppConfig>();
            var workdir = Path.Combine(Path.GetTempPath(), "workdir_" + Guid.NewGuid().ToString("N"));
            var historydir = Path.Combine(Path.GetTempPath(), "history_" + Guid.NewGuid().ToString("N"));

            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ApiUrl", It.IsAny<string>()))
                .Returns("https://api.example.com/");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.WorkDirectory", It.IsAny<string>()))
                .Returns(workdir);
            appConfig.Setup(cfg => cfg.GetValue<string>("DownloadServices.HistoryDirectory"))
                .Returns(historydir);

            var log = new Mock<IAppLog>();
            var apiCaller = new Mock<IApiCallerService>();
            var events = new Mock<IEventQueue>();
            var imageManagementService = new Mock<IImageManagementService>();
            imageManagementService
                .Setup(x => x.ProcessOrderImagesAsync(It.IsAny<StructureInditexOrderFile.InditexOrderData>()))
                .ReturnsAsync(new ImageProcessingResult { RequiresApproval = false });

            var credentialsPath = Path.Combine(GetOrderServiceAssemblyDir(), "InditexCredentials.json");
            WriteCredentialsFile(credentialsPath);

            try
            {
                var order = BuildValidOrder("30049");
                var service = new TestOrderServicesWithFetchedOrder(appConfig.Object, log.Object, apiCaller.Object, events.Object, imageManagementService.Object, order);

                var result = await service.GetOrder("30049", "I25", "12345");

                Assert.Contains("found successfully", result);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "30049" &&
                    x.StepId == "download-order" &&
                    x.Status == "completed" &&
                    x.Message.Contains("downloaded", StringComparison.OrdinalIgnoreCase))), Times.AtLeastOnce);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "30049" &&
                    x.StepId == "send-file-print-central" &&
                    x.Status == "completed")), Times.AtLeastOnce);
                events.Verify(e => e.Send(It.IsAny<FileReceivedEvent>()), Times.Once);
            }
            finally
            {
                if (File.Exists(credentialsPath))
                    File.Delete(credentialsPath);
                if (Directory.Exists(workdir))
                    Directory.Delete(workdir, true);
                if (Directory.Exists(historydir))
                    Directory.Delete(historydir, true);
            }
        }

        [Fact]
        public async Task GetOrder_WhenImagesRequireApproval_PublishesPendingValidationAndDoesNotSendFileEvent()
        {
            var appConfig = new Mock<IAppConfig>();
            var workdir = Path.Combine(Path.GetTempPath(), "workdir_" + Guid.NewGuid().ToString("N"));
            var historydir = Path.Combine(Path.GetTempPath(), "history_" + Guid.NewGuid().ToString("N"));

            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.ApiUrl", It.IsAny<string>()))
                .Returns("https://api.example.com/");
            appConfig.Setup(cfg => cfg.GetValue("DownloadServices.WorkDirectory", It.IsAny<string>()))
                .Returns(workdir);
            appConfig.Setup(cfg => cfg.GetValue<string>("DownloadServices.HistoryDirectory"))
                .Returns(historydir);

            var log = new Mock<IAppLog>();
            var apiCaller = new Mock<IApiCallerService>();
            var events = new Mock<IEventQueue>();
            var imageManagementService = new Mock<IImageManagementService>();
            imageManagementService
                .Setup(x => x.ProcessOrderImagesAsync(It.IsAny<StructureInditexOrderFile.InditexOrderData>()))
                .ReturnsAsync(new ImageProcessingResult { RequiresApproval = true });

            var credentialsPath = Path.Combine(GetOrderServiceAssemblyDir(), "InditexCredentials.json");
            WriteCredentialsFile(credentialsPath, "Indetgroup_Tempe_V6");

            try
            {
                var order = BuildValidOrder("30049");
                var service = new TestOrderServicesWithFetchedOrder(appConfig.Object, log.Object, apiCaller.Object, events.Object, imageManagementService.Object, order);

                var result = await service.GetOrder("30049", "I25", "12345");

                Assert.Contains("found successfully", result);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "30049" &&
                    x.StepId == "send-file-print-central" &&
                    x.Status == "pending-validation" &&
                    x.Message.Contains("Indetgroup_Tempe_V6", StringComparison.Ordinal))), Times.Once);
                events.Verify(e => e.Send(It.Is<OrderGetProgressEvent>(x =>
                    x.OrderNumber == "30049" &&
                    x.StepId == "send-file-print-central" &&
                    x.Status == "completed")), Times.Never);
                events.Verify(e => e.Send(It.IsAny<FileReceivedEvent>()), Times.Never);
            }
            finally
            {
                if (File.Exists(credentialsPath))
                    File.Delete(credentialsPath);
                if (Directory.Exists(workdir))
                    Directory.Delete(workdir, true);
                if (Directory.Exists(historydir))
                    Directory.Delete(historydir, true);
            }
        }

        private static string GetOrderServiceAssemblyDir()
        {
            var location = typeof(OrderServices).Assembly.Location;
            return Path.GetDirectoryName(location);
        }


        private static StructureInditexOrderFile.InditexOrderData BuildValidOrder(string poNumber)
        {
            return new StructureInditexOrderFile.InditexOrderData
            {
                ProductionOrder = new StructureInditexOrderFile.ProductionOrder
                {
                    PONumber = poNumber,
                    Campaign = "I25",
                    Section_Text = "SEC",
                    Brand_Text = "BR",
                    ProductType_Text = "PT",
                    QualityRfid = 1,
                    ModelRfid = 1
                },
                labels = new[]
                {
                    new StructureInditexOrderFile.Label { Reference = "ABC123" }
                }
            };
        }

        private static void WriteCredentialsFile(string path, string credentialName = "Test")
        {
            var json = $@"{{
  ""Credentials"": [
    {{
      ""Name"": ""{credentialName}"",
      ""User"": ""User"",
      ""Password"": ""Pass"",
      ""VendorId"": ""Vendor""
    }}
  ]
}}";
            File.WriteAllText(path, json);
        }


        private sealed class TestOrderServicesWithFetchedOrder : OrderServices
        {
            private readonly StructureInditexOrderFile.InditexOrderData order;

            public TestOrderServicesWithFetchedOrder(
                IAppConfig appConfig,
                IAppLog log,
                IApiCallerService apiCaller,
                IEventQueue events,
                IImageManagementService imageManagementService,
                StructureInditexOrderFile.InditexOrderData order)
                : base(appConfig, log, apiCaller, events, imageManagementService)
            {
                this.order = order;
            }

            protected override Task<StructureInditexOrderFile.InditexOrderData> FetchOrderAsync(
                Credential credential,
                string orderNumber,
                string campaignCode,
                string vendorId)
            {
                return Task.FromResult(order);
            }
        }

        private sealed class TestOrderServices : OrderServices
        {
            public TestOrderServices(
                IAppConfig appConfig,
                IAppLog log,
                IApiCallerService apiCaller,
                IEventQueue events,
                IImageManagementService imageManagementService)
                : base(appConfig, log, apiCaller, events, imageManagementService)
            {
            }

            protected override Task<StructureInditexOrderFile.InditexOrderData> FetchOrderAsync(
                Credential credential,
                string orderNumber,
                string campaignCode,
                string vendorId)
            {
                return Task.FromResult<StructureInditexOrderFile.InditexOrderData>(null);
            }

            public Task<StructureInditexOrderFile.InditexOrderData> ExecuteFetchOrderAsync(
                Credential credential,
                string orderNumber,
                string campaignCode,
                string vendorId)
            {
                return base.FetchOrderAsync(credential, orderNumber, campaignCode, vendorId);
            }
        }
    }
}
