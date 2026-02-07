using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using Microsoft.EntityFrameworkCore;
using Service.Contracts.Authentication;

namespace WebLink.Contracts.Models
{
	public class PackRepository: GenericRepository<IPack, Pack>, IPackRepository
	{
		public PackRepository(IFactory factory)
			: base(factory, (ctx) => ctx.Packs)
		{
		}


		protected override string TableName { get => "Packs"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Pack actual, IPack data)
		{
			actual.ProjectID = data.ProjectID;
			actual.Name = data.Name;
			actual.Description = data.Description;
			if(userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService))
			{
				actual.PackCode = data.PackCode;
			}
		}


		public List<IPack> GetByProjectID(int projectid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByProjectID(ctx, projectid);
			}
		}


		public List<IPack> GetByProjectID(PrintDB ctx, int projectid)
		{
			return new List<IPack>(
				All(ctx).Where(p => p.ProjectID == projectid)
			);
		}


		public IPack GetByCodeInProject(string code, int projectID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCodeInProject(ctx, code, projectID);
			}
		}


		public IPack GetByCodeInProject(PrintDB ctx, string code, int projectID)
		{
			return ctx.Packs.Where(w => w.PackCode == code && w.ProjectID == projectID)
				.AsNoTracking()
				.FirstOrDefault();
		}


		public List<IPackArticle> GetPackArticles(int packid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetPackArticles(ctx, packid);
			}
		}


		public List<IPackArticle> GetPackArticles(PrintDB ctx, int packid)
		{
			var packArticles = ctx.PackArticles
				.Where(p => p.PackID == packid)
				.Include(p => p.Article)
				.AsNoTracking()
				.ToList();

			return new List<IPackArticle>(packArticles);
		}


		public List<IPackArticle> GetPackArticlesByType(int packid, PackArticleType itemType)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetPackArticlesByType(ctx, packid, itemType);
			}
		}


		public List<IPackArticle> GetPackArticlesByType(PrintDB ctx, int packid, PackArticleType itemType)
		{
			var packArticles = ctx.PackArticles
				.Where(p => p.PackID == packid && p.Type == itemType)
				.Include(p => p.Article)
				.AsNoTracking()
				.ToList();

			return new List<IPackArticle>(packArticles);
		}


		public PackArticleViewModel AddArticleToPack(int packid, int articleid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return AddArticleToPack(ctx, packid, articleid);
			}
		}


		public PackArticleViewModel AddArticleToPack(PrintDB ctx, int packid, int articleid)
		{
			// ENSURE use has access to both objects, also need to use IoC here to avoid ciclic dependency between ArticleRep and PackRepo
			var articleRepo = factory.GetInstance<IArticleRepository>();
			var pack = GetByID(ctx, packid);
			var article = articleRepo.GetByID(ctx, articleid);
			// Add
			ctx.PackArticles.Add(new PackArticle() { PackID = pack.ID, ArticleID = article.ID, Type = PackArticleType.ByArticle });
			ctx.SaveChanges();

			return (from a in ctx.Articles
					join pa in ctx.PackArticles on a.ID equals pa.ArticleID
					where (pa.PackID == packid && pa.ArticleID == article.ID) || (pa.Type == PackArticleType.ByOrderData && pa.ArticleID == null)
					select new PackArticleViewModel { PackID = pa.PackID, ArticleID = a.ID, Name = a.Name, ArticleCode = a.ArticleCode, Quantity = pa.Quantity,
                                        Type = pa.Type, Catalog = pa.Catalog.Name, FieldName = pa.FieldName, Mapping = pa.Mapping, PluginName = pa.PluginName } ).FirstOrDefault();
		}

        public PackArticleViewModel AddArticleByData(int id, int packid, int projectId, string field, string mapping, bool allowEmptyValues)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddArticleByData(ctx, id, packid, projectId, field, mapping, allowEmptyValues);
            }
        }


        public PackArticleViewModel AddArticleByData(PrintDB ctx, int id, int packid, int projectId, string field, string mapping, bool allowEmptyValues)
        {
            var catalogRepo = factory.GetInstance<ICatalogRepository>();
            var pack = GetByID(ctx, packid);
            var catalog = catalogRepo.GetByProjectID(ctx, projectId).FirstOrDefault(x => x.Name.Equals("VariableData"));

            //Add or update
            if (id != 0)
            {
                var data = ctx.PackArticles.FirstOrDefault(x => x.ID == id);
                data.Mapping = mapping;
                data.FieldName = field;
                data.AllowEmptyValues = allowEmptyValues;
                ctx.PackArticles.Update(data);
            }
            else            
                ctx.PackArticles.Add(new PackArticle() { 
                    PackID = pack.ID, 
                    CatalogID = catalog.ID, 
                    FieldName = field, 
                    Mapping = mapping, 
                    Type = PackArticleType.ByOrderData,
                    AllowEmptyValues = allowEmptyValues
                });
            
            ctx.SaveChanges();

            return (from pa in ctx.PackArticles
                    join a in ctx.Articles on pa.ArticleID equals a.ID into paj
                    from s in paj.DefaultIfEmpty()
                    where (id != 0 ? (pa.ID == id) : (pa.PackID == packid && pa.Type == PackArticleType.ByOrderData && pa.ArticleID == null && pa.FieldName == field))
                    orderby pa.ID descending
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
                        Type = pa.Type
                    }).FirstOrDefault();
        }

        public PackArticleViewModel AddArticleByPlugin(int packid, int projectId, string pluginName)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddArticleByPlugin(ctx, packid, projectId, pluginName);
            }
        }


        public PackArticleViewModel AddArticleByPlugin(PrintDB ctx, int packid, int projectId, string pluginName)
        {
            // ENSURE use has access to both objects, also need to use IoC here to avoid ciclic dependency between ArticleRep and PackRepo
            var articleRepo = factory.GetInstance<IArticleRepository>();
            var pack = GetByID(ctx, packid);
            // Add
            ctx.PackArticles.Add(new PackArticle() { PackID = pack.ID, Type = PackArticleType.ByPlugin, PluginName = pluginName });
            ctx.SaveChanges();

            return (from pa in ctx.PackArticles
                       join a in ctx.Articles on pa.ArticleID equals a.ID into paj
                       from s in paj.DefaultIfEmpty()
                       where (pa.PackID == packid && pa.Type == PackArticleType.ByPlugin && pa.ArticleID == null && pa.PluginName == pluginName)
                    select new PackArticleViewModel
                    {
                        PackID = pa.PackID,
                        Name = pa.Article.Name,
                        ArticleCode = pa.Article.ArticleCode,
                        Quantity = pa.Quantity,
                        Type = pa.Type,
                        Catalog = pa.Catalog.Name,
                        PluginName = pa.PluginName
                    }).FirstOrDefault();
        }


        public void RemoveArticleFromPack(int packid, int articleid, int id)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				RemoveArticleFromPack(ctx, packid, articleid, id);
			}
		}


		public void RemoveArticleFromPack(PrintDB ctx, int packid, int articleid, int id)
		{
			var articleRepo = factory.GetInstance<IArticleRepository>();
			var pack = GetByID(ctx, packid);
            var article = new Article();

            if (articleid != 0)
                article = (Article)articleRepo.GetByID(ctx, articleid);

            // remove from pack
            var packArticle = ctx.PackArticles.Where(p => id != 0 ? ( p.ID == id ) : ( p.PackID == pack.ID && p.ArticleID == article.ID ) ).FirstOrDefault();
			if (packArticle != null)
			{
				ctx.PackArticles.Remove(packArticle);
				ctx.SaveChanges();
			}
		}


		public void UpadtePackArticle(int packId, int articleId, int quantity, int id)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				UpadtePackArticle(ctx, packId, articleId, quantity, id);
			}
        }


		public void UpadtePackArticle(PrintDB ctx, int packId, int articleId, int quantity, int id)
		{
			var row = ctx.PackArticles.FirstOrDefault(x => id != 0 ? ( x.ID == id ) : ( x.PackID == packId && x.ArticleID == articleId ));
			row.Quantity = quantity;
			ctx.SaveChanges();
		}

        public PackArticleViewModel GetPackArticle(int packId, int articleId)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetPackArticle(ctx, packId, articleId);
            }
        }


        public PackArticleViewModel GetPackArticle(PrintDB ctx, int packId, int articleId)
        {
            return (from a in ctx.Articles
                    join pa in ctx.PackArticles on a.ID equals pa.ArticleID
                    where pa.PackID == packId && pa.ArticleID == articleId
                    select new PackArticleViewModel
                    {
                        PackID = pa.PackID,
                        ArticleID = a.ID,
                        Name = a.Name,
                        ArticleCode = a.ArticleCode,
                        Quantity = pa.Quantity,
                        Type = pa.Type,
                        Catalog = pa.Catalog.Name,
                        FieldName = pa.FieldName,
                        Mapping = pa.Mapping,
                        PluginName = pa.PluginName
                    }).FirstOrDefault();
        }

        public PackArticleViewModel GetPackArticleById(int id)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetPackArticleById(ctx, id);
            }
        }


        public PackArticleViewModel GetPackArticleById(PrintDB ctx, int id)
        {
            return (from pa in ctx.PackArticles
                    join a in ctx.Articles on pa.ArticleID equals a.ID into paj
                    from s in paj.DefaultIfEmpty()
                    where pa.ID == id && pa.Type == PackArticleType.ByOrderData && pa.ArticleID == null
                    orderby pa.ID descending
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
                        AllowEmptyValues = pa.AllowEmptyValues,
                    }).FirstOrDefault();
        }


        public IEnumerable<IPackArticle> GetPackArticlesConfig(int packId)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return ctx.PackArticles
				.Where(w => w.PackID.Equals(packId))
				.ToList();
			}
		}


		public PackArticleConfigDTO GetPackArticleConfig(string packCode, int articleID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetPackArticleConfig(ctx, packCode, articleID);
			}
		}


		public PackArticleConfigDTO GetPackArticleConfig(PrintDB ctx, string packCode, int articleID)
		{
			return ctx.PackArticles
				.Include(p => p.Pack)
				.Where(w => w.Pack.PackCode.Equals(packCode) && w.ArticleID.Equals(articleID))
				.Select(s => new PackArticleConfigDTO()
				{
					PackID = s.PackID,
					PackCode = s.Pack.PackCode,
					ArticleID = s.ArticleID,
					ArticleCode = s.Article.ArticleCode,
					Quantity = s.Quantity,
				})
				.FirstOrDefault();
		}
    }
}
