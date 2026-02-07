using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Printer : IPrinter, ICompanyFilter<Printer>, ISortableSet<Printer>
    {
        public int ID { get; set; }
        [MaxLength(20)]
        public string DeviceID { get; set; }
        [MaxLength(50)]
        public string ProductName { get; set; }
        [MaxLength(35)]
        public string Name { get; set; }
        [MaxLength(30)]
        public string FirmwareVersion { get; set; }
        public DateTime? LastSeenOnline { get; set; }
        public Location Location { get; set; }
        public int LocationID { get; set; }
        [MaxLength(50)]
        public string DriverName { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        [MaxLength(50)]
        public string PrinterType { get; set; }
        public bool SupportsCutter { get; set; }
        public bool SupportsRFID { get; set; }
        public bool IsRemote { get; set; }
        [MaxLength(50)]
        public string IP { get; set; }
        [MaxLength(50)]
        public string Port { get; set; }

        public void Rename(string name) => Name = name;

        public int GetCompanyID(PrintDB db) =>
            (from l in db.Locations
             where l.ID == LocationID
             select l.CompanyID).Single();

        public async Task<int> GetCompanyIDAsync(PrintDB db) => await
            (from l in db.Locations
             where l.ID == LocationID
             select l.CompanyID).SingleAsync();

        public IQueryable<Printer> FilterByCompanyID(PrintDB db, int companyid) =>
            from p in db.Printers
            join l in db.Locations on p.LocationID equals l.ID
            where l.CompanyID == companyid
            select p;

        public IQueryable<Printer> ApplySort(IQueryable<Printer> qry) => qry.OrderBy(p => p.Name);
    }
}

