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
        public async Task GetOrder_WhenServiceReturnsError_ReturnsFailure()
        {
            var localization = new TestLocalizationService();
            var log = new Mock<IAppLog>();
            var orderServices = new Mock<IOrderServices>();
            orderServices.Setup(service => service.GetOrder("123", "C1", "V1"))
                .ReturnsAsync("Error Something went wrong");

            var controller = new OrdersController(localization, log.Object, orderServices.Object);

            var result = await controller.GetOrder(new GetOderDto
            {
                OrderNumber = "123",
                CampaignCode = "C1",
                VendorId = "V1"
            });

            Assert.False(result.Success);
            Assert.Equal("Error Something went wrong", result.Message);
        }

        [Fact]
        public async Task GetOrder_WhenServiceReturnsSuccess_ReturnsSuccess()
        {
            var localization = new TestLocalizationService();
            var log = new Mock<IAppLog>();
            var orderServices = new Mock<IOrderServices>();
            orderServices.Setup(service => service.GetOrder("123", "C1", "V1"))
                .ReturnsAsync("Order found");

            var controller = new OrdersController(localization, log.Object, orderServices.Object);

            var result = await controller.GetOrder(new GetOderDto
            {
                OrderNumber = "123",
                CampaignCode = "C1",
                VendorId = "V1"
            });

            Assert.True(result.Success);
            Assert.Equal("Order found", result.Message);
        }

        [Fact]
        public async Task GetOrder_WhenServiceReturnsDynamicQueueMessage_LocalizesMessage()
        {
            var localization = new TestLocalizationService();
            var log = new Mock<IAppLog>();
            var orderServices = new Mock<IOrderServices>();
            orderServices.Setup(service => service.GetOrder("123", "C1", "V1"))
                .ReturnsAsync("Order number (123) found successfully in ZARA queue.");

            var controller = new OrdersController(localization, log.Object, orderServices.Object);

            var result = await controller.GetOrder(new GetOderDto
            {
                OrderNumber = "123",
                CampaignCode = "C1",
                VendorId = "V1"
            });

            Assert.True(result.Success);
            Assert.Equal("Pedido número (123) encontrado correctamente en la cola ZARA.", result.Message);
        }

        private sealed class TestLocalizationService : ILocalizationService
        {
            private const string QueueFoundTemplate = "Order number ({0}) found successfully in {1} queue.";

            public string this[string key] => key;

            public string this[string key, params object[] args]
            {
                if (key == QueueFoundTemplate)
                {
                    return string.Format("Pedido número ({0}) encontrado correctamente en la cola {1}.", args);
                }

                return string.Format(key, args);
            }
        }
    }
}
