namespace WebLink.Contracts.Sage
{
    public interface IWsRequestMessage
    {
        string Url { get; set; }
        string Header { get; set; }
        string Action { get; set; }
    }
}