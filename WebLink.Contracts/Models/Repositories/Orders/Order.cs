using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Order : IOrder, ICompanyFilter<Order>, ISortableSet<Order>
    {
        public const string REPEAT_PATTERN = @"(-\d+[R])$";
        public const string DERIVATION_PATTERN = @"(-\d+[D])$";

        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        public int OrderDataID { get; set; }        // dynamic db identifier
        [MaxLength(16)]
        public string OrderNumber { get; set; }     // client order number
        public string MDOrderNumber { get; set; }   // ERP Middleware order number
        public DateTime OrderDate { get; set; }
        [MaxLength(50)]
        public string UserName { get; set; }
        public DocumentSource Source { get; set; }
        public int Quantity { get; set; }
        public ProductionType ProductionType { get; set; }
        public int? AssignedPrinterID { get; set; }
        public OrderStatus OrderStatus { get; set; }
        [NotMapped]
        public string OrderStatusText { get => OrderStatus.GetText(); }
        public int? LocationID { get; set; }
        public Location Location { get; set; }
        public bool PreviewGenerated { get; set; }
        public bool? PrintPackageGenerated { get; set; }
        [MaxLength(30)]
        public string BillTo { get; set; }
        [MaxLength(30)]
        public string SendTo { get; set; }
        public int BillToCompanyID { get; set; }
        public int SendToCompanyID { get; set; }
        public int? SendToAddressID { get; set; }       // Address that will be used to deliver the order to the client
        public int? SendToLocationID { get; set; }      // SendToLocationID is probably the same as LocationID (represents production location or factory)...  Use LocationID instead... Remove this later
        public string ValidationUser { get; set; }
        public DateTime? ValidationDate { get; set; }   // Date in which this order was validated
        public DateTime? DueDate { get; set; }         // Date in which this order has to be delivered, this is automatically calculated based on the SLA
        public int OrderGroupID { get; set; }           // Grupo al que pertenece
        public int? ParentOrderID { get; set; }
        public int? ProviderRecordID { get; set; }
        
        [LazyLoad,/*ForeignKey("ProviderRecordID")*/]
        public CompanyProvider Provider { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        #region Flags
        public bool ConfirmedByMD { get; set; }
        public bool IsBilled { get; set; }
        public bool IsBillable { get; set; } = true;
        public bool IsInConflict { get; set; }
        public bool IsStopped { get; set; }
        public bool DuplicatedEPC { get; set; }
        public bool HasOrderWorkflow { get; set; }
        public long? ItemID { get; set; }
        public bool AllowRepeatedOrders { get; set; } = true;

        public DeliveryStatus DeliveryStatusID { get; set; } // ID of the delivery note in Print Central    

        #endregion Flags

        #region Sage Mappings

        public bool SyncWithSage { get; set; }
        [MaxLen(128)]
        public string SageReference { get; set; }
        [MaxLen(128)]
        public string ProjectPrefix { get; set; }
        public SageInvoiceStatus InvoiceStatus { get; set; }
        public SageDeliveryStatus DeliveryStatus { get; set; }
        public SageOrderStatus SageStatus { get; set; }
        public SageCreditStatus CreditStatus { get; set; }
        public DateTime? RegisteredOn { get; set; }

        [NotMapped]
        public string InvoiceStatusText { get => InvoiceStatus.GetText(); }
        [NotMapped]
        public string DeliveryStatusText { get => DeliveryStatus.GetText(); }
        [NotMapped]
        public string SageStatusText { get => SageStatus.GetText(); }
        [NotMapped]
        public string CreditStatusText { get => CreditStatus.GetText(); }

        #endregion SageMappings

        public int GetCompanyID(PrintDB db)
        {
            var r = (from proj in db.Projects
                     join brand in db.Brands on proj.BrandID equals brand.ID
                     where proj.ID == ProjectID
                     select brand.CompanyID).SingleOrDefault();
            return r;
        }

        public Task<int> GetCompanyIDAsync(PrintDB db) =>
            (from proj in db.Projects
             join brand in db.Brands on proj.BrandID equals brand.ID
             where proj.ID == ProjectID
             select brand.CompanyID).SingleOrDefaultAsync();

        public IQueryable<Order> FilterByCompanyID(PrintDB db, int companyid) =>
            from o in db.CompanyOrders
            join prj in db.Projects on o.ProjectID equals prj.ID
            join brand in db.Brands on prj.BrandID equals brand.ID
            where brand.CompanyID == companyid
            select o;

        public IQueryable<Order> ApplySort(IQueryable<Order> qry) => qry.OrderByDescending(p => p.CreatedDate);
    }
}

