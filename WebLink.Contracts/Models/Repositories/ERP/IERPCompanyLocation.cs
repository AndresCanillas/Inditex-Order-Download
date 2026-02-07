using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IERPCompanyLocation : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        int ProductionLocationID { get; set; }
        int ERPInstanceID { get; set; }
        string BillingFactoryCode { get; set; }
        string ProductionFactoryCode { get; set; }
        string Currency { get; set; }
        string ExpeditionAddressCode { get; set; } // factory address - one Compnay factory has many locations
        int? DeliveryAddressID { get; set; }  // for taxes type on ERP - only apply for SAGE
    }
}
