using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using Service.Contracts;
using StructureInditexOrderFile;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class ApiCallerServiceTests
    {
        [Fact]
        public void Start_WhenUrlDoesNotEndWithSlash_NormalizesTrailingSlash()
        {
            var service = new ApiCallerService();

            service.Start("https://api.example.com/base");

            Assert.Equal("https://api.example.com/base/", service.Url);
        }

        [Fact]
        public async Task GetLabelOrders_WhenTokenIsEmpty_ReturnsDefault()
        {
            var service = new ApiCallerService();

            var result = await service.GetLabelOrders("api/orders", string.Empty, "12345", new LabelOrderRequest());

            Assert.Null(result);
        }


        [Fact]
        public async Task GetLabelOrders_WhenVendorIdIsEmpty_DoesNotSendVendorHeader()
        {
            var handler = new CaptureHandler(_ =>
            {
                var responseBody = JsonConvert.SerializeObject(new InditexOrderData());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            });

            BaseServiceClient.client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(1)
            };

            var service = new ApiCallerService();
            service.Start("https://api.product.inditex.com/pubsup/b2b");

            await service.GetLabelOrders("api/v3/label-printing/supplier-data/search", "ACCESS_TOKEN", string.Empty, new LabelOrderRequest
            {
                ProductionOrderNumber = "30049",
                Campaign = "I25",
                SupplierCode = "12345"
            });

            Assert.False(handler.LastRequest.Headers.Contains("x-vendorid"));
        }

        [Fact]
        public async Task GetLabelOrders_SendsPostWithBearerUserAgentAndBody()
        {
            var handler = new CaptureHandler(_ =>
            {
                var responseBody = JsonConvert.SerializeObject(new InditexOrderData());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            });

            BaseServiceClient.client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(1)
            };

            var service = new ApiCallerService();
            service.Start("https://api.product.inditex.com/pubsup/b2b");
            var request = new LabelOrderRequest
            {
                ProductionOrderNumber = "30049",
                Campaign = "I25",
                SupplierCode = "12345"
            };

            await service.GetLabelOrders("api/v3/label-printing/supplier-data/search", "ACCESS_TOKEN", "12345", request);

            Assert.NotNull(handler.LastRequest);
            Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
            Assert.Equal(
                "https://api.product.inditex.com/pubsup/b2b/api/v3/label-printing/supplier-data/search",
                handler.LastRequest.RequestUri.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
            Assert.Equal("ACCESS_TOKEN", handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Equal("BusinessPlatform/1.0", handler.LastRequest.Headers.UserAgent.Single().ToString());
            Assert.Equal("12345", handler.LastRequest.Headers.GetValues("x-vendorid").Single());

            var jsonBody = await handler.LastRequest.Content.ReadAsStringAsync();
            Assert.Contains("\"productionOrderNumber\":\"30049\"", jsonBody);
            Assert.Contains("\"campaign\":\"I25\"", jsonBody);
            Assert.Contains("\"supplierCode\":\"12345\"", jsonBody);
        }

        private sealed class CaptureHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

            public CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                this.responseFactory = responseFactory;
            }

            public HttpRequestMessage LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(responseFactory(request));
            }
        }
    }
}
