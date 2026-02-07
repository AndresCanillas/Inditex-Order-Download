using Service.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Artifact : IArtifact, ISortableSet<Artifact>
    {
        public int ID { get; set; }
        public int? ArticleID { get; set; }

        [LazyLoad]
        public Article Article { get; set; }

        public int? LabelID { get; set; }

        [LazyLoad]
        public LabelData Label { get; set; }
        public int LayerLevel { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }

        public bool EnablePreview { get; set; }
        public bool IsTail { get; set; }
        public bool IsHead { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; }
        public bool IsMain { get; set; }

        // methods
        public IQueryable<Artifact> ApplySort(IQueryable<Artifact> qry) => qry.OrderBy(p => p.Position);
    }
}

