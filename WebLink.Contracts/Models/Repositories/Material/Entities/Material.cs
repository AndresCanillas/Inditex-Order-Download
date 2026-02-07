using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Material : IMaterial, ISortableSet<Material>
    {
        public int ID { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        public string Properties { get; set; }          // A list of properties (key, value pairs) serialized as json
        public bool ShowAsMaterial { get; set; }        // true = The material shows up in the materials catalog (and can be reused in many different articles), false = The material is unique and belongs to ONE article, therefore it will no showup in the materials catalog nor be possible to reuse this material in other articles.
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public void Rename(string name) => Name = name;
        public IQueryable<Material> ApplySort(IQueryable<Material> qry) => qry.OrderBy(p => p.Name);
    }
}

