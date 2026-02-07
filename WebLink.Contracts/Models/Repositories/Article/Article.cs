using Service.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Article : IArticle, ISortableSet<Article>
    {
        public const string EMPTY_ARTICLE_CODE = "000000";

        public int ID { get; set; }
        public int? ProjectID { get; set; }
        public Project Project { get; set; }
        [MaxLength(30)]
        public string Name { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; }
        [MaxLength(25)]
        public string ArticleCode { get; set; }         // Code used in order files (when the client request this article to be produced, they will refer to it using this code)
        [MaxLength(25)]
        public string BillingCode { get; set; }         // Code used for billing
        public int? LabelID { get; set; }
        public LabelData Label { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        [MaxLength(4000)]
        public string Instructions { get; set; }        // simple field for instruction,  only one language
        public int? CategoryID { get; set; }
        public Category Category { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }
        public bool EnableLocalPrint { get; set; }
        public bool EnableConflicts { get; set; }
        public Guid PrintCountSequence { get; set; }    // These fields are used to generate a unique counter for each article that has been printed, this can be used to serialize labels in some cases.
        public PrintCountSequenceType PrintCountSequenceType { get; set; }
        public string PrintCountSelectorField { get; set; }
        public SelectorType PrintCountSelectorType { get; set; }  // Indicates how to normalize the valur of the selector filed

        public void Rename(string name) => Name = name;

        public IQueryable<Article> ApplySort(IQueryable<Article> qry) => qry.OrderBy(p => p.Name);

        public bool EnableAddItems { get; set; }

        [MaxLength(2000)]
        public string ExportBlockedLocationIds { get; set; } = "[]";
    }
}

