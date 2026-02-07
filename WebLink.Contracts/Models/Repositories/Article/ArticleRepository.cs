using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class ArticleRepository : GenericRepository<IArticle, Article>, IArticleRepository
	{
		private IDBConnectionManager connManager;
		private IProjectRepository projectRepo;
		private ILabelRepository labelRepo;
		private IPackRepository packRepo;
		private IRemoteFileStore store;

		public ArticleRepository(
			IFactory factory,
			IDBConnectionManager connManager,
			IProjectRepository projectRepo,
			ILabelRepository labelRepo,
			IPackRepository packRepo,
			IFileStoreManager storeManager,
			IAppConfig config
			)
			: base(factory, (ctx) => ctx.Articles)
		{
			this.connManager = connManager;
			this.projectRepo = projectRepo;
			this.labelRepo = labelRepo;
			this.packRepo = packRepo;
			this.store = storeManager.OpenStore("ArticlePreviewStore");
		}

		protected override string TableName { get => "Articles"; }

		protected override void BeforeInsert(PrintDB ctx, IUserData userData, Article actual, out bool cancelOperation)
		{
			cancelOperation = false;
			actual.PrintCountSequence = Guid.NewGuid();
		}

		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Article actual, IArticle data)
		{
			actual.Name = data.Name;
			actual.Description = data.Description;

			if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService))
			{
				actual.ProjectID = data.ProjectID;
				if (data.ProjectID == 0)
					actual.ProjectID = null;
				actual.ArticleCode = data.ArticleCode;
				actual.BillingCode = data.BillingCode;
				actual.LabelID = data.LabelID == null || data.LabelID < 1 ? null : data.LabelID;
				actual.Instructions = data.Instructions;
				actual.CategoryID = data.CategoryID;
				actual.SyncWithSage = data.SyncWithSage;
				actual.SageRef = data.SageRef;
				actual.EnableLocalPrint = data.EnableLocalPrint;
				actual.EnableConflicts = data.EnableConflicts;
				actual.PrintCountSequenceType = data.PrintCountSequenceType;
				actual.PrintCountSelectorField = data.PrintCountSelectorField;
				actual.PrintCountSelectorType = data.PrintCountSelectorType;
                actual.EnableAddItems = data.EnableAddItems;
				

				if (actual.ProjectID == null && actual.LabelID.HasValue)
				{
					var label = labelRepo.GetByID(actual.LabelID.Value);
					if (label.ProjectID != null)
					{
						label.ProjectID = null;
						labelRepo.Update(label);
					}
				}
			}
		}


		protected override void AfterDelete(PrintDB ctx, IUserData userData, Article actual)
		{
			store.DeleteFile(actual.ID);
		}


		public List<ArticleViewModel> GetFullByProjectID(ArticleByProjectFilter filter)
		{
			var project = projectRepo.GetByID(filter.ProjectID, true); //Ensures user has access to the specified project.

			int typeInt = (int)filter.ArticleType;

			using (var conn = connManager.OpenWebLinkDB())
			{
				return conn.Select<ArticleViewModel>($@"
					select a.ID, a.ProjectID, a.Name, a.Description, a.CategoryID, ct.Name as CategoryName, a.EnableAddItems,
						(case 
							when l.[Type] = 1 then 'Sticker'
							when l.[Type] = 2 then 'Care Label'
							when l.[Type] = 3 then 'Hang Tag'
							when l.[Type] = 4 then 'PiggyBack'
							else 'Unknown Label Type' end) as LabelType,
						a.ArticleCode, a.BillingCode, l.MaterialID, m.Name as MaterialName,
						a.LabelID, l.Name as LabelName, isnull(l.EncodeRFID,0) as EncodeRFID,
                        (case 
                            when a.LabelID is null then 0
                            else 1 end
                        ) as IsLabel
					from Articles a
						left outer join Labels l on a.LabelID = l.ID
						left outer join Materials m on l.MaterialID = m.ID
                        left outer join Categories ct on a.CategoryID = ct.ID
					where (a.ProjectID = @projectid or a.ProjectID is null)
					AND (({typeInt} = 0) OR ({typeInt} = 1 AND a.LabelID IS NOT NULL) OR (a.LabelID IS NULL))
					order by IsLabel DESC, ct.Name, a.Name
				", filter.ProjectID).ToList();
			}
		}


		public ArticleViewModel GetFullArticle(int id)
		{
			var article = GetByID(id); //Ensures user has access to this article
			using (var conn = connManager.OpenWebLinkDB())
			{
				var result = conn.SelectOne<ArticleViewModel>(@"
					select a.ID, a.ProjectID, a.Name, a.Description,
						(case 
							when l.[Type] = 1 then 'Sticker'
							when l.[Type] = 2 then 'Care Label'
							else 'Hang Tag' end) as LabelType,
						a.ArticleCode, a.BillingCode, l.MaterialID, m.Name as MaterialName,
						a.LabelID, l.Name as LabelName, isnull(l.EncodeRFID,0) as EncodeRFID
					from Articles a
						left outer join Labels l on a.LabelID = l.ID
						left outer join Materials m on l.MaterialID = m.ID
					where (a.ID = @id)

				", id);
				return result;
			}
		}


		public IArticle GetByCode(string code)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCode(ctx, code);
			}
		}


		public IArticle GetByCode(PrintDB ctx, string code)
		{
			var userData = factory.GetInstance<IUserData>();
			return (from a in ctx.Articles
					where a.ArticleCode == code && a.ProjectID == userData.SelectedProjectID
					select a)
					.AsNoTracking()
					.FirstOrDefault();
		}


		public List<PackArticleViewModel> GetByPackID(int packid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByPackID(ctx, packid);
			}
		}


		public List<PackArticleViewModel> GetByPackID(PrintDB ctx, int packid)
		{
			var pack = packRepo.GetByID(ctx, packid);  // NOTE: GetByID Ensures user has access to this pack

			return (from pa in ctx.PackArticles
					join a in ctx.Articles on pa.ArticleID equals a.ID into paj
					from s in paj.DefaultIfEmpty()
					where pa.PackID == packid
					select new PackArticleViewModel
					{
						ID = pa.ID,
						PackID = pa.PackID,
						ArticleID = pa.ArticleID,
						Name = pa.Article.Name,
						ArticleCode = pa.Article.ArticleCode,
						Quantity = pa.Quantity,
						Catalog = pa.Catalog.Name,
						Condition = pa.Condition,
						FieldName = pa.FieldName,
						Mapping = pa.Mapping,
						PluginName = pa.PluginName,
						Type = pa.Type,
                        AllowEmptyValues = pa.AllowEmptyValues

					}).ToList();
		}


		public List<IArticle> GetByProjectID(int projectid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByProjectID(ctx, projectid);
			}
		}


		public List<IArticle> GetByProjectID(PrintDB ctx, int projectid)
		{
			projectRepo.GetByID(ctx, projectid); // NOTE: GetByID Ensures user has access to this project

			return new List<IArticle>((
				from a in ctx.Articles
				where a.ProjectID == null || a.ProjectID == projectid
				select a)
				.AsNoTracking());
		}


		public IArticle GetByCodeInProject(string code, int projectID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCodeInProject(ctx, code, projectID);
			}
		}


		public IArticle GetByCodeInProject(PrintDB ctx, string code, int projectID)
		{
			return ctx.Articles.Where(
				w => w.ProjectID == projectID &&
					 w.ArticleCode == code)
				.AsNoTracking()
				.FirstOrDefault();
		}


		public void SetArticlePreview(int id, byte[] imageContent)
		{
			var article = GetByID(id);
			var preview = ImageProcessing.CreateThumb(imageContent, 250, 250);
			var file = store.GetOrCreateFile(id, "preview.png");
			file.SetContent(preview);
		}


		public Stream GetArticlePreview(int id)
		{
			var article = GetByID(id);
			if (article.LabelID == null)
			{
				if (store.TryGetFile(id, out var file))
					return file.GetContentAsStream();
				else
					return null;
			}
			else
			{
				return labelRepo.GetLabelPreview(article.LabelID.Value);
			}
		}

		public Stream GetFixedArticlePreview(int id)
		{
			var article = GetByID(id);
			if (store.TryGetFile(id, out var file))
				return file.GetContentAsStream();
			else
				return null;
		}


		public Guid GetArticlePreviewReference(int articleid)
		{
			var article = GetByID(articleid);
			if (article.LabelID == null)
			{
				if (store.TryGetFile(articleid, out var file))
					return file.FileGUID;
				else
					return Guid.Empty;
			}
			else
			{
				return labelRepo.GetLabelPreviewReference(article.LabelID.Value);
			}
		}


		/// <summary>
		/// Individually check for company orders registerd in Sage
		/// </summary>
		public IEnumerable<IArticle> GetRegisteredInSage()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetRegisteredInSage(ctx);
			}
		}


		public IEnumerable<IArticle> GetRegisteredInSage(PrintDB ctx)
		{
			var now = DateTime.Now;
			var q = ctx.Articles
				.Where(w => w.SyncWithSage.Equals(true))
                .Where(w => !string.IsNullOrEmpty(w.SageRef) )
				.Select(s => new Article()
				{
					ID = s.ID,
					SageRef = s.SageRef,
                    ProjectID = s.ProjectID
				});

			return q.ToList();
		}


		public IArticle GetBySageReference(string reference, int projectID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetBySageReference(ctx, reference, projectID);
			}
		}


		public IArticle GetBySageReference(PrintDB ctx, string reference, int projectID)
		{
			var article = ctx.Articles
				.FirstOrDefault(f => f.SageRef.Equals(reference) && f.ProjectID.Equals(projectID));

			return article;
		}


		public IArticle GetSharedByCode(string articlecode)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetSharedByCode(ctx, articlecode);
			}
		}


		public IArticle GetSharedByCode(PrintDB ctx, string articlecode)
		{
			var article = ctx.Articles
				.AsNoTracking()
				.Where(a => a.ProjectID == null && a.ArticleCode == articlecode)
				.FirstOrDefault();

            if (article == null)
                //throw new ArticleCodeNotFoundException($"Could not locate SHARED article with code \"{articlecode}\".", articlecode);
                return null;

			return article;
		}



		public IArticle GetDefaultArticle()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetDefaultArticle(ctx);
			}
		}


		public IArticle GetDefaultArticle(PrintDB ctx)
		{
			var article = ctx.Articles
				.AsNoTracking()
				.Where(a => a.ProjectID == null && a.ArticleCode == Article.EMPTY_ARTICLE_CODE)
				.FirstOrDefault();

			if (article == null)
			{
				var label = GetOrCreateDefaultLabel(ctx);
				article = new Article()
				{
					ProjectID = null,
					Name = "Not Selected",
					Description = "Article to be used while the user picks which label to print",
					ArticleCode = Article.EMPTY_ARTICLE_CODE,
					BillingCode = Article.EMPTY_ARTICLE_CODE,
					LabelID = label.ID,
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now
				};
				ctx.Articles.Add(article);
				ctx.SaveChanges();
			}
			return article;
		}


		private LabelData GetOrCreateDefaultLabel(PrintDB ctx)
		{
			var label = ctx.Labels.AsNoTracking().FirstOrDefault(l => l.ProjectID == null && l.Name == "Dummy");
			if (label == null)
			{
				label = new LabelData()
				{
					ProjectID = null,
					Name = "Dummy",
					Comments = "Label to be used temporarily while the user picks the correct label.",
					Type = LabelType.Sticker,
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now
				};
				ctx.Labels.Add(label);
				ctx.SaveChanges();
			}
			return label;
		
		}

		public IEnumerable<IArticle> GetArticleByLabelType(RequestLabelType rq)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetArticleByLabelType(ctx, rq).ToList();
			}
		}

		public IEnumerable<IArticle> GetArticleByLabelType(PrintDB ctx, RequestLabelType rq)
		{
			// looking for projects identifier
			var oids = new List<int>();

			rq.Selection.ToList().ForEach(sel => oids.AddRange(sel.Orders));

			var projectsIDs = ctx.CompanyOrders.Where(w => oids.Contains(w.ID)).Select(s => s.ProjectID).ToList();

			var q = ctx.Articles
				.Join(ctx.Labels, a => a.LabelID, l => l.ID, (art, lbl) => new { Article = art, Label = lbl })
				.Where(w => w.Label.Type == rq.LabelType &&  projectsIDs.Contains(w.Article.ProjectID.Value))
				.Select(s => s.Article);

			return q;
		}

        public IEnumerable<IArticle> GetArticleCanIncludeCompo(RequestLabelType rq)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetArticleCanIncludeCompo(ctx, rq).ToList();
            }
        }

        public IEnumerable<IArticle> GetArticleCanIncludeCompo(PrintDB ctx, RequestLabelType rq)
        {
            // looking for projects identifier
            var oids = new List<int>();

            rq.Selection.ToList().ForEach(sel => oids.AddRange(sel.Orders));

            var projectsIDs = ctx.CompanyOrders.Where(w => oids.Contains(w.ID)).Select(s => s.ProjectID).ToList();

            var q = ctx.Articles
                .Join(ctx.Labels, a => a.LabelID, l => l.ID, (art, lbl) => new { Article = art, Label = lbl })
                .Where(w => w.Label.IncludeComposition && projectsIDs.Contains(w.Article.ProjectID.Value))
                .Select(s => s.Article);

            return q;
        }

        public IEnumerable<ArticleInfoDTO> GetArticlesInfo(int projectID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetArticlesInfo(ctx, projectID).ToList();
            }
        }

        public IEnumerable<ArticleInfoDTO> GetArticlesInfo(PrintDB ctx, int projectID)
        {
            var q = from a in ctx.Articles
                    join categorymap in ctx.Categories on a.CategoryID equals categorymap.ID into Categories
                    from cat in Categories.DefaultIfEmpty()
                    where a.ProjectID.Equals(projectID)
                    select new ArticleInfoDTO()
                    {
                        ArticleID = a.ID,
                        ArticleName = a.Name,
                        ArticleCode = a.ArticleCode,
                        CategoryID = cat != null ? cat.ID : default(int?),
                        CategoryName = cat != null ? cat.Name : null

                    };

            return q;
        }

        public List<ArticleWithLabelDTO> GetArticlesWithLabels(List<ArticleWithLabelDTO> articles,int projectID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetArticlesWithLabels(ctx, articles, projectID);
            }
        }

        public List<ArticleWithLabelDTO> GetArticlesWithLabels(PrintDB ctx, List<ArticleWithLabelDTO> articles, int projectID)
        {

            List<ArticleWithLabelDTO> lst = new List<ArticleWithLabelDTO>();

            for(var i= 0; i< articles.Count(); i++)
            {
                var article = (from a in ctx.Articles
                               join l in ctx.Labels on a.LabelID equals l.ID
                               where a.ArticleCode == articles[i].ArticleCode && a.ProjectID == projectID 
                               && a.EnableAddItems == true
                               select new ArticleWithLabelDTO()
                               {
                                   ID = articles[i].ID,
                                   Name = a.Name,
                                   ArticleCode = a.ArticleCode,
                                   EncodeRFID = l.EncodeRFID,
                                   IncludeComposition = l.IncludeComposition
                               }).FirstOrDefault();

                if(article != null)
                    lst.Add(article);
            }

            return lst;
        }


		public IEnumerable<IArticle> GetByOrder(int orderID) 
		{
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrder(ctx, orderID);
            }
        }
		public IEnumerable<IArticle> GetByOrder(PrintDB ctx, int orderID) 
		{
			var q = ctx.Articles
				.Join(ctx.PrinterJobs, art => art.ID, pj => pj.ArticleID, (art, pj) => new { Article = art, PrinterJob = pj })
				.Where(w => w.PrinterJob.CompanyOrderID == orderID)
				.Select(s => s.Article)
				.AsNoTracking();

			return q.ToList();

		}


        public IEnumerable<ArticleInfoDTO> GetArticlesByCompanyId( int companyId)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
                var articles = from a in ctx.Articles
                               join p in ctx.Projects on a.ProjectID equals p.ID
                               join b in ctx.Brands on p.BrandID equals b.ID
                               join c in ctx.Companies on b.CompanyID equals c.ID
                               where c.ID == companyId  
									
                               orderby a.Name ascending
                               select new ArticleInfoDTO()
                               {
                                   ArticleCode = a.ArticleCode,
                                   ArticleName = a.Name,
                                   BillingCode = a.BillingCode,
                                   ArticleID = a.ID,
                                   CategoryID = a.CategoryID,
								   ProjectID =p.ID, 
								   ProjectName = p.Name,
								   LabelId = a.LabelID
                               };
                return articles.ToList();
            }

        }

        public ArticleCompositionConfig GetArticleCompositionConfig(int projectid, int articleid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return ctx.ArticleCompositionConfigs.FirstOrDefault(f => f.ProjectID == projectid && f.ArticleID == articleid);  
            }
        }

        public void SaveArticleComposition(ArticleCompositionConfig articleCompositionConfig, string usernane)
        {
            if (articleCompositionConfig.ID == 0) 
            {
                AddArticleComposition(articleCompositionConfig, usernane);
            }
            else
            {
                UpdateArticleComposition(articleCompositionConfig, usernane);
            }   
        }
        public void SaveArticleAccessBlockConfig(ArticleAccessBlockConfig config, string username)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var article = ctx.Articles.FirstOrDefault(a => a.ID == config.ArticleID);

                if(article == null)
                    return;

                if(string.IsNullOrWhiteSpace(article.ExportBlockedLocationIds))
                {
                    AddArticleAccessBlockConfig(ctx, article, config, username);
                }
                else
                {
                    UpdateArticleAccessBlockConfig(ctx, article, config, username);
                }
            }
        }

        private void AddArticleAccessBlockConfig(PrintDB ctx, Article article, ArticleAccessBlockConfig config, string username)
        {
            article.ExportBlockedLocationIds = string.IsNullOrWhiteSpace(config.ExportBlockedLocationIds)
                ? "[]"
                : config.ExportBlockedLocationIds;

            article.UpdatedBy = username;
            article.UpdatedDate = DateTime.Now;

            ctx.Update(article);
            ctx.SaveChanges();
        }

        private void UpdateArticleAccessBlockConfig(PrintDB ctx, Article article, ArticleAccessBlockConfig config, string username)
        {
            article.ExportBlockedLocationIds = string.IsNullOrWhiteSpace(config.ExportBlockedLocationIds)
                ? "[]"
                : config.ExportBlockedLocationIds;

            article.UpdatedBy = username;
            article.UpdatedDate = DateTime.Now;

            ctx.Update(article);
            ctx.SaveChanges();
        }




        public List<ArticleCompositionConfig> GetCompositionConfigByProjectID(int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return ctx.ArticleCompositionConfigs.Where(x=>x.ProjectID == projectID).ToList();    
            }
        }

        private void UpdateArticleComposition(ArticleCompositionConfig articleCompositionConfig, string username)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var ac = ctx.ArticleCompositionConfigs.FirstOrDefault(a=>a.ID == articleCompositionConfig.ID);
                if(ac != null) { 
                    ac.UpdatedBy= "system";  
                    ac.UpdatedDate = DateTime.Now; 
                    ac.PPI = articleCompositionConfig.PPI; 
                    ac.DefaultCompresion = articleCompositionConfig.DefaultCompresion;
                    ac.HeightInInches = articleCompositionConfig.HeightInInches; 
                    ac.LineNumber = articleCompositionConfig.LineNumber; 
                    ac.MaxPages = articleCompositionConfig.MaxPages; 
                    ac.MaxLinesToIncludeAdditional = articleCompositionConfig.MaxLinesToIncludeAdditional; 
                    ac.WidthInches = articleCompositionConfig.WidthInches; 
                    ac.WidthAdditionalInInches = articleCompositionConfig.WidthAdditionalInInches; 
                    ac.WithSeparatedPercentage = articleCompositionConfig.WithSeparatedPercentage;
                    ac.ArticleCompositionCalculationType = articleCompositionConfig.ArticleCompositionCalculationType;
                    ac.MaxPages = articleCompositionConfig.MaxPages; 
                    ac.ArticleCode = articleCompositionConfig.ArticleCode; 
                    ac.UpdatedBy = username;
                    ac.UpdatedDate = DateTime.Now;
                    ctx.Update(ac);
                    ctx.SaveChanges();   
                }



            }
        }

        private void AddArticleComposition(ArticleCompositionConfig articleCompositionConfig, string userName)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                articleCompositionConfig.CreatedBy = userName;
                articleCompositionConfig.UpdatedBy = userName;
                articleCompositionConfig.CreatedDate = DateTime.Now;
                articleCompositionConfig.UpdatedDate = DateTime.Now;
                ctx.ArticleCompositionConfigs.Add(articleCompositionConfig);
                ctx.SaveChanges(); 
            }
        }

        public ArticleAccessBlockConfig GetArticleAccessBlockConfig(int articleId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var article = ctx.Articles
                    .AsNoTracking()
                    .FirstOrDefault(a => a.ID == articleId);

                if(article == null)
                    return null;

                return new ArticleAccessBlockConfig
                {
                    ArticleID = article.ID,
                    ProjectID = article.ProjectID,
                    ExportBlockedLocationIds = string.IsNullOrWhiteSpace(article.ExportBlockedLocationIds)
                        ? "[]"
                        : article.ExportBlockedLocationIds
                };
            }
        }

        public bool HasExportAccessBlocked(int articleId, int locationId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var json = ctx.Articles
                    .Where(a => a.ID == articleId)
                    .Select(a => a.ExportBlockedLocationIds)
                    .FirstOrDefault();

                if(string.IsNullOrWhiteSpace(json))
                    return false;

                try
                {
                    var ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(json);
                    return ids != null && ids.Contains(locationId);
                }
                catch
                {
                    return false;
                }
            }
        }


    }



    public enum ArticleTypeFilter
	{
		All,
		Label,
		Item,
		ItemExtra, // item added during validation only
		CareLabel,
		HangTag

	}



	public class ArticleByProjectFilter
	{
		public int ProjectID { get; set; }

		public ArticleTypeFilter ArticleType { get; set; }

		public ArticleByProjectFilter()
		{
			ArticleType = ArticleTypeFilter.All;
		}
	}
}
