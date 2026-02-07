using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Contact : IContact, ICompanyFilter<Contact>, ISortableSet<Contact>
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        public string Comments { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public int GetCompanyID(PrintDB db) => CompanyID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(CompanyID);

        public IQueryable<Contact> FilterByCompanyID(PrintDB db, int companyid) =>
            from c in db.Contacts
            where c.CompanyID == companyid
            select c;

        public IQueryable<Contact> ApplySort(IQueryable<Contact> qry) => qry.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
    }
}

