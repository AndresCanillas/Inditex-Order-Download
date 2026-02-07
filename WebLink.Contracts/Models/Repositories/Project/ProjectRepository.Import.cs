using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Models
{
    public partial class ProjectRepository : GenericRepository<IProject, Project>, IProjectRepository
	{
		public void ImportProject(int projectid, string filePath)
		{
			ProjectImportInfo importInfo = new ProjectImportInfo();
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				importInfo.ctx = ctx;
				importInfo.project = GetByID(ctx, projectid);
				importInfo.brand = ctx.Brands.Where(b => b.ID == importInfo.project.BrandID).AsNoTracking().Single();
				importInfo.company = ctx.Companies.Where(c => c.ID == importInfo.brand.CompanyID).AsNoTracking().Single();
				using (FileStream fs = new FileStream(filePath, FileMode.Open))
				{
					using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						importInfo.archive = archive;

						var entries = archive.Entries.Where(x => x.FullName.Contains("Data/"));
						if (entries.Count() > 0)
						{
							foreach (var entry in entries)
							{
								var reader = new StreamReader(entry.Open(), Encoding.UTF8);
								importInfo.data.Add(entry.Name.Split(".").FirstOrDefault(), reader.ReadToEnd());
							}
							ImportProcess(importInfo);
							ImportFiles(importInfo);
						}
					}
				}
			}
		}


		private void ImportProcess(ProjectImportInfo importInfo)
        {
			ImportProject(importInfo);
			ImportMaterials(importInfo);
            ImportCategories(importInfo);
            ImportLabels(importInfo);
            ImportArticles(importInfo);
            ImportArtifacts(importInfo);
			ImportCatalogs(importInfo);
			ImportLookupCatalogData(importInfo);
			ImportPacks(importInfo);
			ImportPackArticles(importInfo);
			ImportMappings(importInfo);
			ImportMappingsCols(importInfo);
			ImportArticlePreviewSettings(importInfo);
            ImportComparerConfiguration(importInfo);
            ImportWizardCustomSteps(importInfo);
			ImportCompanyProviderCompanies(importInfo);
			ImportCompanyProviders(importInfo);
            ImportCareLabelsCompoConfig (importInfo); 
        }

        private void ImportCareLabelsCompoConfig(ProjectImportInfo importInfo)
        {
            if(!importInfo.data.ContainsKey("ArticleCompositionConfigs"))
                return;

            var json = importInfo.data["ArticleCompositionConfigs"];
            if(String.IsNullOrWhiteSpace(json))
                return;

            var repo = factory.GetInstance<IArticleRepository>();
            var ArticleCompositionConfigs = JsonConvert.DeserializeObject<List<ArticleCompositionConfig>>(json);
            var ids = new Dictionary<int, int>();
            importInfo.referenceMap["ArticleCompositionConfigs"] = ids;

            var existing = repo.GetCompositionConfigByProjectID(importInfo.project.ID);

            foreach(var article in ArticleCompositionConfigs)
            {
                var match = existing.FirstOrDefault(l => l.ArticleCode == article.ArticleCode);
                if(match == null)
                {
                    match = new ArticleCompositionConfig();
                    match.ProjectID = importInfo.project.ID;
                    match.ArticleID = GetArticleID(article.ArticleCode, importInfo.project.ID); 
                }

                // Copy all properties save for ID, ProjectID, LabelID & CategoryID fields
                Reflex.Copy(match, article, new string[] { "ID", "ProjectID", "ArticleID" });


                if(match.ArticleID != 0)
                {
                    repo.SaveArticleComposition(match, "SYSTEM");
                    ids.Add(article.ID, match.ID);
                }
            }
        }

        private int GetArticleID(string articleCode, int projectid )
        {
            if(string.IsNullOrWhiteSpace(articleCode))
                return 0;

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                try
                {
                    var articleId = ctx.Articles
                        .Where(x => x.ArticleCode.ToUpper() == articleCode.ToUpper() &&
                               x.ProjectID == projectid)
                        .Select(x => x.ID)
                        .SingleOrDefault();

                    return articleId;
                }
                catch(InvalidOperationException)
                {
                    // Multiple articles found with the same code
                    return 0;
                }
                catch(Exception)
                {
                    // Any other database error
                    return 0;
                }
            }
        }

        private void ImportProject(ProjectImportInfo importInfo)
		{
			var projectData = importInfo.data["Project"];
			var source = JsonConvert.DeserializeObject<Project>(projectData);

			// Copy all properties save for the ID, Name, BrandID and RFIDConfigID fields
			Reflex.Copy(importInfo.project, source, new string[] { "ID", "Name", "BrandID", "RFIDConfigID", "FTPClients", "EnableFTPFolder", "FTPFolder", "ProjectCode", "CreatedDate", "OrderWorkflowConfigID" });

            ImportRFIDConfiguration(importInfo);
            ImportOrderWorkflowConfiguration(importInfo);
            Update(importInfo.ctx, importInfo.project);
		}


		private void ImportRFIDConfiguration(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("RFIDParameters"))
				return;

			var json = importInfo.data["RFIDParameters"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IRFIDConfigRepository>();
			RFIDConfig config = JsonConvert.DeserializeObject<RFIDConfig>(json);

			if (importInfo.project.RFIDConfigID != null)
			{
				config.ID = importInfo.project.RFIDConfigID.Value;
				repo.Update(importInfo.ctx, config);
			}
			else
			{
				config.ID = 0;
				var newConfig = repo.Insert(importInfo.ctx, config);
				importInfo.project.RFIDConfigID = newConfig.ID;
			}
		}

        private void ImportOrderWorkflowConfiguration(ProjectImportInfo importInfo)
        {
            if(!importInfo.data.ContainsKey("OrderWorkflowConfiguration"))
                return;

            var json = importInfo.data["OrderWorkflowConfiguration"];
            if(String.IsNullOrWhiteSpace(json))
                return;

            var repo = factory.GetInstance<IOrderWorkflowConfigRepository>();
            OrderWorkflowConfig config = JsonConvert.DeserializeObject<OrderWorkflowConfig>(json);

            if(importInfo.project.OrderWorkflowConfigID != null)
            {
                config.ID = importInfo.project.OrderWorkflowConfigID.Value;
                repo.Update(importInfo.ctx, config);
            }
            else
            {
                config.ID = 0;
                var newConfig = repo.Insert(importInfo.ctx, config);
                importInfo.project.OrderWorkflowConfigID = newConfig.ID;
            }
        }

        private void ImportMaterials(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Materials"))
				return;

			var json = importInfo.data["Materials"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IMaterialRepository>();
			var materials = JsonConvert.DeserializeObject<List<Material>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Materials"] = ids;

			var existing = repo.GetList();

			foreach (var material in materials)
			{
				var match = existing.FirstOrDefault(m => m.Name == material.Name);
				if (match == null)
					match = new Material();

				// Copy all properties save for the ID field
				Reflex.Copy(match, material, new string[] { "ID" });

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(material.ID, match.ID);
			}
		}


		private void ImportCategories(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Categories"))
				return;

			var json = importInfo.data["Categories"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<ICategoryRepository>();
			var categories = JsonConvert.DeserializeObject<List<Category>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Categories"] = ids;

			var existing = repo.GetByProject(importInfo.project.ID);

			foreach (var category in categories)
			{
				var match = existing.FirstOrDefault(c => c.Name == category.Name);
				if (match == null)
				{
					match = new Category();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for the ID and ProjectID fields
				Reflex.Copy(match, category, new string[] { "ID", "ProjectID" });

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(category.ID, match.ID);
			}
		}


		private void ImportLabels(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Labels"))
				return;

			var json = importInfo.data["Labels"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<ILabelRepository>();
			var labels = JsonConvert.DeserializeObject<List<LabelData>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Labels"] = ids;

			var existing = repo.GetByProjectID(importInfo.project.ID);

			foreach (var label in labels)
			{
				var match = existing.FirstOrDefault(l => l.Name == label.Name);
				if (match == null)
				{
					match = new LabelData();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID, ProjectID and MaterialID fields
				Reflex.Copy(match, label, new string[] { "ID", "ProjectID", "MaterialID" });

				if(label.MaterialID.HasValue)
					match.MaterialID = importInfo.referenceMap["Materials"][label.MaterialID.Value];
				else
					match.MaterialID = null;

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(label.ID, match.ID);
			}
		}


		private void ImportArticles(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Articles"))
				return;

			var json = importInfo.data["Articles"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IArticleRepository>();
			var articles = JsonConvert.DeserializeObject<List<Article>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Articles"] = ids;

			var existing = repo.GetByProjectID(importInfo.project.ID);

			foreach (var article in articles)
			{
				var match = existing.FirstOrDefault(l => l.ArticleCode == article.ArticleCode);
				if (match == null)
				{
					match = new Article();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID, ProjectID, LabelID & CategoryID fields
				Reflex.Copy(match, article, new string[] { "ID", "ProjectID", "LabelID", "CategoryID" });

				if (article.LabelID.HasValue)
					match.LabelID = importInfo.referenceMap["Labels"][article.LabelID.Value];
				else
					match.LabelID = null;

				if (article.CategoryID.HasValue)
					match.CategoryID = importInfo.referenceMap["Categories"][article.CategoryID.Value];
				else
					match.CategoryID = null;

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(article.ID, match.ID);
			}
		}


		private void ImportArtifacts(ProjectImportInfo importInfo)
        {
			if (!importInfo.data.ContainsKey("Artifacts"))
				return;

			var json = importInfo.data["Artifacts"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IArtifactRepository>();
			var artifacts = JsonConvert.DeserializeObject<List<Artifact>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Artifacts"] = ids;

			// NOTE: Artifacts are considered as part of the Articles (as if they were properties of the articles), 
			// and are to be completely overwritten when the project is imported. Hence, we first delete all
			// artifacts, then insert the ones that are being imported.
			importInfo.ctx.Database.ExecuteSqlCommand($@"
				delete from Artifacts where ID in
					(select a.ID from 
						Artifacts a 
						join Articles b on a.ArticleID = b.ID
						where b.ProjectID = {importInfo.project.ID})
			");


			foreach (var artifact in artifacts)
			{
				var match = new Artifact();

				// Copy all properties save for ID, ArticleID and LabelID fields
				Reflex.Copy(match, artifact, new string[] { "ID", "ArticleID", "LabelID" });

				match.ArticleID = importInfo.referenceMap["Articles"][artifact.ArticleID.Value];
				match.LabelID = importInfo.referenceMap["Labels"][artifact.LabelID.Value];
				repo.Insert(match);

				ids.Add(artifact.ID, match.ID);
			}
		}


		private void ImportCatalogs(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Catalogs"))
				return;

			var json = importInfo.data["Catalogs"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<ICatalogRepository>();
			var catalogs = JsonConvert.DeserializeObject<List<Catalog>>(json);

			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Catalogs"] = ids;

			var refs = new Dictionary<int, int>();
			importInfo.referenceMap["CatalogRefs"] = refs;

			var existing = repo.GetByProjectID(importInfo.project.ID);

			foreach (var catalog in catalogs)
			{
				var match = existing.FirstOrDefault(l => l.Name == catalog.Name);
				if (match == null)
				{
					match = new Catalog();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID, ProjectID & CatalogID fields
				Reflex.Copy(match, catalog, new string[] { "ID", "ProjectID", "CatalogID" });

				var fields = catalog.Fields;

				// Update reference and set fields
				var referenceFields = fields.Where(f => f.Type == ColumnType.Reference || f.Type == ColumnType.Set);
				foreach (var field in referenceFields)
				{
					if (field.CatalogID.HasValue)
						field.CatalogID = importInfo.referenceMap["CatalogRefs"][field.CatalogID.Value];
				}
				match.Definition = JsonConvert.SerializeObject(fields);

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

                ids.Add(catalog.ID, match.ID);
				refs.Add(catalog.CatalogID, match.CatalogID);
			}
		}


		private void ImportLookupCatalogData(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Catalogs"))
				return;

			var json = importInfo.data["Catalogs"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<ICatalogDataRepository>();
			var lookupCatalogs = JsonConvert.DeserializeObject<List<Catalog>>(json)
				.Where(x => x.CatalogType == CatalogType.Lookup).ToList();

			foreach (var catalog in lookupCatalogs)
			{
				var lookupJson = importInfo.data[$"{catalog.Name}_Data"];
				var catalogid = importInfo.referenceMap["Catalogs"][catalog.ID];
				repo.ImportLookupCatalog(catalogid, lookupJson);

                //Get Rel data
                var fields = catalog.Fields;
                foreach (var field in fields)
                {
                    if (field.Type == ColumnType.Set && field.CatalogID != null && field.CatalogID > 0)
                    {
                        var relData = importInfo.data[$"Rel_{catalog.ID}_{field.CatalogID}_{field.FieldID}_Data"];
                        var array = JArray.Parse(relData);

                        var oData = new Dictionary<int, List<int>>();

                        foreach (var elm in array)
                        {
                            var row = elm as JObject;
                            var sourceId = row.GetValue<int>("SourceID");
                            var targetId = row.GetValue<int>("TargetID");

                            if (!oData.ContainsKey(sourceId))
                            {
                                var list = new List<int>();
                                list.Add(targetId);
                                oData.Add(sourceId, list);
                            }
                            else
                            {
                                var values = oData[sourceId];
                                values.Add(targetId);
                                oData[sourceId] = values;                                    
                            }
                        }

                        using (var dynamicDB = factory.GetInstance<DynamicDB>())
                        {
                            dynamicDB.Open(connstr);
                            var newCatalogId = importInfo.referenceMap["Catalogs"][catalog.ID];
                            var catalogRepo = factory.GetInstance<ICatalogRepository>();
                            var result = catalogRepo.GetByID(newCatalogId);
                            var catalogDef = dynamicDB.Conn.SelectOne<CatalogDefinition>(result.CatalogID);

                            var refCatalog = lookupCatalogs.FirstOrDefault(x => x.CatalogID == field.CatalogID);
                            var fieldCatalogId = importInfo.referenceMap["Catalogs"][refCatalog.ID];
                            var fieldCatalog = catalogRepo.GetByID(fieldCatalogId);

                            if (fieldCatalog != null)
                            {
                                field.CatalogID = fieldCatalog.CatalogID;

                                foreach (var d in oData)
                                {
                                    var ids = string.Join(",", d.Value.Select(n => n.ToString()).ToArray());
                                    dynamicDB.InsertIntoRel(catalogDef, d.Key, field, ids);
                                }                    
                            }
                        }
                    }
                }
            }
		}


		private void ImportPacks(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Packs"))
				return;

			var json = importInfo.data["Packs"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IPackRepository>();
			var packs = JsonConvert.DeserializeObject<List<Pack>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Packs"] = ids;

			var existing = repo.GetByProjectID(importInfo.project.ID);

			foreach (var pack in packs)
			{
				var match = existing.FirstOrDefault(l => l.PackCode == pack.PackCode);
				if (match == null)
				{
					match = new Pack();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID & ProjectID fields
				Reflex.Copy(match, pack, new string[] { "ID", "ProjectID" });

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(pack.ID, match.ID);
			}
		}


		private void ImportPackArticles(ProjectImportInfo importInfo)
        {
			if (!importInfo.data.ContainsKey("PackArticles"))
				return;

			var json = importInfo.data["PackArticles"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var packArticles = JsonConvert.DeserializeObject<List<PackArticle>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["PackArticles"] = ids;

			// NOTE: PackArticles are considered as part of the Packs (as if they were properties of the pack), 
			// and are to be completely overwritten when the project is imported. Hence, we first delete all
			// PackArticles, then insert the ones that are being imported.
			importInfo.ctx.Database.ExecuteSqlCommand($@"
				delete from PackArticles where ID in
					(select a.ID from 
						PackArticles a 
						join Packs b on a.PackID = b.ID
						where b.ProjectID = {importInfo.project.ID})
			");


			foreach (var packArticle in packArticles)
			{
				var match = new PackArticle();

				// Copy all properties save for ID, PackID, ArticleID and CatalogID fields
				Reflex.Copy(match, packArticle, new string[] { "ID", "PackID", "ArticleID", "CatalogID" });

				match.PackID = importInfo.referenceMap["Packs"][packArticle.PackID];

				if (packArticle.ArticleID.HasValue)
					match.ArticleID = importInfo.referenceMap["Articles"][packArticle.ArticleID.Value];
				else
					match.ArticleID = null;

				if (packArticle.CatalogID.HasValue)
					match.CatalogID = importInfo.referenceMap["Catalogs"][packArticle.CatalogID.Value];
				else
					match.CatalogID = null;

				importInfo.ctx.PackArticles.Add(match);
				importInfo.ctx.SaveChanges();

				ids.Add(packArticle.ID, match.ID);
			}
		}


		private void ImportMappings(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Mappings"))
				return;

			var json = importInfo.data["Mappings"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<IMappingRepository>();
			var mappings = JsonConvert.DeserializeObject<List<DataImportMapping>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Mappings"] = ids;

			var existing = repo.GetByProjectID(importInfo.project.ID);

			foreach (var mapping in mappings)
			{
				var match = existing.FirstOrDefault(l => l.Name == mapping.Name);
				if (match == null)
				{
					match = new DataImportMapping();
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID & ProjectID fields
				Reflex.Copy(match, mapping, new string[] { "ID", "ProjectID", "RootCatalog" });

                int foundCatalogID = 0;

                importInfo.referenceMap["Catalogs"].TryGetValue(mapping.RootCatalog, out foundCatalogID);

                match.RootCatalog = foundCatalogID;

                if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(mapping.ID, match.ID);
			}
		}


		private void ImportMappingsCols(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("MappingsCols"))
				return;

			var json = importInfo.data["MappingsCols"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var cols = JsonConvert.DeserializeObject<List<DataImportColMapping>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["MappingsCols"] = ids;

			// NOTE: DataImportColMappings are considered as part of the mappings (as if they were properties of them), 
			// and are to be completely overwritten when the project is imported. Hence, we first delete all
			// DataImportColMappings, then insert the ones that are being imported.
			importInfo.ctx.Database.ExecuteSqlCommand($@"
				delete from DataImportColMapping where ID in
					(select a.ID from 
						DataImportColMapping a 
						join DataImportMappings b on a.DataImportMappingID = b.ID
						where b.ProjectID = {importInfo.project.ID})
			");


			foreach (var col in cols)
			{
				var match = new DataImportColMapping();

				// Copy all properties save for ID & DataImportMappingID fields
				Reflex.Copy(match, col, new string[] { "ID", "DataImportMappingID" });

				match.DataImportMappingID = importInfo.referenceMap["Mappings"][col.DataImportMappingID.Value];
				importInfo.ctx.DataImportColMapping.Add(match);
				importInfo.ctx.SaveChanges();
				ids.Add(col.ID, match.ID);
			}
		}


		private void ImportArticlePreviewSettings(ProjectImportInfo importInfo)
        {
			if (!importInfo.data.ContainsKey("ArticlePreviewSettings"))
				return;

			var json = importInfo.data["ArticlePreviewSettings"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var settings = JsonConvert.DeserializeObject<List<ArticlePreviewSettings>>(json);

			var existing = importInfo.ctx.ArticlePreviewSettings.FromSql($@"
				select a.* from ArticlePreviewSettings a
					join Articles b on a.ArticleID = b.ID
					where b.ProjectID = {importInfo.project.ID}").ToList();

			foreach (var setting in settings)
			{
				var match = existing.FirstOrDefault(l => l.ArticleID == importInfo.referenceMap["Articles"][setting.ArticleID]);
				if (match == null)
				{
					match = new ArticlePreviewSettings();
					match.ArticleID = importInfo.referenceMap["Articles"][setting.ArticleID];
				}

				// Copy all properties save for ID & ArticleID fields
				Reflex.Copy(match, setting, new string[] { "ID", "ArticleID" });

				if (match.ID == 0)
					importInfo.ctx.ArticlePreviewSettings.Add(match);
				else
					importInfo.ctx.ArticlePreviewSettings.Update(match);
			}
			importInfo.ctx.SaveChanges();
        }


		private void ImportComparerConfiguration(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("ComparerConfiguration"))
				return;

			var json = importInfo.data["ComparerConfiguration"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var settings = JsonConvert.DeserializeObject<List<ComparerConfiguration>>(json);
			if (settings.Count > 0)
			{
				var match = importInfo.ctx.ComparerConfiguration.FromSql($@"
					select * from ComparerConfiguration
					where ProjectID = {importInfo.project.ID}
				").FirstOrDefault();

				if (match == null)
				{
					match = new ComparerConfiguration();
					match.CompanyID = importInfo.company.ID;
					match.BrandID = importInfo.brand.ID;
					match.ProjectID = importInfo.project.ID;
				}

				Reflex.Copy(match, settings[0], new string[] { "ID", "CompanyID", "BrandID", "ProjectID" });

				if (match.ID == 0)
					importInfo.ctx.ComparerConfiguration.Add(match);
				else
					importInfo.ctx.ComparerConfiguration.Update(match);

				importInfo.ctx.SaveChanges();
			}
		}


		private void ImportWizardCustomSteps(ProjectImportInfo importInfo)
        {
			if (!importInfo.data.ContainsKey("WizardCustomSteps"))
				return;

			var json = importInfo.data["WizardCustomSteps"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var steps = JsonConvert.DeserializeObject<List<WizardCustomStep>>(json);

			var existing = importInfo.ctx.WizardCustomSteps.FromSql($@"
					select * from WizardCustomSteps
					where ProjectID = {importInfo.project.ID}
				").ToList();

			foreach (var step in steps)
			{
				var match = existing.FirstOrDefault(p => p.Name == step.Name);
				if (match == null)
				{
					match = new WizardCustomStep();
					match.CompanyID = importInfo.company.ID;
					match.BrandID = importInfo.brand.ID;
					match.ProjectID = importInfo.project.ID;
				}

				// Copy all properties save for ID, CompanyID, BrandID and ProjectID fields
				Reflex.Copy(match, step, new string[] { "ID", "CompanyID", "BrandID", "ProjectID" });

				if (match.ID == 0)
					importInfo.ctx.WizardCustomSteps.Add(match);
				else
					importInfo.ctx.WizardCustomSteps.Update(match);

			}
			importInfo.ctx.SaveChanges();
        }


		private void ImportCompanyProviderCompanies(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Companies"))
				return;

			var json = importInfo.data["Companies"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var repo = factory.GetInstance<ICompanyRepository>();
			var companies = JsonConvert.DeserializeObject<List<Company>>(json);
			var ids = new Dictionary<int, int>();
			importInfo.referenceMap["Companies"] = ids;

			var existing = repo.GetAll();

			foreach (var company in companies)
			{
				var match = existing.FirstOrDefault(l => l.Name == company.Name);
				if (match == null)
				{
					match = new Company();
					match.ShowAsCompany = false;
				}

				// Copy all properties save for ID, RFIDConfigID and ShowAsCompany fields
				Reflex.Copy(match, company, new string[] { "ID", "RFIDConfigID", "ShowAsCompany" });

				if (match.ID == 0)
					match = repo.Insert(match);
				else
					repo.Update(match);

				ids.Add(company.ID, match.ID);
			}
		}

        private void ImportInlays(ProjectImportInfo importInfo)
        {
            if (!importInfo.data.ContainsKey("Inlays"))
                return;

            var json = importInfo.data["Inlays"];
            if (String.IsNullOrWhiteSpace(json))
                return;

            var inlays = JsonConvert.DeserializeObject<List<InLay>>(json);
            var existing = importInfo.ctx.InLays.FromSql($@"
					select * from Inlays
				").ToList();
            foreach (var inlay in inlays)
            {
                var match = existing.FirstOrDefault(p => p.ChipName.ToLower() == inlay.ChipName.ToLower() && p.ProviderName.ToLower() == inlay.ProviderName.ToLower());
                if (match == null)
                    importInfo.ctx.InLays.Add(inlay);
                else
                {
                    //if(match.ID != inlay.ID && inlay.Equals(match))
                    //{
                    //    importInfo.ctx.InLays.Update(inlay);
                    //}
                    importInfo.ctx.InLays.Update(inlay);
                }
            }
            importInfo.ctx.SaveChanges();
        }

        private void ImportInlayConfig(ProjectImportInfo importInfo)
        {
            if (!importInfo.data.ContainsKey("InlayConfigs"))
                return;

            var json = importInfo.data["InlayConfigs"];
            if (String.IsNullOrWhiteSpace(json))
                return;

            var inlayConfigs = JsonConvert.DeserializeObject<List<InlayConfig>>(json);
            var existing = importInfo.ctx.InlayConfigs.FromSql($@"
					select * from InlayConfigs
				").ToList();
            foreach (var inlayConfig in inlayConfigs)
            {
                var match = existing.FirstOrDefault(x => x.Description.ToLower() == inlayConfig.Description.ToLower() && x.CompanyID == inlayConfig.CompanyID
                                            && x.ProjectID == inlayConfig.ProjectID && x.BrandID == inlayConfig.BrandID && x.InlayID == inlayConfig.InlayID);
                if (match == null)
                    importInfo.ctx.InlayConfigs.Add(inlayConfig);
                else
                    importInfo.ctx.InlayConfigs.Update(inlayConfig);
            }
            importInfo.ctx.SaveChanges();
        }

		private void ImportCompanyProviders(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("CompanyProviders"))
				return;

			var json = importInfo.data["CompanyProviders"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var providers = JsonConvert.DeserializeObject<List<CompanyProvider>>(json);
			var existing = importInfo.ctx.CompanyProviders.Where(p => p.CompanyID == importInfo.company.ID).ToList();

			foreach (var provider in providers)
			{
                var match = existing
                    .Where(p => p.ProviderCompanyID == importInfo.referenceMap["Companies"][provider.ProviderCompanyID])
                    .Where(p => p.ClientReference == provider.ClientReference)
                    .FirstOrDefault();

				if (match == null)
				{
					match = new CompanyProvider();
					match.CompanyID = importInfo.company.ID;
					match.ProviderCompanyID = importInfo.referenceMap["Companies"][provider.ProviderCompanyID];
				}

				// Copy all properties save for ID, CompanyID  and ProviderCompanyID fields
				Reflex.Copy(match, provider, new string[] { "ID", "CompanyID", "ProviderCompanyID" });

				if (match.ID == 0)
					importInfo.ctx.CompanyProviders.Add(match);
				else
					importInfo.ctx.CompanyProviders.Update(match);
                importInfo.ctx.SaveChanges();

            }
            //importInfo.ctx.SaveChanges();
        }


		private void ImportFiles(ProjectImportInfo importInfo)
		{
			if (!projectStore.TryGetFile(importInfo.project.ID, out var container))
				container = projectStore.GetOrCreateFile(importInfo.project.ID, Project.FILE_CONTAINER_NAME);

			ImportLabelFiles(container, importInfo);
			ImportPreviewFiles(container, importInfo);
			ImportImageFiles(container, importInfo);
			ImportArticlePreviewFiles(importInfo);
		}


		private void ImportLabelFiles(IFileData container, ProjectImportInfo importInfo)
		{
			var labels = container.GetAttachmentCategory("Labels");
			var labelEntries = importInfo.archive.Entries.Where(x => x.FullName.StartsWith("Labels/") && !x.FullName.Contains("_meta.dat"));

			foreach (var entry in labelEntries)
			{
				using (var src = entry.Open())
				{
					var label = labels.GetOrCreateAttachment(entry.Name);
					label.SetContent(src);
				}
			}
		}


		private void ImportPreviewFiles(IFileData container, ProjectImportInfo importInfo)
		{
			var previews = container.GetAttachmentCategory("Previews");
			var previewEntries = importInfo.archive.Entries.Where(x => x.FullName.StartsWith("Previews/") && !x.FullName.Contains("_meta.dat"));

			foreach (var entry in previewEntries)
			{
				using (var src = entry.Open())
				{
					var preview = previews.GetOrCreateAttachment(entry.Name);
					preview.SetContent(src);
				}
			}
		}


		private void ImportImageFiles(IFileData container, ProjectImportInfo importInfo)
		{
			var previews = container.GetAttachmentCategory("Images");
			var imageEntries = importInfo.archive.Entries.Where(x => x.FullName.StartsWith("Images/") && !x.FullName.Contains("_meta.dat"));

			var metadata = GetImageMetadata(importInfo.archive);
            var userData = factory.GetInstance<IUserData>();

            foreach (var entry in imageEntries)
			{
				using (var src = entry.Open())
				{

                    if (entry.Name.Contains("ImageMetadata.json")) continue;// ignore this file

					var image = previews.GetOrCreateAttachment(entry.Name);
					image.SetContent(src);

					//Add Image Metadata
					var imageMeta = metadata.FirstOrDefault(p => p != null && String.Compare(p.FileName, entry.Name, true) == 0);
					if (imageMeta != null)
					{
                        imageMeta.ProjectID = importInfo.project.ID;
                        imageMeta.UpdateDate = DateTime.Now;
                        imageMeta.UpdateUser = userData.UserName;

                        image.SetMetadata(imageMeta);
					}
				}
			}
		}


		private List<ImageMetadata> GetImageMetadata(ZipArchive archive)
		{
            var fileJSON = "Images/ImageMetadata.json";
			var entry = archive.GetEntry(fileJSON);

            if (entry == null) return new List<ImageMetadata>();

			using (var s = entry.Open())
			{
				var json = s.ReadAllText(Encoding.UTF8);
				json = json.Trim(new char[] { '\uFEFF', '\u200B' });
				return JsonConvert.DeserializeObject<List<ImageMetadata>>(json);
			}
		}


		private void ImportArticlePreviewFiles(ProjectImportInfo importInfo)
		{
			if (!importInfo.data.ContainsKey("Articles"))
				return;

			var json = importInfo.data["Articles"];
			if (String.IsNullOrWhiteSpace(json))
				return;

			var articles = JsonConvert.DeserializeObject<List<Article>>(json);
			var previewEntries = importInfo.archive.Entries.Where(x => x.FullName.StartsWith("ArticlePreviews/"));

			foreach (var article in articles)
			{
				var entry = previewEntries.FirstOrDefault(e => String.Compare(e.FullName, $"ArticlePreviews/{article.ID}/preview.png", true) == 0);
				if (entry != null)
				{
					var articleId = importInfo.referenceMap["Articles"][article.ID];

					if (!articlePreviewStore.TryGetFile(articleId, out var articlePreview))
						articlePreview = articlePreviewStore.GetOrCreateFile(articleId, "preview.png");
					using (var src = entry.Open())
						articlePreview.SetContent(src);
				}
			}
		}
	}


	class ProjectImportInfo
	{
		public PrintDB ctx;
		public ICompany company;
		public IBrand brand;
		public IProject project;
		public ZipArchive archive;
		public Dictionary<string, string> data = new Dictionary<string, string>();
		public Dictionary<string, Dictionary<int, int>> referenceMap = new Dictionary<string, Dictionary<int, int>>();
	}
}
