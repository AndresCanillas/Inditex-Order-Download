using OrderDonwLoadService.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services
{
    public interface IPrintCentralService
    {
        string Url { get; set; }
        string Token { get; }
        bool Authenticated { get; }
        DateTime ExpirationDate { get; }
        void Login(string loginUrl, string userName, string password);
        string Logout();
        Task<string> LogoutAsync();
        Task LoginAsync(string loginUrl, string userName, string password);
        Task<User> GetUserAsync(string username);
        Task<List<Brand>> GetBrandAsync(int companyId);
        Task<List<Project>> GetByBrandID(int brandId);
        Task<List<Catalog>> GetCatalogsByProjectID(int projectId);
        Task<List<CatalogData>> GetCatalogDataByID(int catalogid);
        Task<Output> FtpServiceUpload<Input, Output>(string controller, Input input, string filePath);
        Task<List<CompanyOrderDTO>> GetOrder(int companyId, int projectId, string orderNumber);
        Task<bool> CreateFile(string storeName, int fileid, string filename);
        Task<int> CreateAttachment(string storeName, int fileid, string category, string filename);
        Task SetAttachmentContent(string storeName, int fileid, string category, int attachmentId, string filePath);
    }
}
