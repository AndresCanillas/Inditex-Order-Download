using System.Threading.Tasks;
using Moq;
using OrderDonwLoadService.Synchronization;
using OrderDownloadWebApi.Controllers;
using Service.Contracts;
using Xunit;

namespace OrderDownloadWebApi.Tests
{
    public class OrdersControllerTests
    {
        [Fact]
        public async Task MergePdf_WhenServiceReturnsError_ReturnsFailure()
        {
            var localization = new TestLocalizationService();
            var log = new Mock<IAppLog>();
            var orderServices = new Mock<IOrderServices>();
            orderServices.Setup(service => service.GetOrder("123", "C1", "V1"))
                .ReturnsAsync("Error Something went wrong");

            var controller = new OrdersController(localization, log.Object, orderServices.Object);

            var result = await controller.MergePdf(new GetOderDto
            {
                OrderNumber = "123",
                CampaignCode = "C1",
                VendorId = "V1"
            });

            Assert.False(result.Success);
            Assert.Equal("Error Something went wrong", result.Message);
        }

        [Fact]
        public async Task MergePdf_WhenServiceReturnsSuccess_ReturnsSuccess()
        {
            var localization = new TestLocalizationService();
            var log = new Mock<IAppLog>();
            var orderServices = new Mock<IOrderServices>();
            orderServices.Setup(service => service.GetOrder("123", "C1", "V1"))
                .ReturnsAsync("Order found");

            var controller = new OrdersController(localization, log.Object, orderServices.Object);

            var result = await controller.MergePdf(new GetOderDto
            {
                OrderNumber = "123",
                CampaignCode = "C1",
                VendorId = "V1"
            });

            Assert.True(result.Success);
            Assert.Equal("Order found", result.Message);
        }

        private sealed class TestLocalizationService : ILocalizationService
        {
            public string this[string key] => key;

            public string this[string key, params object[] args] => string.Format(key, args);
        }
    }
}
