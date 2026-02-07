using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Category : ICategory, ISortableSet<Category>
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        public string Name { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public void Rename(string name) => Name = name;

        public IQueryable<Category> ApplySort(IQueryable<Category> qry) => qry.OrderBy(m => m.Name);
    }
}

