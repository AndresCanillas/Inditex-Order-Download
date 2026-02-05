namespace OrderDonwLoadService.Model
{
    public class AutenticationResult
    {
        public string token_type { get; set; }
        public double expires_in { get; set; }
        public string access_token { get; set; }
        public string scope { get; set; }

    }

}
