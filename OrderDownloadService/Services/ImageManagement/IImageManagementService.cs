using System.Threading.Tasks;
using StructureInditexOrderFile;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public interface IImageManagementService
    {
        Task<ImageProcessingResult> ProcessOrderImagesAsync(InditexOrderData order);
        Task<bool> AreOrderImagesReadyAsync(string orderFilePath);
    }
}
