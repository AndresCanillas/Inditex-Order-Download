using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Country : ICountry, ISortableSet<Country>
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Alpha2 { get; set; }
        public string Alpha3 { get; set; }
        public string NumericCode { get; set; }

        public IQueryable<Country> ApplySort(IQueryable<Country> qry) => qry.OrderBy(m => m.Name);
    }
}

