using Newtonsoft.Json;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class Catalog : ICatalog, ISortableSet<Catalog>
    {

        public const string ORDER_CATALOG = "Orders";
        public const string ORDERDETAILS_CATALOG = "OrderDetails";
        public const string VARIABLEDATA_CATALOG = "VariableData";
        public const string COMPOSITIONLABEL_CATALOG = "CompositionLabel";
        public const string CMP_USER_SECTIONS_CATALOG = "UserSections";
        public const string CMP_USER_FIBERS_CATALOG = "UserFibers";
        public const string CMP_USER_CAREINSTRUCTIONS_CATALOG = "UserCareInstructions";
        public const string BRAND_CAREINSTRUCTIONS_TEMPLATES_CATALOG = "Templates";
        public const string BRAND_SECTIONS_CATALOG = "Sections";
        public const string BRAND_FIBERS_CATALOG = "Fibers";
        public const string BRAND_CAREINSTRUCTIONS_CATALOG = "CareInstructions";
        public const string BRAND_MADEIN_CATALOG = "MadeIn";
        public const string BASEDATA_CATALOG = "BaseData";

        public static IList<string> GetAllCompoCatalogNames => new List<string> {
            ORDER_CATALOG,
            ORDERDETAILS_CATALOG,
            VARIABLEDATA_CATALOG,
            COMPOSITIONLABEL_CATALOG,
            CMP_USER_SECTIONS_CATALOG,
            CMP_USER_FIBERS_CATALOG,
            CMP_USER_CAREINSTRUCTIONS_CATALOG,
            BRAND_SECTIONS_CATALOG,
            BRAND_FIBERS_CATALOG,
            BRAND_CAREINSTRUCTIONS_CATALOG,
            BRAND_CAREINSTRUCTIONS_TEMPLATES_CATALOG,
            BASEDATA_CATALOG
        };


        public int ID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        public int CatalogID { get; set; }
        public string Name { get; set; }
        public string Captions { get; set; }
        public string Definition { get; set; }
        public int SortOrder { get; set; }
        public bool IsSystem { get; set; }
        public bool IsHidden { get; set; }
        public bool IsReadonly { get; set; }
        public CatalogType CatalogType { get; set; }
        public string RequiredRoles { get; set; }  // Ej: "CompanyAdmin,IDTProdManager"
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public IQueryable<Catalog> ApplySort(IQueryable<Catalog> qry) => qry.OrderBy(p => p.SortOrder);
        [NotMapped]
        public string TableName { get => $"{Name}_{CatalogID}"; }
        [NotMapped]
        [JsonIgnore]
        public IList<FieldDefinition> Fields
        {
            get
            {

                if(Definition == null)
                    return new List<FieldDefinition>();

                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<FieldDefinition>>(Definition);
            }
        }
    }
}

