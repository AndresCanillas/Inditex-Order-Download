using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class PackArticle : IPackArticle
    {
        public int ID { get; set; }
        public int PackID { get; set; }
        public Pack Pack { get; set; }
        public int? ArticleID { get; set; }
        public Article Article { get; set; }
        public int Quantity { get; set; }
        public PackArticleType Type { get; set; }
        public int? CatalogID { get; set; }                 // TODO: Remove, packs will be expanded using data from the file, not the database
        public Catalog Catalog { get; set; }                // TODO: Remove, packs will be expanded using data from the file, not the database
        public string FieldName { get; set; }
        public PackArticleCondition Condition { get; set; } // TODO: Remove, assume case insensitive equality (any logic more complex than that would be solved by implementing support for Plugins)
        public string Mapping { get; set; }
        public string PluginName { get; set; }
        public bool AllowEmptyValues { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

    }
}

