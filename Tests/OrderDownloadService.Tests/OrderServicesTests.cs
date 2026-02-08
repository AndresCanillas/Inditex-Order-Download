using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using OrderDonwLoadService.Model;
using OrderDonwLoadService.Synchronization;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class OrderServicesTests
    {
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
            }
            finally
            {
                if (File.Exists(credentialsPath))
                    File.Delete(credentialsPath);
            }
        }

        private static string GetOrderServiceAssemblyDir()
        {
            var location = typeof(OrderServices).Assembly.Location;
            return Path.GetDirectoryName(location);
        }

        private static void WriteCredentialsFile(string path)
        {
            var json = @"{
  ""Credentials"": [
    {
      ""Name"": ""Test"",
      ""User"": ""User"",
      ""Password"": ""Pass"",
      ""VendorId"": ""Vendor""
    }
  ]
}";
            File.WriteAllText(path, json);
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
        }
    }
}
