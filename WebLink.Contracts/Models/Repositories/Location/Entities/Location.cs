using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Location : ILocation, ICompanyFilter<Location>, ISortableSet<Location>
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string DeliverTo { get; set; }
        [MaxLength(50)]
        public string AddressLine1 { get; set; }
        [MaxLength(50)]
        public string AddressLine2 { get; set; }
        [MaxLength(50)]
        public string CityOrTown { get; set; }
        [MaxLength(50)]
        public string StateOrProvince { get; set; }
        [MaxLength(30)]
        public string Country { get; set; }
        [MaxLength(8)]
        public string ZipCode { get; set; }

        // The following fields only make sense if this is a Smartdots factory, these fields are used during billing and production
        // =========================================
        [MaxLength(25)]
        public string FactoryCode { get; set; } // reference location of Sales Plant in Sage , to identify where order will be manufacture

        // The following are used to calculate delivery dates for orders based of the SLA, Takins into account WorkingDays, Holidays and CutoffTime (which can be configured for each factory).
        public int WorkingDays { get; set; }    // Flags indicating which week days are working days in this factory, multiple days are XORed together. Mon = 1, Tue = 2, ..., Sun = 64.
        public string Holidays { get; set; }    // JSON array containing "holidays" for this factory. Each holyday is composed of 3 fields: Name, Month & Day.
        public string CutoffTime { get; set; }  // Represents the cutoff time, must be in the format "HH:mm". If an order is received after this time of the day, SLA will start counting as if it was received the next day.

        public int? ProductionManager1 { get; set; }  // The id of the user that is the production manager for this location (used for emails & notifications). If null then check SmartDots company for company wide defaults; if that is null too, then an error notification will be sent to SysAdmin.
        public int? ProductionManager2 { get; set; }  // Similar to previous field, is the id of the user that is the production manager backup, in this case if it is null then nothing is done (simply the notification is sent to ProductionManager1)
        public bool EnableERP { get; set; } = false;           // Enable Invoice System for this factory, only support one ERP Invoice For all Factories, require update in future for multiple ERP Systems
                                                               // =========================================

        public int CountryID { get; set; }

        public string ERPCurrency { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int MaxNotEncodingQuantity { get; set; }
        [MaxLength(16)]
        public string FscCode { get; set; }

        public void Rename(string name) => Name = name;

        public int GetCompanyID(PrintDB db) => CompanyID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(CompanyID);

        public IQueryable<Location> FilterByCompanyID(PrintDB db, int companyid) =>
            from l in db.Locations
            where l.CompanyID == companyid
            select l;

        public IQueryable<Location> ApplySort(IQueryable<Location> qry) => qry.OrderBy(p => p.Name);
    }
}

