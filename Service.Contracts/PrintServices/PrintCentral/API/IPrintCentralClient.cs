using Service.Contracts.Documents;
using System.Threading.Tasks;

namespace Service.Contracts.PrintCentral
{
    public interface IPrintCentralClient
    {
        string Url { get; set; }
        string Token { get; set; }
        bool Authenticated { get; }
        void Login(string controller, string userName, string password);
        Task LoginAsync(string controller, string userName, string password);
        string Logout();
        Task<string> LogoutAsync();
        Task<Output> FtpServiceUpload<Input, Output>(string controller, Input input, string filePath);
        Task<OperationResult> AddCompanyAsync<T1, T2>(string v, T1 suplier);
        Task<Output> AddProviderAsync< Input, Output>(string controller, Input provider);
        Task<Output> AddAddress<Input, Output>(string controller, Input address);

        Task<Output> GetProviderAsync<Output>(string controller);
    }
}