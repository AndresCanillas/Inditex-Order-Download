using System.Threading.Tasks;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public interface IImageDownloader
    {
        Task<DownloadedImage> DownloadAsync(string url);
    }
}
