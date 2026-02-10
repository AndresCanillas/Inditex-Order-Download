using StructureInditexOrderFile;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public interface IQrProductSyncService
    {
        Task SyncAsync(InditexOrderData order);
    }
}
