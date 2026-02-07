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

namespace WebLink.Contracts.Models
{
    public partial class ProjectRepository : GenericRepository<IProject, Project>, IProjectRepository
	{

		public T GetEntityList<T>(string value)
		{
			return JsonConvert.DeserializeObject<T>(value);
		}

		#region ExportProcess

		public Stream ExportProject(int id, out string fileName)
		{
			var temp = factory.GetInstance<ITempFileService>();
			var project = GetByID(id);
			var path = temp.GetTempDirectory();
			fileName = "Project-" + project.Name + "-" + id + ".zip";
			var zipFile = path + "\\" + fileName;
			return CreateProjectZipFile(id, zipFile);
		}


        private Stream CreateProjectZipFile(int id, string zipFile)
        {
            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    using (var conn = connManager.OpenWebLinkDB())
                    {
                        AddData(conn, archive, id);
                        AddFiles(conn, archive, id);
                    }
                }
            }

            return File.Open(zipFile, FileMode.Open, FileAccess.Read);
        }

        private void AddData(IDBX conn, ZipArchive archive, int id)
        {
            var Data = GetProjectData(id);

            foreach (var item in Data)
            {
                var entryName = $"Data/{item.Key}";
                var entry = archive.Entries.FirstOrDefault(x => x.Name.Equals(entryName));
                if (entry != null)
                {
                    entry.Delete();
                }

                ZipArchiveEntry readmeEntry = archive.CreateEntry(entryName);
                using (StreamWriter writer = new StreamWriter(readmeEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(item.Value);
                }
            }
        }

        private Dictionary<string, string> GetProjectData(int id)
        {
            var data = new Dictionary<string, string>();

            using (IDBX conn = connManager.OpenWebLinkDB())
            {
                //Add Current Project
                GetProjectData(data, conn, id);

                //Add Materials
                GetCommonTableData(data, conn, id, "Materials");

                //Add Categories
                GetData(data, conn, id, "Categories");

                //Add Labels
                GetData(data, conn, id, "Labels");

                //Add Articles
                GetData(data, conn, id, "Articles");

                //Add Artifacts
                GetArtifacts(data, conn, id);

                //Add Packs
                GetData(data, conn, id, "Packs");

                //Add PackArticles
                GetPackArticles(data, conn, id);

                //Add ArticlePreviewSettings
                GetArticlePreviewSettings(data, conn, id);

                //Add ComparerConfiguration
                GetData(data, conn, id, "ComparerConfiguration");

                //Add RFIDParameters
                GetRFIDConfig(data, conn, id, "RFIDParameters");

                //Add WizardCustomSteps
                GetData(data, conn, id, "WizardCustomSteps");

                //Add Catalogs
                GetData(data, conn, id, "Catalogs");
                SortCatalogs(data);

                //Add Catalogs Data
                GetCatalogData(data, "Catalogs");

                //Add DataImportMapping
                GetData(data, conn, id, "DataImportMappings");

                //Add DataImportColMapping
                var jsonObject = data.FirstOrDefault(x => x.Key.Equals("DataImportMappings"));
                var dataList = JsonConvert.DeserializeObject<List<DataImportColMapping>>(jsonObject.Value).Select(x => x.ID).ToList();

                if (dataList.Count > 0)
                {
                    GetDataImportColMapping(data, conn, "DataImportColMapping", dataList);
                }
            }

            return data;
        }

        private void GetCommonTableData(Dictionary<string, string> data, IDBX conn, int id, string entity)
        {
            data.Add(entity, JsonConvert.SerializeObject(conn.SelectToJson($@"
                select * from {entity};",
            id)));
        }

        private void GetArtifacts(Dictionary<string, string> data, IDBX conn, int id)
        {
            data.Add("Artifacts", JsonConvert.SerializeObject(conn.SelectToJson($@"
                select distinct art.* from Artifacts art 
                join Articles a on a.ID = art.ArticleID
                where a.ProjectID = @id;",
            id)));
        }

        private void GetPackArticles(Dictionary<string, string> data, IDBX conn, int id)
        {
            data.Add("PackArticles", JsonConvert.SerializeObject(conn.SelectToJson($@"
                select distinct pa.* from PackArticles pa 
                join Packs p on p.ID = pa.PackID
                where p.ProjectID = @id;",
            id)));
        }

        private void GetArticlePreviewSettings(Dictionary<string, string> data, IDBX conn, int id)
        {
            data.Add("ArticlePreviewSettings", JsonConvert.SerializeObject(conn.SelectToJson($@"
                select distinct ap.* from ArticlePreviewSettings ap
                join Articles a on a.ID = ap.ArticleID
                where a.ProjectID = @id;",
            id)));
        }

        private void GetRFIDConfig(Dictionary<string, string> data, IDBX conn, int id, string entity)
        {
            var project = GetByID(id);
            data.Add(entity, JsonConvert.SerializeObject(conn.SelectToJson($@"
                select * from {entity} 
                where id = @id;",
            project.RFIDConfigID)));
        }

        private void GetData(Dictionary<string, string> data, IDBX conn, int id, string entity)
        {
            data.Add(entity, JsonConvert.SerializeObject(conn.SelectToJson($@"
                select * from {entity} 
                where ProjectID = @id;",
            id)));
        }

        private void GetProjectData(Dictionary<string, string> data, IDBX conn, int id)
        {
            data.Add("Project", JsonConvert.SerializeObject(conn.SelectToJson($@"
                select * from Projects 
                where ID = @id;",
            id)));
        }

        private void SortCatalogs(Dictionary<string, string> data)
        {
            var jsonCatalogs = data.FirstOrDefault(x => x.Key.Equals("Catalogs"));
            var catalogs = JsonConvert.DeserializeObject<List<Catalog>>(jsonCatalogs.Value).ToList();
            var sortedCatalogs = new List<Catalog>();

            List<int> catalogList = new List<int>();
            List<int> parentIds = new List<int>();

            foreach (var catalog in catalogs)
            {
                if (!parentIds.Contains(catalog.CatalogID))
                {
                    parentIds.Add(catalog.CatalogID);
                    SetCatalogOrder(catalogList, catalog, catalogs, parentIds);
                }
            }

            foreach (var id in catalogList)
            {
                sortedCatalogs.Add(catalogs.FirstOrDefault(x => x.CatalogID.Equals(id)));
            }

            data["Catalogs"] = JsonConvert.SerializeObject(sortedCatalogs);
        }

        public void SetCatalogOrder(List<int> catalogList, Catalog catalog, List<Catalog> catalogs, List<int> parentIds)
        {
            var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(catalog.Definition);
            foreach (var field in fields.Where(x => x.CatalogID != null && x.CatalogID > 0 && (x.Type == ColumnType.Reference || x.Type == ColumnType.Set) && !parentIds.Contains(x.CatalogID.Value)))
            {
                if (!catalogList.Contains(field.CatalogID.Value))
                {
                    parentIds.Add(field.CatalogID.Value);
                    var currentCatalog = catalogs.FirstOrDefault(x => x.CatalogID.Equals(field.CatalogID.Value) && (field.Type == ColumnType.Reference || field.Type == ColumnType.Set));
                    if (currentCatalog != null)
                    {
                        SetCatalogOrder(catalogList, currentCatalog, catalogs, parentIds);
                    }
                }
            }

            catalogList.Add(catalog.CatalogID);
        }

        private void GetCatalogData(Dictionary<string, string> data, string entity)
        {
			var catalogDataRepo = factory.GetInstance<ICatalogDataRepository>();
            var jsonCatalogs = data.FirstOrDefault(x => x.Key.Equals(entity));
            var catalogs = JsonConvert.DeserializeObject<List<Catalog>>(jsonCatalogs.Value).ToList();

            var lookUps = catalogs.Where(x => x.CatalogType == CatalogType.Lookup).Select(x => x.Name + "_" + x.CatalogID).ToList();
            foreach (var catalog in lookUps)
            {
                string[] values = catalog.Split('_');
                var catalogData = catalogDataRepo.GetList(int.Parse(values.Last()));
                var name = string.Join("_", values).Replace("_" + values.Last(), "");
                data.Add(name + "_Data", catalogData);
            }
        }

        private void GetDataImportColMapping(Dictionary<string, string> data, IDBX conn, string entity, List<int> ids)
        {
            var filter = "(" + string.Join(",", ids.Select(n => n.ToString()).ToArray()) + ")";
            data.Add(entity, JsonConvert.SerializeObject(conn.SelectToJson($@"
                select * from {entity} 
                where DataImportMappingID in {filter};",
            filter)));
        }

        private void AddFiles(IDBX conn, ZipArchive archive, int id)
        {
            var labels = conn.Select<LabelData>(@"
				select * from Labels 
                where ProjectID = @id", id);

            if (!projectStore.TryGetFile(id, out var container))
                container = projectStore.GetOrCreateFile(id, "ProjectContainer");

            //Add Project Label files
            foreach (var label in labels)
            {
                var labelsCategory = container.GetAttachmentCategory("Labels");
                if (String.IsNullOrWhiteSpace(label.FileName)) continue;
                if (labelsCategory.TryGetAttachment(label.FileName, out var labelFile))
                {
                    using (var src = labelFile.GetContentAsStream())
                    {
                        var entry = archive.CreateEntry($"Labels/{label.FileName}");
                        using (Stream dst = entry.Open())
                        {
                            src.CopyTo(dst, 4096);
                        }
                    }
                }
            }

            //Add Project Label previews
            foreach (var label in labels)
            {
                var previewsCategory = container.GetAttachmentCategory("Previews");
                var previewFileName = Path.GetFileNameWithoutExtension(label.FileName) + "-preview.png";
                if (previewsCategory.TryGetAttachment(previewFileName, out var previewFile))
                {
                    using (var src = previewFile.GetContentAsStream())
                    {
                        var entry = archive.CreateEntry($"Previews/{previewFile.FileName}");
                        using (Stream dst = entry.Open())
                        {
                            src.CopyTo(dst, 4096);
                        }
                    }
                }
            }

			//Add project images
			var metadata = new List<ImageMetadata>();
            var imagesCategory = container.GetAttachmentCategory("Images");
            foreach (var image in imagesCategory)
            {
                using (var src = image.GetContentAsStream())
                {
                    var entry = archive.CreateEntry($"Images/{image.FileName}");
                    using (Stream dst = entry.Open())
                    {
                        src.CopyTo(dst, 4096);
                    }
                }

				metadata.Add(image.GetMetadata<ImageMetadata>());
			}

			//Add Image Metadata
			var meta = JsonConvert.SerializeObject(metadata);
			var readmeEntry = archive.CreateEntry($"ImageMetadata.json");
			using (StreamWriter writer = new StreamWriter(readmeEntry.Open(), Encoding.UTF8))
			{
				writer.Write(meta);
			}

			//Add Project Article previews            
			var articles = conn.Select<Article>(@"
				select * from Articles 
                where ProjectID = @id", id);
            foreach (var art in articles)
            {
                if (articlePreviewStore.TryGetFile(art.ID, out var preview))
                {
                    var entry = archive.CreateEntry($"ArticlePreviews/{art.ID}/preview.png");
                    using (var dst = entry.Open())
                    {
                        using (var src = preview.GetContentAsStream())
                        {
                            src.CopyTo(dst, 4096);
                        }
                    }
                }
            }
        }

        #endregion

        #region UploadProcess

        public bool UploadProject(int brandId, string fileName, Stream content)
        {
            var data = new Dictionary<string, string>();
            var commonData = new Dictionary<string, Dictionary<int, int>>();
            var newProjectId = 0;
            using (var archive = new ZipArchive(content, ZipArchiveMode.Read))
            {
                //Upload Data Process
                var entries = archive.Entries.Where(x => x.FullName.Contains("Data/"));
                var value = Regex.Match(fileName, @"-\d+.zip$").Value;
                var id = int.Parse(Regex.Match(fileName, @"\d+").Value);

                if (entries.Count() > 0)
                {
                    foreach (var entry in entries)
                    {
                        var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                        data.Add(entry.Name.Split(".").FirstOrDefault(), reader.ReadToEnd());
                    }

                    newProjectId = UploadDataProcess(brandId, id, data, commonData);
                }

                //Upload File Process
                var files = archive.Entries.Where(x => !x.FullName.Contains("Data/")).ToList();

                if (files.Count() > 0 && newProjectId != 0)
                {
                    UploadFilesProcess(archive, files, newProjectId, id, commonData, data);
                }

                return true;
            }
        }

        #region UploadData

        private int UploadDataProcess(int brandId, int id, Dictionary<string, string> data, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals("Project"));
            var project = GetEntityList<List<Project>>(jsonObject.Value).FirstOrDefault();

            var newProject = new Project();
            newProject = project;
            newProject.ID = 0;
            newProject.BrandID = brandId;
            newProject.Name = "New Project";
            newProject.EnableFTPFolder = false;
            newProject.FTPFolder = null;
            newProject.FTPClients = null;
            var newProjectId = Insert(project).ID;

            //Upload Materials Data
            UploadMaterialData(data, "Materials", newProjectId, commonData);

            //Upload Categories Data
            UploadCategoryData(data, "Categories", newProjectId, commonData);

            //Upload Labels Data
            UploadLabelData(data, "Labels", newProjectId, commonData);

            //Upload Articles Data
            UploadArticleData(data, "Articles", newProjectId, commonData);

            //Upload Artifacts Data
            UploadArtifactData(data, "Artifacts", newProjectId, commonData);

            //Upload Packs Data
            UploadPackData(data, "Packs", newProjectId, commonData);

            //Upload ArticlePreviewSettings Data
            UploadArticlePreviewSettingsData(data, "ArticlePreviewSettings", newProjectId, commonData);

            //Upload RFIDParameters Data
            UploadRFIDParametersData(data, "RFIDParameters", newProjectId);

            //Upload ComparerConfiguration Data
            UploadComparerConfigurationData(data, "ComparerConfiguration", newProjectId);

            //Upload WizardCustomSteps Data
            UploadWizardCustomStepsData(data, "WizardCustomSteps", newProjectId);

            //Add Catalogs Data
            UploadCatalogData(data, "Catalogs", newProjectId, commonData);

            //Upload PackArticles Data
            UploadPackArticleData(data, "PackArticles", newProjectId, commonData);

            //Add DataImportMappings Data
            UploadMappingData(data, "DataImportMappings", newProjectId);

            //Copy Files
            //AddFiles(id, newProjectId);

            return newProjectId;
        }

        //Upload Materials Data
        public void UploadMaterialData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var materialRepo = factory.GetInstance<IMaterialRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var materials = GetEntityList<List<Material>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var material in materials)
                {
                    var currentMaterial = materialRepo.GetList().FirstOrDefault(x => x.Name.Equals(material.Name));
                    var oldId = material.ID;
                    var newMaterial = new Material();
                    newMaterial = material;
                    newMaterial.ID = 0;
                    var newMaterialId = currentMaterial == null ? materialRepo.Insert(newMaterial).ID : currentMaterial.ID;
                    commonData[entity].Add(oldId, newMaterialId);
                }
            }
        }

        //Upload Categories Data
        public void UploadCategoryData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var categoryRepo = factory.GetInstance<ICategoryRepository>();
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var categories = GetEntityList<List<Category>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var category in categories)
                {
                    var oldId = category.ID;
                    var newCategory = new Category();
                    newCategory = category;
                    newCategory.ID = 0;
                    newCategory.ProjectID = projectId;
                    var newCategoryId = categoryRepo.Insert(newCategory).ID;
                    commonData[entity].Add(oldId, newCategoryId);
                }
            }
        }

        //Upload Labels Data
        public void UploadLabelData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var labelRepo = factory.GetInstance<ILabelRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var labels = GetEntityList<List<LabelData>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var label in labels)
                {
                    var oldId = label.ID;
                    var newLabel = new LabelData();
                    newLabel = label;
                    newLabel.ID = 0;
                    newLabel.ProjectID = projectId;
                    newLabel.MaterialID = label.MaterialID != null ? commonData["Materials"].FirstOrDefault(x => x.Key.Equals(label.MaterialID)).Value : (int?)null;
                    var newLabelId = labelRepo.Insert(newLabel).ID;
                    commonData[entity].Add(oldId, newLabelId);
                }
            }
        }

        //Upload Articles Data
        public void UploadArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var articleRepo = factory.GetInstance<IArticleRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var articles = GetEntityList<List<Article>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var article in articles)
                {
                    var oldLabelId = article.LabelID;
                    var oldCategoryId = article.CategoryID;
                    var oldId = article.ID;
                    var newArticle = new Article();
                    newArticle = article;
                    newArticle.ID = 0;
                    newArticle.ProjectID = projectId;
                    newArticle.LabelID = oldLabelId != null ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(oldLabelId)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(oldLabelId)).Value : (int?)null : null;
                    newArticle.CategoryID = oldCategoryId != null ? commonData["Categories"].FirstOrDefault(x => x.Key.Equals(oldCategoryId)).Value : (int?)null;
                    var newArticleId = articleRepo.Insert(newArticle).ID;
                    commonData[entity].Add(oldId, newArticleId);
                }
            }
        }

        //Upload Artifacts Data
        public void UploadArtifactData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
			var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
			if (!string.IsNullOrEmpty(jsonObject.Value))
			{
				var artifacts = GetEntityList<List<Artifact>>(jsonObject.Value);
				commonData.Add(entity, new Dictionary<int, int>());
				var artifactList = new List<Artifact>();

				foreach (var artifact in artifacts)
				{
					var newArtifact = new Artifact();
					newArtifact = artifact;
					newArtifact.ID = 0;
					newArtifact.LabelID = artifact.LabelID != null ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(artifact.LabelID)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(artifact.LabelID)).Value : (int?)null : null;
					newArtifact.ArticleID = artifact.ArticleID != null ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(artifact.ArticleID)).Value : (int?)null;
					artifactList.Add(newArtifact);
				}

				using (var ctx = factory.GetInstance<PrintDB>())
				{
					ctx.Artifacts.AddRange(artifactList);
					ctx.SaveChanges();
				}
			}
        }


        //Upload Packs Data
        public void UploadPackData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var packRepo = factory.GetInstance<IPackRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var packs = GetEntityList<List<Pack>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var pack in packs)
                {
                    var oldId = pack.ID;
                    var newPack = new Pack();
                    newPack = pack;
                    newPack.ID = 0;
                    newPack.ProjectID = projectId;
                    var newPackId = packRepo.Insert(newPack).ID;
                    commonData[entity].Add(oldId, newPackId);
                }
            }
        }


		//Upload PackArticles Data
		public void UploadPackArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		{
			var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
			if (!string.IsNullOrEmpty(jsonObject.Value))
			{
				var packArticles = GetEntityList<List<PackArticle>>(jsonObject.Value);
				var packArticleList = new List<PackArticle>();

				foreach (var packArticle in packArticles)
				{
					var newPackArticle = new PackArticle();
					newPackArticle = packArticle;
					newPackArticle.ID = 0;
					newPackArticle.PackID = newPackArticle.PackID != 0 ? commonData["Packs"].FirstOrDefault(x => x.Key.Equals(newPackArticle.PackID)).Value : 0;
					newPackArticle.ArticleID = newPackArticle.ArticleID != null ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(newPackArticle.ArticleID)).Value : (int?)null;
                    newPackArticle.CatalogID = newPackArticle.CatalogID != null ? commonData["Catalogs"].FirstOrDefault(x => x.Key.Equals(newPackArticle.CatalogID)).Value : (int?)null;

                    packArticleList.Add(newPackArticle);

                }

                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    ctx.PackArticles.AddRange(packArticleList);
                    ctx.SaveChanges();
                }
            }
		}

        //Upload ArticlePreviewSettings Data
        public void UploadArticlePreviewSettingsData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var previewSettings = GetEntityList<List<ArticlePreviewSettings>>(jsonObject.Value);
                var previewSettingsList = new List<ArticlePreviewSettings>();

                foreach (var previewSetting in previewSettings)
                {
                    var newPreview = new ArticlePreviewSettings();
                    newPreview = previewSetting;
                    newPreview.ID = 0;
                    newPreview.ArticleID = newPreview.ArticleID != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(newPreview.ArticleID)).Value : 0;
                    previewSettingsList.Add(newPreview);
                }

				using (var ctx = factory.GetInstance<PrintDB>())
				{
					ctx.ArticlePreviewSettings.AddRange(previewSettingsList);
					ctx.SaveChanges();
				}
            }
        }

        //Upload RFIDParameters Data
        public void UploadRFIDParametersData(Dictionary<string, string> data, string entity, int projectId)
        {
            var rfidRepo = factory.GetInstance<IRFIDConfigRepository>();
            var project = (Project)GetByID(projectId);

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var configs = GetEntityList<List<RFIDConfig>>(jsonObject.Value);

				using (var ctx = factory.GetInstance<PrintDB>())
				{
					foreach (var config in configs)
					{
						var newConfig = new RFIDConfig();
						newConfig = config;
						newConfig.ID = 0;
						project.RFIDConfigID = rfidRepo.Insert(newConfig).ID;
						ctx.Projects.Update(project);
					}

					ctx.SaveChanges();
				}
            }
        }

        //Upload ComparerConfiguration Data
        public void UploadComparerConfigurationData(Dictionary<string, string> data, string entity, int projectId)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
				var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
				var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
				if (!string.IsNullOrEmpty(jsonObject.Value))
				{
					var configs = GetEntityList<List<ComparerConfiguration>>(jsonObject.Value);

					foreach (var config in configs)
					{
						var newConfig = new ComparerConfiguration();
						newConfig = config;
						newConfig.ID = 0;
						newConfig.CompanyID = companyId;
						newConfig.BrandID = brandId;
						newConfig.ProjectID = projectId;
						ctx.ComparerConfiguration.Add(newConfig);
					}

					ctx.SaveChanges();
				}
			}
        }

        //Upload WizardCustomSteps Data
        public void UploadWizardCustomStepsData(Dictionary<string, string> data, string entity, int projectId)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
				var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
				var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
				if (!string.IsNullOrEmpty(jsonObject.Value))
				{
					var wizardSteps = GetEntityList<List<WizardCustomStep>>(jsonObject.Value);

					foreach (var wizardStep in wizardSteps)
					{
						var newWizardStep = new WizardCustomStep();
						newWizardStep = wizardStep;
						newWizardStep.ID = 0;
						newWizardStep.CompanyID = companyId;
						newWizardStep.BrandID = brandId;
						newWizardStep.ProjectID = projectId;
						ctx.WizardCustomSteps.Add(newWizardStep);
					}

					ctx.SaveChanges();
				}
			}
        }

        //Catalogs
        public void UploadCatalogData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var catalogs = GetEntityList<List<Catalog>>(jsonObject.Value);
                var idList = new Dictionary<int, int>();
                commonData.Add(entity, new Dictionary<int, int>());


                foreach (var catalog in catalogs)
                {
                    var oldId = catalog.CatalogID;
                    var oldKeyId = catalog.ID;
                    var newCatalog = new Catalog();
                    newCatalog = catalog;
                    newCatalog.ID = 0;
                    newCatalog.ProjectID = projectId;
                    //validate current catalog Ids
                    ProcessFields(newCatalog, idList);
                    var insertedCatalog = catalogRepo.Insert(newCatalog);
                    idList.Add(oldId, insertedCatalog.CatalogID);
                    if (newCatalog.CatalogType == CatalogType.Lookup)
                    {
                        AddDynamicData(data, insertedCatalog.CatalogID, catalog.Name);
                    }

                    commonData[entity].Add(oldKeyId, insertedCatalog.ID);

                }
            }
        }

        public void ProcessFields(Catalog catalog, Dictionary<int, int> dictionary)
        {
            var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(catalog.Definition);

            foreach (var field in fields)
            {
                if (field.CatalogID != null && field.CatalogID > 0)
                {
                    field.CatalogID = dictionary.GetValueOrDefault(field.CatalogID.Value);
                }
            }

            catalog.Definition = JsonConvert.SerializeObject(fields);
        }

        public void UploadMappingData(Dictionary<string, string> data, string entity, int projectId)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var mappings = GetEntityList<List<DataImportMapping>>(jsonObject.Value);
					var catalogs = GetEntityList<List<Catalog>>(data.FirstOrDefault(x => x.Key.Equals("Catalogs")).Value);

					foreach (var mapping in mappings)
					{
						var mappingId = mapping.ID;
						var newMapping = new DataImportMapping();
						newMapping = mapping;
						newMapping.ID = 0;
						newMapping.ProjectID = projectId;
						newMapping.RootCatalog = mapping.RootCatalog != 0 ? ctx.Catalogs.FirstOrDefault(x => x.ProjectID == projectId &&
									x.Name.Equals(catalogs.FirstOrDefault(y => y.ID == mapping.RootCatalog).Name)).ID : 0;
						ctx.DataImportMappings.Add(newMapping);
						ctx.SaveChanges();
						var newMappingId = newMapping.ID;

						AddColMappingData(data, "DataImportColMapping", mappingId, newMappingId);
					}
				}
            }
        }


        public void AddColMappingData(Dictionary<string, string> data, string entity, int oldMappingId, int newMappingId)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var colMappings = GetEntityList<List<DataImportColMapping>>(jsonObject.Value).Where(x => x.DataImportMappingID == oldMappingId).ToList();

					foreach (var cols in colMappings)
					{
						var newColMapping = new DataImportColMapping();
						newColMapping = cols;
						newColMapping.ID = 0;
						newColMapping.DataImportMappingID = newMappingId;
						ctx.DataImportColMapping.Add(newColMapping);
					}

					ctx.SaveChanges();
				}
            }
        }


        public void AddDynamicData(Dictionary<string, string> data, int catalogId, string catalogName)
        {
			var catalogDataRepo = factory.GetInstance<ICatalogDataRepository>();
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(catalogName + "_Data"));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var dataList = JsonConvert.DeserializeObject<List<JObject>>(jsonObject.Value);

                foreach (var catalogData in dataList)
                {
                    catalogDataRepo.Insert(catalogId, catalogData.ToString());
                }
            }
        }

        #endregion

        #region UploadFiles

        private void UploadFilesProcess(ZipArchive archive, List<ZipArchiveEntry> entries, int projectId, int id, Dictionary<string, Dictionary<int, int>> commonData, Dictionary<string, string> data)
        {
            if (!projectStore.TryGetFile(projectId, out var container))
                container = projectStore.GetOrCreateFile(projectId, "ProjectContainer");

            UploadLabels(container, entries);
            UploadPreviews(container, entries);
            UploadImages(archive, container, entries);
            UploadArticlePreviews(articlePreviewStore, entries, commonData, data);
        }

        private void UploadLabels(IFileData container, List<ZipArchiveEntry> entries)
        {
            var labels = container.GetAttachmentCategory("Labels");
            var labelEntries = entries.Where(x => x.FullName.StartsWith("Labels/") && !x.FullName.Contains("_meta.dat"));

            foreach (var entry in labelEntries)
            {
                using (var src = entry.Open())
                {
					var label = labels.GetOrCreateAttachment(entry.Name);
                    label.SetContent(src);
                }
            }
        }

        private void UploadPreviews(IFileData container, List<ZipArchiveEntry> entries)
        {
            var previews = container.GetAttachmentCategory("Previews");
            var previewEntries = entries.Where(x => x.FullName.StartsWith("Previews/") && !x.FullName.Contains("_meta.dat"));

            foreach (var entry in previewEntries)
            {
                using (var src = entry.Open())
                {
					var preview = previews.GetOrCreateAttachment(entry.Name);
                    preview.SetContent(src);
                }
            }
        }

        private void UploadImages(ZipArchive archive, IFileData container, List<ZipArchiveEntry> entries)
        {
            var previews = container.GetAttachmentCategory("Images");
            var imageEntries = entries.Where(x => x.FullName.StartsWith("Images/") && !x.FullName.Contains("_meta.dat"));

			var metadata = GetImageMetadata(archive);

            foreach (var entry in imageEntries)
            {
                using (var src = entry.Open())
                {
					var image = previews.GetOrCreateAttachment(entry.Name);
                    image.SetContent(src);

					//Add Image Metadata
					var imageMeta = metadata.FirstOrDefault(p => p != null && String.Compare(p.FileName, entry.Name, true) == 0);
                    if (imageMeta != null)
                    {
                        image.SetMetadata(imageMeta);
                    }
                }
            }
        }


		private List<ImageMetadata> GetImageMetadata(ZipArchive archive)
		{
			var entry = archive.GetEntry("ImageMetadata.json");
			using (var s = entry.Open())
			{
				var json = s.ReadAllText(Encoding.UTF8);
				json = json.Trim(new char[] { '\uFEFF', '\u200B' });
				return JsonConvert.DeserializeObject<List<ImageMetadata>>(json);
			}
		}


		private void UploadArticlePreviews(IFileStore articlePreviewStore, List<ZipArchiveEntry> entries, Dictionary<string, Dictionary<int, int>> commonData, Dictionary<string, string> data)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals("Articles"));
            var articles = GetEntityList<List<Article>>(jsonObject.Value);
            var previewEntries = entries.Where(x => x.FullName.StartsWith("ArticlePreviews/"));

            foreach (var article in articles)
            {
                var entry = previewEntries.FirstOrDefault(e => String.Compare(e.FullName, $"ArticlePreviews/{article.ID}/preview.png", true) == 0);
                if (entry != null)
                {
                    var articleId = commonData["Articles"].FirstOrDefault(x => x.Key.Equals(article.ID)).Value;

                    if (!articlePreviewStore.TryGetFile(articleId, out var articlePreview))
                        articlePreview = articlePreviewStore.GetOrCreateFile(articleId, "preview.png");
                    using (var src = entry.Open())
                        articlePreview.SetContent(src);
                }
            }
        }

        #endregion

        #endregion

        /*Copy Files from old project*/
        private void AddFiles(int oldProjectId, int newProjectId)
        {
            if (projectStore.TryGetFile(oldProjectId, out var file))
            {
                var projectFile = projectStore.GetOrCreateFile(newProjectId, file.FileName);
				using (var src = file.GetContentAsStream())
				{
					projectFile.SetContent(src);
				}

				foreach (var category in projectStore.Categories)
                {
                    foreach (var sourceAttachment in file.GetAttachmentCategory(category))
                    {
                        var targetAttachments = projectFile.GetAttachmentCategory(category);
                        var targetAttachment = targetAttachments.GetOrCreateAttachment(sourceAttachment.FileName);
						using (var src = sourceAttachment.GetContentAsStream())
						{
							targetAttachment.SetContent(src);
						}
                    }
                }
            }
        }


        #region Import

        public bool Import(int projectId, string fileName, Stream content)
        {
            var data = new Dictionary<string, string>();
            var commonData = new Dictionary<string, Dictionary<int, int>>();
            using (var archive = new ZipArchive(content, ZipArchiveMode.Read))
            {
                //Upload Data Process
                var entries = archive.Entries.Where(x => x.FullName.Contains("Data/"));
                var value = Regex.Match(fileName, @"-\d+.zip$").Value;
                var id = int.Parse(Regex.Match(fileName, @"\d+").Value);

                if (entries.Count() > 0)
                {
                    foreach (var entry in entries)
                    {
                        var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                        data.Add(entry.Name.Split(".").FirstOrDefault(), reader.ReadToEnd());
                    }

                    ImportProcess(projectId, id, data, commonData);
                }

                //Upload File Process
                var files = archive.Entries.Where(x => !x.FullName.Contains("Data/")).ToList();

                if (files.Count() > 0 && projectId != 0)
                {
                    UploadFilesProcess(archive, files, projectId, id, commonData, data);
                }

                return true;
            }
        }

        #region UploadData

        private int ImportProcess(int projectId, int id, Dictionary<string, string> data, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals("Project"));
            var project = GetEntityList<List<Project>>(jsonObject.Value).FirstOrDefault();

            //Import Categories Data
            ImportCategoryData(data, "Categories", projectId, commonData);

            //Import Labels Data
            ImportLabelData(data, "Labels", projectId, commonData);

            //Import Articles Data
            ImportArticleData(data, "Articles", projectId, commonData);

            //Import Artifacts Data
            ImportArtifactData(data, "Artifacts", projectId, commonData);

            //Import Packs Data
            ImportPackData(data, "Packs", projectId, commonData);

            //Import PackArticles Data
            ImportPackArticleData(data, "PackArticles", projectId, commonData);

            //Import ArticlePreviewSettings Data
            ImportArticlePreviewSettingsData(data, "ArticlePreviewSettings", projectId, commonData);

            //Import ComparerConfiguration Data
            ImportComparerConfigurationData(data, "ComparerConfiguration", projectId);

            //Upload WizardCustomSteps Data
            ImportWizardCustomStepsData(data, "WizardCustomSteps", projectId);

            //Add Catalogs Data
            ImportCatalogData(data, "Catalogs", projectId);

            //Add DataImportMappings Data
            ImportMappingData(data, "DataImportMappings", projectId);

            //Copy Files
            //AddFiles(id, newProjectId);

            return projectId;
        }



        //Import Categories Data
        public void ImportCategoryData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var categoryRepo = factory.GetInstance<ICategoryRepository>();
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var categories = GetEntityList<List<Category>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var currentCategory in categories)
                {
                    var savedCategory = categoryRepo.GetByProject(projectId).FirstOrDefault(x => x.Name.Equals(currentCategory.Name));

                    if (savedCategory == null)
                    {
                        var oldId = currentCategory.ID;
                        var newCategory = new Category();
                        newCategory = currentCategory;
                        newCategory.ID = 0;
                        newCategory.ProjectID = projectId;
                        var newCategoryId = categoryRepo.Insert(newCategory).ID;
                        commonData[entity].Add(oldId, newCategoryId);
                    }
                    else
                        commonData[entity].Add(currentCategory.ID, savedCategory.ID);
                }
            }
        }

        
        //Import Labels Data
        public void ImportLabelData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var labelRepo = factory.GetInstance<ILabelRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var labels = GetEntityList<List<LabelData>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var currentLabel in labels)
                {
                    var savedLabel = labelRepo.GetByProjectID(projectId).FirstOrDefault(x => x.Name.Equals(currentLabel.Name));

                    var label = new LabelData();
                    label = currentLabel;
                    label.ProjectID = projectId;

                    if (savedLabel == null)
                    {
                        var oldId = currentLabel.ID;
                        label.ID = 0;
                        var newLabelId = labelRepo.Insert(label).ID;
                        commonData[entity].Add(oldId, newLabelId);
                    }
                    else
                    {
                        commonData[entity].Add(currentLabel.ID, savedLabel.ID);
                        label.ID = savedLabel.ID;
                        labelRepo.Update(label);
                    }
                }
            }
        }
        

        //Import Articles Data
        public void ImportArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var articleRepo = factory.GetInstance<IArticleRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var articles = GetEntityList<List<Article>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var currentArticle in articles)
                {
                    var savedArticle = articleRepo.GetByProjectID(projectId).FirstOrDefault(x => x.ArticleCode.Equals(currentArticle.ArticleCode));

                    var article = new Article();
                    article = currentArticle;
                    article.ProjectID = projectId;
                    article.LabelID = commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArticle.LabelID)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArticle.LabelID)).Value : (int?)null;
                    article.CategoryID = commonData["Categories"].FirstOrDefault(x => x.Key.Equals(currentArticle.CategoryID)).Value != 0 ? commonData["Categories"].FirstOrDefault(x => x.Key.Equals(currentArticle.CategoryID)).Value : (int?)null;

                    if (savedArticle == null)
                    {
                        var oldId = currentArticle.ID;
                        article.ID = 0;
                        var newArticleId = articleRepo.Insert(article).ID;
                        commonData[entity].Add(oldId, newArticleId);
                    }
                    else
                    {
                        commonData[entity].Add(currentArticle.ID, savedArticle.ID);
                        article.ID = savedArticle.ID;
                        articleRepo.Update(article);
                    }
                }
            }
        }

        
        //Import Artifacts Data
        public void ImportArtifactData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var artifactRepo = factory.GetInstance<IArtifactRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var artifacts = GetEntityList<List<Artifact>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var currentArtifact in artifacts)
                {
                    var articleId = commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentArtifact.ArticleID)).Value != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentArtifact.ArticleID)).Value : 0;
                    var labelId = commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArtifact.LabelID)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArtifact.LabelID)).Value : 0;

                    var savedArtifact = artifactRepo.GetByArticle(articleId).FirstOrDefault(x => x.LabelID == labelId && x.Name == currentArtifact.Name);

                    var artifact = new Artifact();
                    artifact = currentArtifact;
                    artifact.LabelID = commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArtifact.LabelID)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(currentArtifact.LabelID)).Value : (int?)null;
                    artifact.ArticleID = commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentArtifact.ArticleID)).Value != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentArtifact.ArticleID)).Value : (int?)null;

                    if (savedArtifact == null)
                    {
                        var oldId = currentArtifact.ID;
                        artifact.ID = 0;
                        var newArtifactId = artifactRepo.Insert(artifact).ID;
                        commonData[entity].Add(oldId, newArtifactId);
                    }
                    else
                    {
                        commonData[entity].Add(currentArtifact.ID, savedArtifact.ID);
                        artifact.ID = savedArtifact.ID;
                        artifactRepo.Update(artifact);
                    }
                }
            }
        }


        //Import Packs Data
        public void ImportPackData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var packRepo = factory.GetInstance<IPackRepository>();

            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var packs = GetEntityList<List<Pack>>(jsonObject.Value);
                commonData.Add(entity, new Dictionary<int, int>());

                foreach (var currentPack in packs)
                {
                    var savedPack = packRepo.GetByProjectID(projectId).FirstOrDefault(x => x.Name.Equals(currentPack.Name));

                    var pack = new Pack();

                    if (savedPack == null)
                    {
                        var oldId = pack.ID;
                        pack = currentPack;
                        pack.ID = 0;
                        pack.ProjectID = projectId;
                        var newPackId = packRepo.Insert(pack).ID;
                        commonData[entity].Add(oldId, newPackId);
                    }
                    else
                    {
                        commonData[entity].Add(currentPack.ID, savedPack.ID);
                        pack.ID = savedPack.ID;
                        packRepo.Update(pack);
                    }
                }
            }
        }


        //Import PackArticles Data
        public void ImportPackArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var packArticles = GetEntityList<List<PackArticle>>(jsonObject.Value);

					foreach (var currentPackArticle in packArticles)
					{
						var savedPackArticle = ctx.PackArticles.Where(p => p.PackID == currentPackArticle.PackID && p.ArticleID == currentPackArticle.ArticleID).FirstOrDefault();

						var packArticle = new PackArticle();

						if (savedPackArticle == null)
						{
							packArticle = currentPackArticle;
							packArticle.ID = 0;
							packArticle.PackID = commonData["Packs"].FirstOrDefault(x => x.Key.Equals(currentPackArticle.PackID)).Value != 0 ? commonData["Packs"].FirstOrDefault(x => x.Key.Equals(currentPackArticle.PackID)).Value : 0;
							packArticle.ArticleID = commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentPackArticle.ArticleID)).Value != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentPackArticle.ArticleID)).Value : 0;
							ctx.PackArticles.Add(packArticle);
						}
						else
						{
							packArticle = savedPackArticle;
							packArticle.Quantity = currentPackArticle.Quantity;
						}
					}

					ctx.SaveChanges();
				}
            }
        }
        

        //Import ArticlePreviewSettings Data
        public void ImportArticlePreviewSettingsData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var previewSettingsList = GetEntityList<List<ArticlePreviewSettings>>(jsonObject.Value);

					foreach (var currentPreviewSetting in previewSettingsList)
					{
						var savedPreviewSetting = ctx.ArticlePreviewSettings.Where(p => p.ArticleID == currentPreviewSetting.ArticleID).FirstOrDefault();

						var previewSettings = new ArticlePreviewSettings();

						if (savedPreviewSetting == null)
						{
							previewSettings = currentPreviewSetting;
							previewSettings.ID = 0;
							previewSettings.ArticleID = commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentPreviewSetting.ArticleID)).Value != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(currentPreviewSetting.ArticleID)).Value : 0;
							ctx.ArticlePreviewSettings.Add(previewSettings);
						}
						else
						{
							previewSettings = savedPreviewSetting;
							previewSettings.Rows = currentPreviewSetting.Rows;
							previewSettings.Cols = currentPreviewSetting.Cols;
						}
					}

					ctx.SaveChanges();
				}
            }
        }


		//Import ComparerConfiguration Data
		public void ImportComparerConfigurationData(Dictionary<string, string> data, string entity, int projectId)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
				var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
				var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
				if (!string.IsNullOrEmpty(jsonObject.Value))
				{
					var configs = GetEntityList<List<ComparerConfiguration>>(jsonObject.Value);

					foreach (var config in configs)
					{
						var savedConfig = ctx.ComparerConfiguration.FirstOrDefault(p => p.CompanyID == config.CompanyID && p.BrandID == config.BrandID && p.ProjectID == config.ProjectID);

						var newConfig = new ComparerConfiguration();

						if (savedConfig == null)
						{
							newConfig = config;
							newConfig.ID = 0;
							ctx.ComparerConfiguration.Add(newConfig);
						}
						else
						{
							newConfig = savedConfig;
							newConfig.Type = config.Type;
							newConfig.ColumnName = config.ColumnName;
							newConfig.Method = config.Method;
							newConfig.CategorizeArticle = config.CategorizeArticle;
						}
					}

					ctx.SaveChanges();
				}
			}
		}

        
        //Import WizardCustomSteps Data
        public void ImportWizardCustomStepsData(Dictionary<string, string> data, string entity, int projectId)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
				var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
				var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
				if (!string.IsNullOrEmpty(jsonObject.Value))
				{
					var wizardSteps = GetEntityList<List<WizardCustomStep>>(jsonObject.Value);

					foreach (var wizardStep in wizardSteps)
					{
						var savedConfig = ctx.WizardCustomSteps.FirstOrDefault(p => p.CompanyID == wizardStep.CompanyID && p.BrandID == wizardStep.BrandID && p.ProjectID == wizardStep.ProjectID);

						var newConfig = new WizardCustomStep();

						if (savedConfig == null)
						{
							newConfig = wizardStep;
							newConfig.ID = 0;
							ctx.WizardCustomSteps.Add(newConfig);
						}
						else
						{
							newConfig = savedConfig;
							newConfig.Type = wizardStep.Type;
							newConfig.Url = wizardStep.Url;
							newConfig.Position = wizardStep.Position;
							newConfig.Description = wizardStep.Description;
							newConfig.Name = wizardStep.Name;
						}
					}

					ctx.SaveChanges();
				}
			}
        }

        
        //Catalogs
        public void ImportCatalogData(Dictionary<string, string> data, string entity, int projectId)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var catalogs = GetEntityList<List<Catalog>>(jsonObject.Value);
                var idList = new Dictionary<int, int>();

                foreach (var currentCatalog in catalogs)
                {
                    var savedCatalog = catalogRepo.GetByName(projectId, currentCatalog.Name);

                    var catalog = new Catalog();
                    catalog = currentCatalog;
                    catalog.ProjectID = projectId;

                    var newId = 0;
                    var oldId = 0;
                    if (savedCatalog == null)
                    {
                        oldId = currentCatalog.CatalogID;
                        catalog.ID = 0;

                        //validate current catalog Ids
                        var catlogFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(catalog.Definition);
                        catalog.Definition = ImportProcessFields(catlogFields, idList);
                        newId = catalogRepo.Insert(catalog).CatalogID;
                    }
                    else
                    {
                        oldId = currentCatalog.CatalogID;
                        catalog.CatalogID = savedCatalog.CatalogID;
                        catalog.ID = savedCatalog.ID;

                        var svedDefinition = JsonConvert.DeserializeObject<List<FieldDefinition>>(savedCatalog.Definition);
                        var newDefinition = JsonConvert.DeserializeObject<List<FieldDefinition>>(currentCatalog.Definition);
                        var definition = currentCatalog.Definition;
                        var updatedDefinition = JsonConvert.DeserializeObject<List<FieldDefinition>>(definition);

                        foreach (var item in svedDefinition)
                        {
                            var savedField = newDefinition.SingleOrDefault(x => x.Name == item.Name);
                            if (savedField != null)
                                newDefinition.Remove(savedField);

                            if (item.CatalogID != null && item.CatalogID > 0)
                            {
                                var column = updatedDefinition.SingleOrDefault(x => x.Name == item.Name);
                                if (column != null)
                                    column.CatalogID = item.CatalogID;
                            }

                            var updtedField = updatedDefinition.SingleOrDefault(x => x.Name == item.Name);
                            if (updtedField != null)
                                updtedField.FieldID = item.FieldID;
                        }

                        //definition = JsonConvert.SerializeObject(updatedDefinition);

                        catalog.Definition = JsonConvert.SerializeObject(newDefinition);

                        //validate current catalog Ids
                        var catlogFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(catalog.Definition);
                        catalog.Definition = ImportProcessFields(catlogFields, idList);
                        definition = ImportProcessFields(updatedDefinition, idList);

                        catalogRepo.ImportCatalog(catalog.CatalogID, catalog, definition);
                        newId = catalog.CatalogID;
                    }

                    idList.Add(oldId, newId);

                    if (catalog.CatalogType == CatalogType.Lookup)
                    {
                        ImportDynamicData(data, newId, catalog.Name);
                    }
                }
            }
        }
        


        public string ImportProcessFields(List<FieldDefinition> fields, Dictionary<int, int> dictionary)
        {
            foreach (var field in fields)
            {
                if (field.CatalogID != null && field.CatalogID > 0)
                {
                    var value = dictionary.GetValueOrDefault(field.CatalogID.Value);
                    if (value != 0)
                        field.CatalogID = value;
                }
            }

            return JsonConvert.SerializeObject(fields);
        }

        
        public void ImportMappingData(Dictionary<string, string> data, string entity, int projectId)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var mappings = GetEntityList<List<DataImportMapping>>(jsonObject.Value);
					var catalogs = GetEntityList<List<Catalog>>(data.FirstOrDefault(x => x.Key.Equals("Catalogs")).Value);

					foreach (var currentMapping in mappings)
					{
						var savedMapping = ctx.DataImportMappings.Where(p => p.ProjectID == projectId).FirstOrDefault(x => x.Name == currentMapping.Name);

						var mapping = new DataImportMapping();
						mapping = currentMapping;
						mapping.ProjectID = projectId;
						var mappingId = currentMapping.ID;

						if (savedMapping == null)
						{
							mapping.ID = 0;
							mapping.RootCatalog = currentMapping.RootCatalog != 0 ? ctx.Catalogs.FirstOrDefault(x => x.ProjectID == projectId &&
											x.Name.Equals(catalogs.FirstOrDefault(y => y.ID == currentMapping.RootCatalog).Name)).ID : 0;
							ctx.DataImportMappings.Add(mapping);
						}
						else
						{
							mapping.ID = savedMapping.ID;
						}

						var newMappingId = mapping.ID;
						ImportColMappingData(data, "DataImportColMapping", mappingId, newMappingId);
					}

					ctx.SaveChanges();
				}
			}
        }
		

        public void ImportColMappingData(Dictionary<string, string> data, string entity, int mappingId, int newMappingId)
        {
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var colMappings = GetEntityList<List<DataImportColMapping>>(jsonObject.Value).Where(x => x.DataImportMappingID == mappingId).ToList();

					ctx.Database.ExecuteSqlCommand("delete from DataImportColMapping where DataImportMappingID = @id", new SqlParameter("@id", newMappingId));

					foreach (var currentMapping in colMappings)
					{
						var mapping = new DataImportColMapping();
						mapping = currentMapping;

						mapping.ID = 0;
						mapping.DataImportMappingID = newMappingId;
						ctx.DataImportColMapping.Add(mapping);
					}

					ctx.SaveChanges();
				}
            }
        }


        public void ImportDynamicData(Dictionary<string, string> data, int catalogId, string catalogName)
        {
			var catalogDataRepo = factory.GetInstance<ICatalogDataRepository>();
            var jsonObject = data.FirstOrDefault(x => x.Key.Equals(catalogName + "_Data"));
            if (!string.IsNullOrEmpty(jsonObject.Value))
            {
                var dataList = JsonConvert.DeserializeObject<List<JObject>>(jsonObject.Value);

                foreach (var catalogData in dataList)
                {
                    var id = int.Parse(catalogData["ID"].ToString());
                    var row = JObject.Parse(catalogDataRepo.GetByID(catalogId, id));
                    JToken token = row["ID"];
                    if (token == null)
                        catalogDataRepo.Insert(catalogId, catalogData.ToString());
                    else
                        catalogDataRepo.Update(catalogId, catalogData.ToString());
                }
            }
        }
        
        #endregion

        #endregion

    }
}
