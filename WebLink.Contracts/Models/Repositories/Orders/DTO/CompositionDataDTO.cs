using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public class CompositionDataDTO
    {
        public List<CompositionLabelData> Composition { get; set; }
    }

    public class CompositionLabelData
    {
        public int OrderGroupId { get; set; }
        public int ArticleID { get; set; }
        [Obsolete("En Pruebas, se repite dentro del objeto CompositionDefinition, se agrego por facilidad")]
        public int OrderID { get; set; }
        public int Quantity { get; set; }
        public IList<CompositionDefinition> Definition { get; set; }

    }

    public enum CompositionRequiredOption
    {
        NO,
        YES,
        OPTIONAL
    }

    public class CompositionDefinition
    {
        public int ID { get; set; }
        public int OrderID { get; set; }
        public int Quantity { get; set; }
        public string KeyValue { get; set; }
        public string KeyName { get; set; }
        /// <summary>
        /// Not Defined => 0, By Fiber => 1, By Part => 2
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 0 => Not Defined
        /// 1 => For Clothes
        /// 2 => For Shoes 3 sections
        /// 3 => For shoes 2 sections
        /// </summary>
        public int TargetArticle { get; set; }
        public int EnableComposition { get; set; }
        public int EnableWashingRulesSection { get; set; }
        public int EnableExceptions { get; set; }
        public int WrTemplate { get; set; } // Washing Rules
        public string ArticleCode { get; set; }
        public int ProjectID { get; set; }
        public IList<Section> Sections { get; set; }
        public IList<CareInstruction> CareInstructions { get; set; }
        public int ProductDataID { get; set; }
        public int OrderDataID { get; set; }
        public int OrderGroupID { get; set; }
        public int ArticleID { get; set; }
        public CompositionRequiredOption Required {get;set;}
        public int Product { get; set; }
        public int? ClonedFrom { get; set; }    

        public string GenerateCompoText { get; set; }

        public int ExceptionsLocation { get; set; } 
        public CompositionDefinition()
        {
            Required = CompositionRequiredOption.OPTIONAL;
        }
    }


    public static class CareInstructionCategory
    {
        public static readonly string WASH = "Wash";
        public static readonly string BLEACH = "Bleach";
        public static readonly string IRON = "Iron";
        public static readonly string DRYCLEANING = "DryCleaning";
        public static readonly string DRY = "Dry";
        public static readonly string ADDITIONAL = "Additional";
        public static readonly string EXCEPTION = "Exception";
    }

    public class CareInstruction
    {
        public int ID { get; set; }
        public int Instruction { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public int CompositionID { get; set; }
        public string AllLangs { get; set; }
        public string SymbolType { get; set; } // Font or Image
        public string Symbol { get; set; } // character or Image File Name
        public int Position { get; set; }
    }

    public class Section
    {
        public int ID { get; set; }
        public int SectionID { get; set; }
        public string Code { get; set; }
        public string Percentage { get; set; }
        public int Sort { get; set; }
        public IList<Fiber> Fibers { get; set; }
        public int CompositionID { get; set; }
        public string AllLangs { get; set; }
        public bool IsBlank { get; set; }
        public bool IsMainTitle { get; set; }

    }

    public class Fiber
    {
        public int ID { get; set; }
        public int FiberID { get; set; }
        public string Code { get; set; }
        public string Percentage { get; set; }
        public string CountryOfOrigin { get; set; }
        public string FiberType { get; set; }
        public string FiberIcon { get; set; }
        public int SectionID { get; set; }
        public string AllLangs { get; set; }
    }

    public class ProjectCatalogData
    {
        public int CatalogID { get; set; }
        public string Name { get; set; }
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string CompanyName { get; set; }
        public string Definition { get; set; }
    }

    public enum CompoCatalogName
    {
        ALL,
        MADEIN,
        SECTIONS,
        FIBERS,
        CAREINSTRUCTIONS,
        ADDITIONALS,
        EXCEPTIONS
    }
}
