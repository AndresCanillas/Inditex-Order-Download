using System.Threading.Tasks;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public interface IImageAssetRepository
    {
        Task<ImageAssetRecord> GetLatestByUrlAsync(string url);
        ImageAssetRecord GetLatestByUrl(string url);
        Task<int> InsertAsync(ImageAssetRecord record);
        Task MarkObsoleteAsync(int id);
    }
}
