using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Pack : IPack, ISortableSet<Pack>
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        [MaxLength(30)]
        public string Name { get; set; }                // A friendly name for the pack
        public string Description { get; set; }         // Brief description of the pack
        [MaxLength(25)]
        public string PackCode { get; set; }            // Code used in order files (when the client request this pack to be produced, they will refer to it using this code)
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public void Rename(string name) => Name = name;
        public IQueryable<Pack> ApplySort(IQueryable<Pack> qry) => qry.OrderBy(p => p.ProjectID).ThenBy(p => p.Name);
    }
}

