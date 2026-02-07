using OrderDonwLoadService.Services;
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
        }
    }
}
