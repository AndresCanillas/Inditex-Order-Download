using Service.Contracts.LabelService;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IPrinterRepository : IGenericRepository<IPrinter>
    {
        IPrinter GetByDeviceID(string deviceid);
        IPrinter GetByDeviceID(PrintDB ctx, string deviceid);

        List<IPrinter> GetByLocationID(int locationid);
        List<IPrinter> GetByLocationID(PrintDB ctx, int locationid);

        List<IPrinter> GetByCompanyID(int companyid);
        List<IPrinter> GetByCompanyID(PrintDB ctx, int companyid);

        IPrinterSettings GetSettings(int printerid, int articleid);
        IPrinterSettings GetSettings(PrintDB ctx, int printerid, int articleid);

        void UpdateSettings(IPrinterSettings settings);
        void UpdateSettings(PrintDB ctx, IPrinterSettings settings);

        void UpdateLastSeen(string deviceid, string productName, string firmware);
        void UpdateLastSeen(PrintDB ctx, string deviceid, string productName, string firmware);

        void ChangeLocation(int printerid, int locationid);
        void ChangeLocation(PrintDB ctx, int printerid, int locationid);

        List<PrinterDriverInfo> GetPrinterDrivers();
    }
}
