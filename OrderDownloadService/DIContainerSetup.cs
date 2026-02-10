using OrderDonwLoadService.Services;
using OrderDonwLoadService.Services.ImageManagement;
using OrderDonwLoadService.Synchronization;
using Service.Contracts;

namespace OrderDonwLoadService
{
    public class DIContainerSetup
    {
        public DIContainerSetup(IFactory factory)
        {

            factory.RegisterSingleton<IPrintCentralService, PrintCentralService>();
            factory.RegisterSingleton<IOrderServices, OrderServices>();
            factory.RegisterTransient<IApiCallerService, ApiCallerService>();
            factory.RegisterTransient<IImageAssetRepository, ImageAssetRepository>();
            factory.RegisterSingleton<IImageDownloader, HttpImageDownloader>();
            factory.RegisterTransient<IQrProductSyncService, QrProductSyncService>();
            factory.RegisterTransient<IImageManagementService, ImageManagementService>();
        }
    }
}
