using System.Threading.Tasks;
using Moq;
using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using Service.Contracts;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class OrderDownloadHelperTests
    {
        [Fact]
        public async Task CallGetToken_UsesTokenControllerAndScopeFromConfig()
        {
            var appConfig = new Mock<IAppConfig>();
            appConfig.Setup(c => c.GetValue("DownloadServices.TokenApiUrl", It.IsAny<string>()))
                .Returns("https://auth.inditex.com:443/");
            appConfig.Setup(c => c.GetValue("DownloadServices.ControllerToken", It.IsAny<string>()))
                .Returns("openam/oauth2/itxid/itxidmp/b2b/access_token");
            appConfig.Setup(c => c.GetValue("DownloadServices.TokenScope", It.IsAny<string>()))
                .Returns("scope_1 scope_n");
            appConfig.Setup(c => c.GetValue("DownloadServices.MaxTrys", 2)).Returns(2);
            appConfig.Setup(c => c.GetValue("DownloadServices.SecondsToWait", 240d)).Returns(1d);

            var apiCaller = new Mock<IApiCallerService>();
            apiCaller.Setup(a => a.GetToken(
                    "https://auth.inditex.com/openam/oauth2/itxid/itxidmp/b2b/access_token",
                    "client-id",
                    "secret",
                    "scope_1 scope_n"))
                .ReturnsAsync(new AuthenticationResult { access_token = "token" });

            var result = await OrderDownloadHelper.CallGetToken(appConfig.Object, "client-id", "secret", apiCaller.Object);

            Assert.Equal("token", result.access_token);
            apiCaller.VerifyAll();
        }

        [Fact]
        public async Task CallGetToken_UsesIdTokenWhenAccessTokenIsMissing()
        {
            var appConfig = new Mock<IAppConfig>();
            appConfig.Setup(c => c.GetValue("DownloadServices.TokenApiUrl", It.IsAny<string>())).Returns("https://auth");
            appConfig.Setup(c => c.GetValue("DownloadServices.ControllerToken", It.IsAny<string>())).Returns("oauth/token");
            appConfig.Setup(c => c.GetValue("DownloadServices.TokenScope", It.IsAny<string>())).Returns("inditex");
            appConfig.Setup(c => c.GetValue("DownloadServices.MaxTrys", 2)).Returns(2);
            appConfig.Setup(c => c.GetValue("DownloadServices.SecondsToWait", 240d)).Returns(1d);

            var apiCaller = new Mock<IApiCallerService>();
            apiCaller.Setup(a => a.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationResult { id_token = "id-token-only" });

            var result = await OrderDownloadHelper.CallGetToken(appConfig.Object, "client-id", "secret", apiCaller.Object);

            Assert.Equal("id-token-only", result.access_token);
        }

        [Fact]
        public void LoadInditexCreadentials_BackwardCompatibility_DelegatesToNewMethod()
        {
            var log = new Mock<IAppLog>();

            var result = OrderDownloadHelper.LoadInditexCreadentials(log.Object);

            Assert.Null(result);
            log.Verify(l => l.LogMessage(It.Is<string>(m => m.Contains("InditexCredentials.json"))), Times.Once);
        }
    }
}
