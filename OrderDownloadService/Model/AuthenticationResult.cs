namespace OrderDonwLoadService.Model
{
    public class AuthenticationResult
    {
        public string token_type { get; set; }
        public double expires_in { get; set; }
        public string access_token { get; set; }
        public string id_token { get; set; }
        public string scope { get; set; }
    }

    [System.Obsolete("Use AuthenticationResult")]
    public class AutenticationResult : AuthenticationResult
    {
    }
}
