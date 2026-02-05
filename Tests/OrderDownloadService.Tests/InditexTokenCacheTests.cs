using System;
using OrderDonwLoadService.Services;
using Xunit;

namespace OrderDownloadService.Tests
{
    public class InditexTokenCacheTests
    {
        [Fact]
        public void TryGetValidToken_WhenMissing_ReturnsFalse()
        {
            var cache = new InditexTokenCache();

            var result = cache.TryGetValidToken("vendor", DateTime.UtcNow, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void TryGetValidToken_WhenNotExpired_ReturnsToken()
        {
            var cache = new InditexTokenCache();
            cache.StoreToken("vendor", "token-123", DateTime.UtcNow.AddMinutes(5));

            var result = cache.TryGetValidToken("vendor", DateTime.UtcNow, out var token);

            Assert.True(result);
            Assert.Equal("token-123", token);
        }

        [Fact]
        public void TryGetValidToken_WhenExpired_ReturnsFalse()
        {
            var cache = new InditexTokenCache();
            cache.StoreToken("vendor", "token-123", DateTime.UtcNow.AddMinutes(-1));

            var result = cache.TryGetValidToken("vendor", DateTime.UtcNow, out var token);

            Assert.False(result);
            Assert.Null(token);
        }
    }
}
