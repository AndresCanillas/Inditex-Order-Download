using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
	public class ArticleTrackingRepository : IArticleTrackingRepository
	{
		private readonly PrintDB printDB;
		private IDBConnectionManager connManager;

		public ArticleTrackingRepository(
			IFactory factory,
			IDBConnectionManager connManager
		)
		{
			this.printDB = factory.GetInstance<PrintDB>();
			this.connManager = connManager;
		}

		public IArticleTracking GetById(int id)
		{
			return printDB.ArticleTracking.FirstOrDefault(a => a.ID == id);
		}

		public List<ArticleTrackingInfo> GetTrackedArticles(int page, int pageSize, string search, int factoryID)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 20;

			using (var conn = connManager.OpenWebLinkDB())
			{
				return conn.Select<ArticleTrackingInfo>($@"
					select t.ArticleID, a.ArticleCode, c.Name companyName, b.Name brandName,
					p.Name projectName,t.InitialDate, a.Description, t.ID, 
					(select isnull(sum(o.Quantity),0) from CompanyOrders o
						join PrinterJobs j on j.CompanyOrderID = o.ID
						where j.ArticleID = t.articleID
						and o.OrderDate > t.InitialDate
						and o.OrderStatus in (3,6,30,40,50)
                        and (o.LocationID = {factoryID} or {factoryID}=0) 
					) quantity
					from ArticleTracking t
					left join Articles a on a.ID = t.ArticleID
					left join Projects p on p.ID = a.ProjectID
					left join Brands b on b.ID = p.BrandID
					left join Companies c on c.ID = b.CompanyID
					where articleCode + c.Name + b.Name + p.Name like '%{search}%'
					order by ArticleID OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY;
				").ToList();
			}
		}
		public int GetTrackedArticlesCount(string search)
		{
			using (var conn = connManager.OpenWebLinkDB())
			{
				return (int)conn.ExecuteScalar($@"
					select count(*) from (
						select t.ArticleID
						from ArticleTracking t
						left join Articles a on a.ID = t.ArticleID
						left join Projects p on p.ID = a.ProjectID
						left join Brands b on b.ID = p.BrandID
						left join Companies c on c.ID = b.CompanyID
						where articleCode + c.Name + b.Name + p.Name like '%{search}%'
					) lineas 
				");
			}
		}

		public List<ArticleTrackingInfo> GetUntrackedArticles(int page, int pageSize, string search)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 20;

			using (var conn = connManager.OpenWebLinkDB())
			{
				return conn.Select<ArticleTrackingInfo>($@"
					select  
					c.Name CompanyName, b.Name BrandName, j.Name ProjectName,
					a.id ArticleID, a.ArticleCode ArticleCode, a.Description Description
					from Articles a
					join Projects j on j.ID = a.ProjectID
					join Brands b on b.ID = j.BrandID
					join Companies c on c.ID = b.CompanyID
					where a.ArticleCode+b.Name+c.Name+j.Name like '%{search}%'
					and a.ID not in (select z.ArticleID from ArticleTracking z)
					order by a.id OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY;
				").ToList();
			}
		}

		public int GetUntrackedArticlesCount(string search)
		{
			using (var conn = connManager.OpenWebLinkDB())
			{
				return (int)conn.ExecuteScalar($@"
					select count(*) from
					(
					select  
					c.Name CompanyName, b.Name BrandName, j.Name ProjectName,
					a.id ArticleID, a.ArticleCode ArticleCode, a.Description Description
					from Articles a
					join Projects j on j.ID = a.ProjectID
					join Brands b on b.ID = j.BrandID
					join Companies c on c.ID = b.CompanyID
					where a.ArticleCode+b.Name+c.Name+j.Name like '%{search}%'
					and a.ID not in (select z.ArticleID from ArticleTracking z)
					)  lineas
				");
			}
		}

		public void AddArticle(int articleId, DateTime initialDate, string username)
		{
			using (var conn = connManager.OpenWebLinkDB())
			{
				var articleTracking = new ArticleTracking
				{
					ArticleID = articleId,
					InitialDate = initialDate,
					LastUpdateUserName = username
				};

				conn.Insert<ArticleTracking>(articleTracking);
			}
		}

		public void RemoveArticle(int articleId)
		{
			using (var conn = connManager.OpenWebLinkDB())
			{
				conn.Delete<ArticleTracking>(articleId);
			}
		}

		public void ResetDate(int articleId, DateTime initialDate, string username)
		{
			using (var conn = connManager.OpenWebLinkDB())
			{
				var article = conn.SelectOne<ArticleTracking>(articleId);
				article.InitialDate = initialDate;
				article.LastUpdateUserName = username;
				conn.Update<ArticleTracking>(article);
			}
		}
	}
}

