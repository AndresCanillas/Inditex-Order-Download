using Service.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Brand : IBrand, ICompanyFilter<Brand>, ISortableSet<Brand>
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        [Nullable]
        public byte[] Icon { get; set; }
        public bool EnableFTPFolder { get; set; }
        public string FTPFolder { get; set; }
        public int? RFIDConfigID { get; set; }
        public RFIDConfig RFIDConfig { get; set; }

        public void Rename(string name) => Name = name;
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public int GetCompanyID(PrintDB db) => CompanyID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(CompanyID);

        public IQueryable<Brand> FilterByCompanyID(PrintDB db, int companyid) =>
            from b in db.Brands
            where b.CompanyID == companyid
            select b;

        public IQueryable<Brand> ApplySort(IQueryable<Brand> qry) => qry.OrderBy(p => p.Name);
    }
}

