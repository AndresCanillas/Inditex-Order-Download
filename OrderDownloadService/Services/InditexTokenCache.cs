using System;
using System.Collections.Generic;

namespace OrderDonwLoadService.Services
{
    public class InditexTokenCache
    {
        private readonly Dictionary<string, TokenInfo> tokens = new Dictionary<string, TokenInfo>();

        private class TokenInfo
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public bool TryGetValidToken(string vendorId, DateTime now, out string token)
        {
            token = null;
            if(!tokens.TryGetValue(vendorId, out var info))
                return false;

            if(info.ExpiresAt <= now)
                return false;

            token = info.Token;
            return true;
        }

        public void StoreToken(string vendorId, string token, DateTime expiresAt)
        {
            tokens[vendorId] = new TokenInfo
            {
                Token = token,
                ExpiresAt = expiresAt
            };
        }

        public void RemoveToken(string vendorId)
        {
            tokens.Remove(vendorId);
        }
    }
}
