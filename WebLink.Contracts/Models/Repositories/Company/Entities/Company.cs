using Service.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Company : ICompany, ICompanyFilter<Company>, ISortableSet<Company>
    {
        public const int TEST_COMPANY_ID = 45;

        public int ID { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        public int? MainLocationID { get; set; }
        [MaxLength(50), Nullable]
        public string MainContact { get; set; }
        [MaxLength(50), Nullable]
        public string MainContactEmail { get; set; }
        [MaxLength(10), Nullable]
        public string Culture { get; set; }
        [MaxLength(2000), Nullable]
        public string Instructions { get; set; }
        [LazyLoad, Nullable]
        public byte[] Logo { get; set; }
        [MaxLength(12), Nullable]
        public string CompanyCode { get; set; }
        [MaxLength(30), Nullable]
        public string IDTZone { get; set; }
        [MaxLength(6), Nullable]
        public string GSTCode { get; set; }
        public int? GSTID { get; set; }
        [MaxLength(10)]
        public string ClientReference { get; set; }     // Similar to the value ClientReference found in CompanyProvider, this value is used to ensure we process the fields BillTo and SendTo of an order. This would be the code used by the Company to refer to itself in an order.
        public int? SLADays { get; set; }
        public int? DefaultProductionLocation { get; set; }
        public int? DefaultDeliveryLocation { get; set; }
        public bool ShowAsCompany { get; set; }
        [MaxLength(20)]
        public string FtpUser { get; set; }
        [MaxLength(500)]
        public string FtpPassword { get; set; }
        [MaxLength(50), Nullable]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50), Nullable]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public int? RFIDConfigID { get; set; }
        public RFIDConfig RFIDConfig { get; set; }

        [MaxLength(2000)]
        public string OrderSort { get; set; }
        [MaxLength(200)]
        public string HeaderFields { get; set; }
        [MaxLength(200)]
        public string StopFields { get; set; }

        // The following field define responsible people for notifications and emails. These are used only as defaults when the contacts of the respective project have not been configured.
        public string CustomerSupport1 { get; set; }    // Default Costumer assigned to this company, if null and if we need to send a notification, then sysadmin will receive an error notification.
        public string CustomerSupport2 { get; set; }    // Backup in case main costumer is not available. If null, then notification will be sent only to CustomerSupport1.
        public string ProductionManager1 { get; set; }  // Default production manager assigned to this company, if null and if we need to send a notification, then sysadmin will receive an error notification.
        public string ProductionManager2 { get; set; }  // Production manager backup. If null, then notification will be sent only to ProductionManager1.
        public string ClientContact1 { get; set; }      // Main responsible for this company on the client side. If null and if we need to send a notification, then sysadmin will receive an error notification.
        public string ClientContact2 { get; set; }      // Backup responsible for this company on the client side. If null then only ClientContact1 will be sent a notification.

        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }
        public bool IsBroker { get; set; }
        public bool? HasOrderWorkflow { get; set; }


        public void Rename(string name) => Name = name;

        public int GetCompanyID(PrintDB db) => ID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(ID);

        public IQueryable<Company> FilterByCompanyID(PrintDB db, int companyid) =>
            from c in db.Companies
            where c.ID == companyid
            select c;

        public IQueryable<Company> ApplySort(IQueryable<Company> qry) => qry.OrderBy(p => p.Name);
    }
}

