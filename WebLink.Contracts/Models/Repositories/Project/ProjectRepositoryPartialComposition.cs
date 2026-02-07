using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public partial class ProjectRepository : GenericRepository<IProject, Project>, IProjectRepository
    {

        public bool CompositionCatalogsExist(int projectID)
        {
            using (PrintDB ctx = factory.GetInstance<PrintDB>())
                return CompositionCatalogsExist(ctx, projectID);
        }

        public bool CompositionCatalogsExist(PrintDB ctx, int projectID)
        {
            var catalogs = ctx.Catalogs
                .Where(w => w.ProjectID.Equals(projectID))
                .Where(w => w.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG))
                .ToList();

            return catalogs.Count > 0;
        }

        //public void AddCompositionCatalogIfNotExist(int projectID)
        //{
        //    using (var ctx = factory.GetInstance<PrintDB>())
        //    {
        //        AddCompositionCatalogIfNotExist(ctx, projectID);
        //    }
        //}

        //public void AddCompositionCatalogIfNotExist(PrintDB ctx, int projectID)
        //{
        //    var project = GetByID(projectID);

        //    if (project.AllowAddOrChangeComposition)
        //        if (!CompositionCatalogsExist(project.ID))
        //        {
        //            var compoCatalog = AddCompositionCatalogs(project.ID);
        //            var variableDataCatalog = ctx.Catalogs.First(w => w.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
        //            var fiberSetField = JsonConvert.DeserializeObject<List<FieldDefinition>>(variableDataCatalog.Definition);

        //            // TODO no terminado
        //        }
        //}

        public ICatalog AddCompositionCatalogs(int id)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddCompositionCatalogs(ctx, id);
            }
        }

        public ICatalog AddCompositionCatalogs(PrintDB ctx, int id)
        {
            var project = (Project)GetByID(ctx, id);

            #region Lookups
            var ciDef = typeof(CareInstructionsCatalogTemplate).GetCatalogDefinition();
            ciDef.CatalogType = CatalogType.Lookup;
            var ciCatalog = CreateCatalog(ctx, project, Catalog.BRAND_CAREINSTRUCTIONS_CATALOG, ciDef);

            var templatesDef = typeof(TemplatesTemplate).GetCatalogDefinition();
            templatesDef.Fields.Add(new FieldDefinition() { FieldID = templatesDef.Fields.Count, Name = "CareInstructions", IsSystem = true, IsLocked = true, Type = ColumnType.Set, CatalogID = ciCatalog.CatalogID });
            templatesDef.CatalogType = CatalogType.Lookup;
            CreateCatalog(ctx, project, "Templates", templatesDef);

            var sectionsDef = typeof(SectionsCatalogTemplate).GetCatalogDefinition();
            sectionsDef.CatalogType = CatalogType.Lookup;
            var sectionsCatalog = CreateCatalog(ctx, project, Catalog.BRAND_SECTIONS_CATALOG, sectionsDef);

            var fiberDef = typeof(FibersCatalogTemplate).GetCatalogDefinition();
            fiberDef.CatalogType = CatalogType.Lookup;
            var fibersCatalog = CreateCatalog(ctx, project, Catalog.BRAND_FIBERS_CATALOG, fiberDef);
            #endregion Lookups

            //var variableDataCatalog = catalogRepo.GetByName(id , "VariableData");
            //var Fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(variableDataCatalog.Definition);

            var userFibersDef = typeof(UserCompositionFibersTemplate).GetCatalogDefinition();
            var rf = userFibersDef.Fields.Single(f => f.Name == "Fiber");
            rf.Type = ColumnType.Reference;
            rf.CatalogID = fibersCatalog.CatalogID;
            rf.IsSystem = true;
            rf.IsLocked = true;
            var userFibersCatalog = CreateCatalog(ctx, project, Catalog.CMP_USER_FIBERS_CATALOG, userFibersDef);

            var userSectionsDef = typeof(UserCompositionSectionTemplate).GetCatalogDefinition();
            
            var rs = userSectionsDef.Fields.Single(f => f.Name == "Section");
            rs.Type = ColumnType.Reference;
            rs.CatalogID = sectionsCatalog.CatalogID;
            rs.IsSystem = true;
            rs.IsLocked = true;

            var ruf = userSectionsDef.Fields.Single(f => f.Name == "Fibers");
            ruf.Type = ColumnType.Set;
            ruf.IsSystem = true;
            ruf.IsLocked = true;
            ruf.CatalogID = userFibersCatalog.CatalogID;

            var userSectionCatalog = CreateCatalog(ctx, project, Catalog.CMP_USER_SECTIONS_CATALOG, userSectionsDef);

            var userCiDef = typeof(UserCompositionCareInstructionsTemplate).GetCatalogDefinition();
            var rci = userCiDef.Fields.Single(f => f.Name == "Instruction");
            rci.Type = ColumnType.Reference;
            rci.CatalogID = ciCatalog.CatalogID;
            rci.IsSystem = true;
            rci.IsLocked = true;

            var userCiCatalog = CreateCatalog(ctx, project, Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG, userCiDef);

            var compoDef = typeof(UserCompositionTemplate).GetCatalogDefinition();

            var sectionSet = compoDef.Fields.Single(f => f.Name == "Sections");

            sectionSet.FieldID = compoDef.Fields.IndexOf(sectionSet);
            sectionSet.Name = "Sections";
            sectionSet.IsSystem = true;
            sectionSet.IsLocked = true;
            sectionSet.Type = ColumnType.Set;
            sectionSet.CatalogID = userSectionCatalog.CatalogID;
            
            var careInstructionsSet = compoDef.Fields.Single(f => f.Name == "CareInstructions");

            careInstructionsSet.FieldID = compoDef.Fields.IndexOf(careInstructionsSet);
            careInstructionsSet.Name = "CareInstructions";
            careInstructionsSet.IsSystem = true;
            careInstructionsSet.IsLocked = true;
            careInstructionsSet.Type = ColumnType.Set;
            careInstructionsSet.CatalogID = userCiCatalog.CatalogID;
            
            //compoDef.Fields.Add(sectionSet);
            //compoDef.Fields.Add(careInstructionsSet);

            var compoCatalog = CreateCatalog(ctx, project, Catalog.COMPOSITIONLABEL_CATALOG, compoDef);
            
            return compoCatalog;
        }
    }

    #region Composition Catalogs

    public class UserCompositionTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }
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
        
        public bool EnableCompositionSection { get; set; }
        public bool EnableWashingRulesSection { get; set; }
        public bool EnableExceptions { get; set; }
        [MaxLength(8184)]
        public string FullComposition { get; set; }
        [MaxLength(4096)]
        public string FullCareInstructions { get; set; }
        public int Sections { get; set; }// to keep field positions
        public int CareInstructions { get; set; }// to keep field positions
        public int WrTemplate { get; set; }

        // set will be added while project's catalogs are created

    }

    public class UserCompositionSectionTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }
        public int Position { get; set; }
        [Nullable]
        public int Percentage { get; set; }
        public int Section { get; set; } // reference to lookup catalog of clothes parts
        public int Fibers { get; set; }
        public bool IsBlank {get;set;}  // default false, blank section allow to add a section without fibers

    }

    public class UserCompositionFibersTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }
        public int Position { get; set; }
        public int Percentage { get; set; }
        [Nullable]
        public string CountryOfOrigin { get; set; }
        [Nullable]
        public string FiberType { get; set; }
        [Nullable]
        public string FiberIcon { get; set; }
        
        public int Fiber { get; set; } // reference to lookup catalog of fibers
    }


    public class UserCompositionCareInstructionsTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }
        public int Position { get; set; }
        [MaxLength(64), Nullable]
        public string Icon { get; set; }
        public int Instruction { get; set; } // reference to lookup catalog of care instructions
    }


    public class SectionsCatalogTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }

        [MaxLength(16), Required]
        public string Code { get; set; }
        [MaxLength(64), Nullable]
        public string Category { get; set; }
        [MaxLength(128), Required]
        public string English { get; set; }
        public int Position { get; set; }
    }

    public class FibersCatalogTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }

        [MaxLength(16), Required]
        public string Code { get; set; }
        [MaxLength(64), Nullable]
        public string Category { get; set; }
        [MaxLength(128), Required]
        public string English { get; set; }
    }

    public class CareInstructionsCatalogTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }

        [MaxLength(16), Required]
        public string Code { get; set; }
        [Nullable]
        public int SymbolType { get; set; }  // No defined -> 0, Font -> 1, Image -> 2
        [MaxLength(64), Nullable]
        public string Symbol { get; set; }
        [MaxLength(64), Nullable]
        public string Category { get; set; }
        [MaxLength(128), Required]
        public string English { get; set; }
    }
    #endregion

     

}
