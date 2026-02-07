using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Address : IAddress, ISortableSet<Address>
    {
        public int ID { get; set; }
        [MaxLength(40)]
        public string Name { get; set; }
        [MaxLength(128)]
        public string AddressLine1 { get; set; }
        [MaxLength(128)]
        public string AddressLine2 { get; set; }
        [MaxLength(128)]
        public string AddressLine3 { get; set; }
        [MaxLength(128)]
        public string CityOrTown { get; set; }
        [MaxLength(128)]
        public string StateOrProvince { get; set; }
        [MaxLength(128)]
        public string Country { get; set; }
        public int CountryID { get; set; }
        [MaxLength(8)]
        public string ZipCode { get; set; }
        public string Notes { get; set; }
        public bool Default { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }
        public string SageProvinceCode { get; set; }
        public string Telephone1 { get; set; }
        public string Telephone2 { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string BusinessName1 { get; set; }
        public string BusinessName2 { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }


        public IQueryable<Address> ApplySort(IQueryable<Address> qry) => qry.OrderBy(p => p.Name);
    }
}

