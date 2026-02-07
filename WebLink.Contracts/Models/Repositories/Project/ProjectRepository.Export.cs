using Newtonsoft.Json;
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

		public T GetEntityList<T>(string value)
		{
			return JsonConvert.DeserializeObject<T>(value);
		}


		public void ExportProject(int projectid, string filePath)
		{
			var project = GetByID(projectid);
			CreateProjectZipFile(project, filePath);
		}


        private void CreateProjectZipFile(IProject project, string zipFile)
        {
            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    using (var conn = connManager.OpenWebLinkDB())
                    {
                        AddData(project, conn, archive);
                        AddFiles(project, conn, archive);
                    }
                }
            }
        }


        private void AddData(IProject project, IDBX conn, ZipArchive archive)
        {
            var Data = GetProjectData(project);
            foreach (var item in Data)
            {
                var entryName = $"Data/{item.Key}";
				var entry = archive.CreateEntry(entryName);
                using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                {
                    writer.Write(item.Value);
                }
            }
        }

        private Dictionary<string, string> GetProjectData(IProject project)
        {
            var data = new Dictionary<string, string>();
            using (IDBX conn = connManager.OpenWebLinkDB())
            {
				data.Add("Project", JsonConvert.SerializeObject(project));
                GetRFIDConfig(project, conn, data);
                GetOrderWorkflowConfig(project, conn, data);
                GetMaterialsData(project, conn, data);
				GetCategoriesData(project, conn, data);
				GetLabelsData(project, conn, data);
				GetArticlesData(project, conn, data);
				GetArtifactsData(project, conn, data);
                GetPacksData(project, conn, data);
                GetPackArticlesData(project, conn, data);
                GetArticlePreviewSettings(project, conn, data);
				GetComparerConfigurationData(project, conn, data);
				GetWizardCustomSteps(project, conn, data);
				GetCatalogs(project, conn, data);
				GetLookupCatalogsData(project, conn, data);
				GetMappingsData(project, conn, data);
				GetMappingsColumnData(project, conn, data);
				GetProviderData(project, conn, data);
                GetInlay(project, conn, data);
                GetInlayConfig(project, conn, data);
                GetCareLabelsCompoConfig (project, conn, data);  
            }
            return data;
        }

        private void GetCareLabelsCompoConfig(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            var articles = conn.Select<ArticleCompositionConfig>($@"
                select * from ArticleCompositionConfigs
                where ProjectID = @id",
                            project.ID);

            data.Add("ArticleCompositionConfigs", JsonConvert.SerializeObject(articles));
        }

        private void GetRFIDConfig(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			if (project.RFIDConfigID != null)
			{
				var rfidConfig = conn.SelectOne<RFIDConfig>($@"
					select * from RFIDParameters
					where id = @id;",
				project.RFIDConfigID);

				data.Add("RFIDParameters", JsonConvert.SerializeObject(rfidConfig));
			}
			else
			{
				data.Add("RFIDParameters", null);
			}
		}

        private void GetOrderWorkflowConfig(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            if(project.OrderWorkflowConfigID != null)
            {
                var orderWorkflowConfig = conn.SelectOne<OrderWorkflowConfig>($@"
					select * from OrderWorkflowConfigs
					where id = @id;",
                project.OrderWorkflowConfigID);

                data.Add("OrderWorkflowConfiguration", JsonConvert.SerializeObject(orderWorkflowConfig));
            }
            else
            {
                data.Add("OrderWorkflowConfiguration", null);
            }
        }


        private void GetMaterialsData(IProject project, IDBX conn, Dictionary<string, string> data)
        {
			var materials = conn.Select<Material>($@"select * from Materials");

			data.Add(
				"Materials", 
				JsonConvert.SerializeObject(materials)
            );
        }

		private void GetCategoriesData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var categories = conn.Select<Category>($@"
                select * from Categories 
                where ProjectID = @id",
			project.ID);

			data.Add("Categories", JsonConvert.SerializeObject(categories));
		}

		private void GetLabelsData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var labels = conn.Select<LabelData>($@"
                select * from Labels
                where ProjectID = @id or ProjectID is null",
			project.ID);

			data.Add("Labels", JsonConvert.SerializeObject(labels));
		}

		private void GetArticlesData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var articles = conn.Select<Article>($@"
                select * from Articles
                where ProjectID = @id",
			project.ID);

			data.Add("Articles", JsonConvert.SerializeObject(articles));
		}

		private void GetArtifactsData(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            var artifacts = conn.Select<Artifact>($@"
                select distinct art.* from Artifacts art 
                join Articles a on a.ID = art.ArticleID
                where a.ProjectID = @id",
            project.ID);

			data.Add("Artifacts", JsonConvert.SerializeObject(artifacts));
		}

		private void GetPacksData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var packs = conn.Select<Pack>($@"
                select * from Packs
                where ProjectID = @id",
			project.ID);

			data.Add("Packs", JsonConvert.SerializeObject(packs));
		}

		private void GetPackArticlesData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var packArticles = conn.Select<PackArticle>($@"
                select distinct pa.* from PackArticles pa 
                join Packs p on p.ID = pa.PackID
                where p.ProjectID = @id",
			project.ID);

			data.Add("PackArticles", JsonConvert.SerializeObject(packArticles));
		}

        private void GetArticlePreviewSettings(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            var settings = conn.Select<ArticlePreviewSettings>($@"
                select distinct ap.* from ArticlePreviewSettings ap
                join Articles a on a.ID = ap.ArticleID
                where a.ProjectID = @id",
            project.ID);

			data.Add("ArticlePreviewSettings", JsonConvert.SerializeObject(settings));
		}

		private void GetComparerConfigurationData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var settings = conn.Select<ComparerConfiguration>($@"
                select * from ComparerConfiguration
                where ProjectID = @id",
			project.ID);

			data.Add("ComparerConfiguration", JsonConvert.SerializeObject(settings));
		}

		private void GetWizardCustomSteps(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var settings = conn.Select<WizardCustomStep>($@"
                select * from WizardCustomSteps
                where ProjectID = @id",
			project.ID);

			data.Add("WizardCustomSteps", JsonConvert.SerializeObject(settings));
		}

		private void GetCatalogs(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var catalogs = conn.Select<Catalog>($@"
                select * from Catalogs
                where ProjectID = @id",
			project.ID);

			catalogs = SortCatalogs(catalogs);

            data.Add("Catalogs", JsonConvert.SerializeObject(catalogs));
		}

        private void GetInlay(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            var catalogs = conn.Select<InLay>($@"
                select * from Inlays");

            data.Add("Inlays", JsonConvert.SerializeObject(catalogs));
        }

        private void GetInlayConfig(IProject project, IDBX conn, Dictionary<string, string> data)
        {
            var catalogs = conn.Select<InlayConfig>($@"
                select * from InlayConfigs");

            data.Add("InlayConfig", JsonConvert.SerializeObject(catalogs));
        }

        private List<Catalog> SortCatalogs(List<Catalog> catalogs)
        {
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

            return sortedCatalogs;
        }

        public void SetCatalogOrder(List<int> catalogList, Catalog catalog, List<Catalog> catalogs, List<int> parentIds)
        {
            var fields = catalog.Fields;
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

        private void GetLookupCatalogsData(IProject project, IDBX conn, Dictionary<string, string> data)
        {
			var catalogDataRepo = factory.GetInstance<ICatalogDataRepository>();

			var catalogs = JsonConvert.DeserializeObject<List<Catalog>>(data["Catalogs"]);

            var lookupCatalogs = catalogs.Where(x => x.CatalogType == CatalogType.Lookup).ToList();
            foreach (var catalog in lookupCatalogs)
            {
                var catalogData = catalogDataRepo.GetList(catalog.CatalogID);
                data.Add(catalog.Name + "_Data", catalogData);

                //Get Rel data
                var fields = catalog.Fields;
                foreach (var field in fields)
                {
                    if (field.Type == ColumnType.Set && field.CatalogID != null && field.CatalogID > 0)
                    {
                        var relData = catalogDataRepo.GetFullSubset(catalog.CatalogID, field.Name);
                        data.Add($"Rel_{catalog.ID}_{field.CatalogID}_{field.FieldID}_Data", relData);
                    }
                }
            }
        }

		private void GetMappingsData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			//var mappings = conn.Select<DataImportMapping>($@"
   //             select * from DataImportMappings
   //             where ProjectID = @id",
			//project.ID);

            using(var ctx = factory.GetInstance<PrintDB>())
            {
               var mappings = ctx.DataImportMappings.Where (m=>m.ProjectID == project.ID).ToList();
               data.Add("Mappings", JsonConvert.SerializeObject(mappings));
            }

			
		}

		private void GetMappingsColumnData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var cols = conn.Select<DataImportColMapping>($@"
                select col.* from DataImportColMapping col
                join DataImportMappings m on m.ID = col.DataImportMappingID
                where m.ProjectID = @id",
			project.ID);

			data.Add("MappingsCols", JsonConvert.SerializeObject(cols));
		}


		private void GetProviderData(IProject project, IDBX conn, Dictionary<string, string> data)
		{
			var brand = conn.SelectOne<Brand>("select * from Brands where ID = @id", project.BrandID);

			var providers = conn.Select<CompanyProvider>($@"
                select * from CompanyProviders
                where CompanyID = @id",
			brand.CompanyID);

			data.Add("CompanyProviders", JsonConvert.SerializeObject(providers));

			GetProviderCompanies(brand.CompanyID, conn, data);
		}


		private void GetProviderCompanies(int companyid, IDBX conn, Dictionary<string, string> data)
		{
			var companies = conn.Select<Company>($@"
				select * from Companies where ID in (
					select ProviderCompanyID from CompanyProviders where CompanyID = @companyid
				)",
			companyid);

			data.Add("Companies", JsonConvert.SerializeObject(companies));
		}


		private void AddFiles(IProject project, IDBX conn, ZipArchive archive)
        {
            var labels = conn.Select<LabelData>(@"
				select * from Labels 
                where ProjectID = @id", project.ID);

            if (!projectStore.TryGetFile(project.ID, out var container))
                container = projectStore.GetOrCreateFile(project.ID, Project.FILE_CONTAINER_NAME);

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
			var readmeEntry = archive.CreateEntry($"Images/ImageMetadata.json");
			using (StreamWriter writer = new StreamWriter(readmeEntry.Open(), Encoding.UTF8))
			{
				writer.Write(meta);
			}

			//Add Project Article previews            
			var articles = conn.Select<Article>(@"
				select * from Articles 
                where ProjectID = @id", project.ID);

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


		#region Deprecated Code

		//private bool UploadProject(int brandId, string filePath)
		//{
		//	var data = new Dictionary<string, string>();
		//	var commonData = new Dictionary<string, Dictionary<int, int>>();
		//	var newProjectId = 0;

		//	using (FileStream fs = new FileStream(filePath, FileMode.Open))
		//	{
		//		using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
		//		{
		//			//Upload Data Process
		//			var entries = archive.Entries.Where(x => x.FullName.Contains("Data/"));

		//			if (entries.Count() > 0)
		//			{
		//				foreach (var entry in entries)
		//				{
		//					var reader = new StreamReader(entry.Open(), Encoding.UTF8);
		//					data.Add(entry.Name.Split(".").FirstOrDefault(), reader.ReadToEnd());
		//				}

		//				newProjectId = UploadDataProcess(brandId, data, commonData);
		//			}

		//			//Upload File Process
		//			var files = archive.Entries.Where(x => !x.FullName.Contains("Data/")).ToList();

		//			if (files.Count() > 0 && newProjectId != 0)
		//			{
		//				UploadFilesProcess(archive, files, newProjectId, commonData, data);
		//			}

		//			return true;
		//		}
		//	}
		//}



		//private int UploadDataProcess(int brandId, Dictionary<string, string> data, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals("Project"));
		//	var project = GetEntityList<List<Project>>(jsonObject.Value).FirstOrDefault();

		//	var newProject = new Project();
		//	newProject = project;
		//	newProject.ID = 0;
		//	newProject.BrandID = brandId;
		//	newProject.Name = project.Name;
		//	newProject.EnableFTPFolder = false;
		//	newProject.FTPFolder = null;
		//	var newProjectId = Insert(project).ID;

		//	//Upload Materials Data
		//	UploadMaterialData(data, "Materials", newProjectId, commonData);

		//	//Upload Categories Data
		//	UploadCategoryData(data, "Categories", newProjectId, commonData);

		//	//Upload Labels Data
		//	UploadLabelData(data, "Labels", newProjectId, commonData);

		//	//Upload Articles Data
		//	UploadArticleData(data, "Articles", newProjectId, commonData);

		//	//Upload Artifacts Data
		//	UploadArtifactData(data, "Artifacts", newProjectId, commonData);

		//	//Upload Packs Data
		//	UploadPackData(data, "Packs", newProjectId, commonData);

		//	//Upload ArticlePreviewSettings Data
		//	UploadArticlePreviewSettingsData(data, "ArticlePreviewSettings", newProjectId, commonData);

		//	//Upload RFIDParameters Data
		//	UploadRFIDParametersData(data, "RFIDParameters", newProjectId);

		//	//Upload ComparerConfiguration Data
		//	UploadComparerConfigurationData(data, "ComparerConfiguration", newProjectId);

		//	//Upload WizardCustomSteps Data
		//	UploadWizardCustomStepsData(data, "WizardCustomSteps", newProjectId);

		//	//Add Catalogs Data
		//	UploadCatalogData(data, "Catalogs", newProjectId, commonData);

		//	//Upload PackArticles Data
		//	UploadPackArticleData(data, "PackArticles", newProjectId, commonData);

		//	//Add DataImportMappings Data
		//	UploadMappingData(data, "DataImportMappings", newProjectId);

		//	//Copy Files
		//	//AddFiles(id, newProjectId);

		//	return newProjectId;
		//}

		////Upload Materials Data
		//public void UploadMaterialData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var materialRepo = factory.GetInstance<IMaterialRepository>();

		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var materials = GetEntityList<List<Material>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());

		//		foreach (var material in materials)
		//		{
		//			var currentMaterial = materialRepo.GetList().FirstOrDefault(x => x.Name.Equals(material.Name));
		//			var oldId = material.ID;
		//			var newMaterial = new Material();
		//			newMaterial = material;
		//			newMaterial.ID = 0;
		//			var newMaterialId = currentMaterial == null ? materialRepo.Insert(newMaterial).ID : currentMaterial.ID;
		//			commonData[entity].Add(oldId, newMaterialId);
		//		}
		//	}
		//}

		////Upload Categories Data
		//public void UploadCategoryData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var categoryRepo = factory.GetInstance<ICategoryRepository>();
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var categories = GetEntityList<List<Category>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());

		//		foreach (var category in categories)
		//		{
		//			var oldId = category.ID;
		//			var newCategory = new Category();
		//			newCategory = category;
		//			newCategory.ID = 0;
		//			newCategory.ProjectID = projectId;
		//			var newCategoryId = categoryRepo.Insert(newCategory).ID;
		//			commonData[entity].Add(oldId, newCategoryId);
		//		}
		//	}
		//}

		////Upload Labels Data
		//public void UploadLabelData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var labelRepo = factory.GetInstance<ILabelRepository>();

		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var labels = GetEntityList<List<LabelData>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());

		//		foreach (var label in labels)
		//		{
		//			var oldId = label.ID;
		//			var newLabel = new LabelData();
		//			newLabel = label;
		//			newLabel.ID = 0;
		//			newLabel.ProjectID = projectId;
		//			newLabel.MaterialID = label.MaterialID != null ? commonData["Materials"].FirstOrDefault(x => x.Key.Equals(label.MaterialID)).Value : (int?)null;
		//			var newLabelId = labelRepo.Insert(newLabel).ID;
		//			commonData[entity].Add(oldId, newLabelId);
		//		}
		//	}
		//}

		////Upload Articles Data
		//public void UploadArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var articleRepo = factory.GetInstance<IArticleRepository>();

		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var articles = GetEntityList<List<Article>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());

		//		foreach (var article in articles)
		//		{
		//			var oldLabelId = article.LabelID;
		//			var oldCategoryId = article.CategoryID;
		//			var oldId = article.ID;
		//			var newArticle = new Article();
		//			newArticle = article;
		//			newArticle.ID = 0;
		//			newArticle.ProjectID = projectId;
		//			newArticle.LabelID = oldLabelId != null ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(oldLabelId)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(oldLabelId)).Value : (int?)null : null;
		//			newArticle.CategoryID = oldCategoryId != null ? commonData["Categories"].FirstOrDefault(x => x.Key.Equals(oldCategoryId)).Value : (int?)null;
		//			var newArticleId = articleRepo.Insert(newArticle).ID;
		//			commonData[entity].Add(oldId, newArticleId);
		//		}
		//	}
		//}

		////Upload Artifacts Data
		//public void UploadArtifactData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var artifacts = GetEntityList<List<Artifact>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());
		//		var artifactList = new List<Artifact>();

		//		foreach (var artifact in artifacts)
		//		{
		//			var newArtifact = new Artifact();
		//			newArtifact = artifact;
		//			newArtifact.ID = 0;
		//			newArtifact.LabelID = artifact.LabelID != null ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(artifact.LabelID)).Value != 0 ? commonData["Labels"].FirstOrDefault(x => x.Key.Equals(artifact.LabelID)).Value : (int?)null : null;
		//			newArtifact.ArticleID = artifact.ArticleID != null ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(artifact.ArticleID)).Value : (int?)null;
		//			artifactList.Add(newArtifact);
		//		}

		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			ctx.Artifacts.AddRange(artifactList);
		//			ctx.SaveChanges();
		//		}
		//	}
		//}


		////Upload Packs Data
		//public void UploadPackData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var packRepo = factory.GetInstance<IPackRepository>();

		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var packs = GetEntityList<List<Pack>>(jsonObject.Value);
		//		commonData.Add(entity, new Dictionary<int, int>());

		//		foreach (var pack in packs)
		//		{
		//			var oldId = pack.ID;
		//			var newPack = new Pack();
		//			newPack = pack;
		//			newPack.ID = 0;
		//			newPack.ProjectID = projectId;
		//			var newPackId = packRepo.Insert(newPack).ID;
		//			commonData[entity].Add(oldId, newPackId);
		//		}
		//	}
		//}


		////Upload PackArticles Data
		//public void UploadPackArticleData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var packArticles = GetEntityList<List<PackArticle>>(jsonObject.Value);
		//		var packArticleList = new List<PackArticle>();

		//		foreach (var packArticle in packArticles)
		//		{
		//			var newPackArticle = new PackArticle();
		//			newPackArticle = packArticle;
		//			newPackArticle.ID = 0;
		//			newPackArticle.PackID = newPackArticle.PackID != 0 ? commonData["Packs"].FirstOrDefault(x => x.Key.Equals(newPackArticle.PackID)).Value : 0;
		//			newPackArticle.ArticleID = newPackArticle.ArticleID != null && newPackArticle.ArticleID > 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(newPackArticle.ArticleID)).Value : (int?)null;
		//			newPackArticle.CatalogID = newPackArticle.CatalogID != null && newPackArticle.CatalogID > 0 ? commonData["Catalogs"].FirstOrDefault(x => x.Key.Equals(newPackArticle.CatalogID)).Value : (int?)null;

		//			newPackArticle.ArticleID = newPackArticle.ArticleID != 0 ? newPackArticle.ArticleID : (int?)null;
		//			packArticleList.Add(newPackArticle);

		//		}

		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			ctx.PackArticles.AddRange(packArticleList);
		//			ctx.SaveChanges();
		//		}
		//	}
		//}

		////Upload ArticlePreviewSettings Data
		//public void UploadArticlePreviewSettingsData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var previewSettings = GetEntityList<List<ArticlePreviewSettings>>(jsonObject.Value);
		//		var previewSettingsList = new List<ArticlePreviewSettings>();

		//		foreach (var previewSetting in previewSettings)
		//		{
		//			var newPreview = new ArticlePreviewSettings();
		//			newPreview = previewSetting;
		//			newPreview.ID = 0;
		//			newPreview.ArticleID = newPreview.ArticleID != 0 ? commonData["Articles"].FirstOrDefault(x => x.Key.Equals(newPreview.ArticleID)).Value : 0;
		//			previewSettingsList.Add(newPreview);
		//		}

		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			ctx.ArticlePreviewSettings.AddRange(previewSettingsList);
		//			ctx.SaveChanges();
		//		}
		//	}
		//}

		////Upload RFIDParameters Data
		//public void UploadRFIDParametersData(Dictionary<string, string> data, string entity, int projectId)
		//{
		//	var rfidRepo = factory.GetInstance<IRFIDConfigRepository>();
		//	var project = (Project)GetByID(projectId);

		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var configs = GetEntityList<List<RFIDConfig>>(jsonObject.Value);

		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			foreach (var config in configs)
		//			{
		//				var newConfig = new RFIDConfig();
		//				newConfig = config;
		//				newConfig.ID = 0;
		//				project.RFIDConfigID = rfidRepo.Insert(newConfig).ID;
		//				ctx.Projects.Update(project);
		//			}

		//			ctx.SaveChanges();
		//		}
		//	}
		//}

		////Upload ComparerConfiguration Data
		//public void UploadComparerConfigurationData(Dictionary<string, string> data, string entity, int projectId)
		//{
		//	using (var ctx = factory.GetInstance<PrintDB>())
		//	{
		//		var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
		//		var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
		//		var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//		if (!string.IsNullOrEmpty(jsonObject.Value))
		//		{
		//			var configs = GetEntityList<List<ComparerConfiguration>>(jsonObject.Value);

		//			foreach (var config in configs)
		//			{
		//				var newConfig = new ComparerConfiguration();
		//				newConfig = config;
		//				newConfig.ID = 0;
		//				newConfig.CompanyID = companyId;
		//				newConfig.BrandID = brandId;
		//				newConfig.ProjectID = projectId;
		//				ctx.ComparerConfiguration.Add(newConfig);
		//			}

		//			ctx.SaveChanges();
		//		}
		//	}
		//}

		////Upload WizardCustomSteps Data
		//public void UploadWizardCustomStepsData(Dictionary<string, string> data, string entity, int projectId)
		//{
		//	using (var ctx = factory.GetInstance<PrintDB>())
		//	{
		//		var brandId = ctx.Projects.FirstOrDefault(x => x.ID.Equals(projectId)).BrandID;
		//		var companyId = ctx.Brands.FirstOrDefault(x => x.ID.Equals(brandId)).CompanyID;
		//		var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//		if (!string.IsNullOrEmpty(jsonObject.Value))
		//		{
		//			var wizardSteps = GetEntityList<List<WizardCustomStep>>(jsonObject.Value);

		//			foreach (var wizardStep in wizardSteps)
		//			{
		//				var newWizardStep = new WizardCustomStep();
		//				newWizardStep = wizardStep;
		//				newWizardStep.ID = 0;
		//				newWizardStep.CompanyID = companyId;
		//				newWizardStep.BrandID = brandId;
		//				newWizardStep.ProjectID = projectId;
		//				ctx.WizardCustomSteps.Add(newWizardStep);
		//			}

		//			ctx.SaveChanges();
		//		}
		//	}
		//}

		////Catalogs
		//public void UploadCatalogData(Dictionary<string, string> data, string entity, int projectId, Dictionary<string, Dictionary<int, int>> commonData)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var catalogs = GetEntityList<List<Catalog>>(jsonObject.Value);
		//		var idList = new Dictionary<int, int>();
		//		commonData.Add(entity, new Dictionary<int, int>());


		//		foreach (var catalog in catalogs)
		//		{
		//			var oldId = catalog.CatalogID;
		//			var oldKeyId = catalog.ID;
		//			var newCatalog = new Catalog();
		//			newCatalog = catalog;
		//			newCatalog.ID = 0;
		//			newCatalog.ProjectID = projectId;
		//			//validate current catalog Ids
		//			ProcessFields(newCatalog, idList);
		//			var insertedCatalog = catalogRepo.Insert(newCatalog);
		//			idList.Add(oldId, insertedCatalog.CatalogID);
		//			if (newCatalog.CatalogType == CatalogType.Lookup)
		//			{
		//				AddDynamicData(data, insertedCatalog.CatalogID, catalog.Name);
		//			}

		//			commonData[entity].Add(oldKeyId, insertedCatalog.ID);

		//		}
		//	}
		//}

		//public void ProcessFields(Catalog catalog, Dictionary<int, int> dictionary)
		//{
		//	var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(catalog.Definition);

		//	foreach (var field in fields)
		//	{
		//		if (field.CatalogID != null && field.CatalogID > 0)
		//		{
		//			field.CatalogID = dictionary.GetValueOrDefault(field.CatalogID.Value);
		//		}
		//	}

		//	catalog.Definition = JsonConvert.SerializeObject(fields);
		//}

		//public void UploadMappingData(Dictionary<string, string> data, string entity, int projectId)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			var mappings = GetEntityList<List<DataImportMapping>>(jsonObject.Value);
		//			var catalogs = GetEntityList<List<Catalog>>(data.FirstOrDefault(x => x.Key.Equals("Catalogs")).Value);

		//			foreach (var mapping in mappings)
		//			{
		//				var mappingId = mapping.ID;
		//				var newMapping = new DataImportMapping();
		//				newMapping = mapping;
		//				newMapping.ID = 0;
		//				newMapping.ProjectID = projectId;
		//				newMapping.RootCatalog = mapping.RootCatalog != 0 ? ctx.Catalogs.FirstOrDefault(x => x.ProjectID == projectId &&
		//							x.Name.Equals(catalogs.FirstOrDefault(y => y.ID == mapping.RootCatalog).Name)).ID : 0;
		//				ctx.DataImportMappings.Add(newMapping);
		//				ctx.SaveChanges();
		//				var newMappingId = newMapping.ID;

		//				AddColMappingData(data, "DataImportColMapping", mappingId, newMappingId);
		//			}
		//		}
		//	}
		//}


		//public void AddColMappingData(Dictionary<string, string> data, string entity, int oldMappingId, int newMappingId)
		//{
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(entity));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		using (var ctx = factory.GetInstance<PrintDB>())
		//		{
		//			var colMappings = GetEntityList<List<DataImportColMapping>>(jsonObject.Value).Where(x => x.DataImportMappingID == oldMappingId).ToList();

		//			foreach (var cols in colMappings)
		//			{
		//				var newColMapping = new DataImportColMapping();
		//				newColMapping = cols;
		//				newColMapping.ID = 0;
		//				newColMapping.DataImportMappingID = newMappingId;
		//				ctx.DataImportColMapping.Add(newColMapping);
		//			}

		//			ctx.SaveChanges();
		//		}
		//	}
		//}


		//public void AddDynamicData(Dictionary<string, string> data, int catalogId, string catalogName)
		//{
		//	var catalogDataRepo = factory.GetInstance<ICatalogDataRepository>();
		//	var jsonObject = data.FirstOrDefault(x => x.Key.Equals(catalogName + "_Data"));
		//	if (!string.IsNullOrEmpty(jsonObject.Value))
		//	{
		//		var dataList = JsonConvert.DeserializeObject<List<JObject>>(jsonObject.Value);

		//		foreach (var catalogData in dataList)
		//		{
		//			catalogDataRepo.Insert(catalogId, catalogData.ToString());
		//		}
		//	}
		//}


		///*Copy Files from old project*/
		//private void AddFiles(int oldProjectId, int newProjectId)
		//{
		//	if (projectStore.TryGetFile(oldProjectId, out var file))
		//	{
		//		var projectFile = projectStore.GetOrCreateFile(newProjectId, file.FileName);
		//		using (var src = file.GetContentAsStream())
		//		{
		//			projectFile.SetContent(src);
		//		}

		//		foreach (var category in projectStore.Categories)
		//		{
		//			foreach (var sourceAttachment in file.GetAttachmentCategory(category))
		//			{
		//				var targetAttachments = projectFile.GetAttachmentCategory(category);
		//				var targetAttachment = targetAttachments.GetOrCreateAttachment(sourceAttachment.FileName);
		//				using (var src = sourceAttachment.GetContentAsStream())
		//				{
		//					targetAttachment.SetContent(src);
		//				}
		//			}
		//		}
		//	}
		//}

		#endregion
	}
}
