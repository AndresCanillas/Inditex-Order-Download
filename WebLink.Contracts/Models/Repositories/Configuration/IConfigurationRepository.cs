using System;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IConfigurationRepository
    {
        int FindProductionLocationID(int companyid, string clientReference, int projectid);
        int FindProductionLocationID(PrintDB ctx, int companyid, string clientReference, int projectid);

        ILocation GetProductionLocationAndSLA(int companyid, string clientReference, int projectid, out int SLADays);
        ILocation GetProductionLocationAndSLA(PrintDB ctx, int companyid, string clientReference, int projectid, out int SLADays);

        DateTime GetOrderDueDate(int companyid, string clientReference, int projectid);
        DateTime GetOrderDueDate(PrintDB ctx, int companyid, string clientReference, int projectid);

        int FindDefaultDeliveryAddress(int sendToCompanyID);
        int FindDefaultDeliveryAddress(PrintDB ctx, int sendToCompanyID);

        Task<OrderParameters> GetOrderParametersAsync(PrintDB ctx, int companyid, string clientReference, int projectid);
        Task<OrderParameters> GetOrderParametersAsync(PrintDB ctx, int companyid, string clientReference, int projectid, int locationid);
    }
}
