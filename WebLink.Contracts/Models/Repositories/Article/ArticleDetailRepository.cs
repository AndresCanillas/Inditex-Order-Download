using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class ArticleDetailRepository : GenericRepository<IArticleDetail, ArticleDetail>,  IArticleDetailRepository
    {
        private readonly IFactory factory;
        private readonly IEventQueue events;
        private readonly IAppConfig config;
        private readonly IDBConnectionManager connManager;
        
        private readonly ILogService log;
        private readonly PrintDB printDB; 

        public ArticleDetailRepository(
                                    IFactory factory,
                                    IEventQueue events,
                                    IAppConfig config,
                                    IDBConnectionManager connManager,
                                    ILogService log) : base(factory, (ctx) => ctx.ArticleDetails)
        {
            this.factory = factory;
            this.events = events;
            this.config = config;
            this.connManager = connManager;
            this.log = log;
            this.printDB = factory.GetInstance<PrintDB>();
        }

        protected override string TableName => "ArticleDetail";

        public ArticleDetailDTO AddArticleDetail(int companyId, int articleId)
        {
            var articleDetail =  printDB.ArticleDetails.FirstOrDefault(a=> a.ArticleID == articleId && a.CompanyID == companyId);
            if (articleDetail !=  null)
            {
                return new ArticleDetailDTO()
                {
                    ID = articleDetail.ID,
                    Article = articleDetail.Article.Name, 
                    ArticleId =  articleDetail.ArticleID, 
                    CompanyId = companyId,
                }; 
            }
            var userData = factory.GetInstance<IUserData>();
            var newArticleDetail = new ArticleDetail() { ArticleID = articleId, 
                                                         CompanyID = companyId,
                                                         CreatedDate = DateTime.Now,
                                                         UpdatedDate = DateTime.Now,
                                                         CreatedBy = userData.UserName, 
                                                         UpdatedBy = userData.UserName};
            printDB.ArticleDetails.Add(newArticleDetail); 
            printDB.SaveChanges();
            var article = printDB.Articles.FirstOrDefault(a => a.ID == newArticleDetail.ArticleID);
            return new ArticleDetailDTO()
            {
                ID = newArticleDetail.ID,
                Article = $"{article.Name}-{article.ArticleCode}",
                ArticleId = newArticleDetail.ArticleID,
                CompanyId = companyId,
            };
        }

        public void Delete(int id)
        {
            var artileDetail = printDB.ArticleDetails.FirstOrDefault(a => a.ID == id);
            if (artileDetail is null) 
                return; 

            printDB.ArticleDetails.Remove(artileDetail);
            printDB.SaveChanges();  
        }

        public void DeleteByCompanyId(int companyId, int providerId )
        {
            using (var transaction = printDB.Database.BeginTransaction())
            {
                printDB.Database.ExecuteSqlCommand($@"DELETE FROM ARTICLEDETAILS WHERE COMPANYID={providerId}
                         AND ARTICLEID IN (SELECT A.ID FROM Articles A 
                         INNER JOIN Projects P ON P.ID = A.ProjectID 
                         INNER JOIN Brands B ON B.ID = P.BrandID 
                         INNER JOIN Companies C ON C.ID = B.CompanyID 
                          WHERE C.ID={companyId})");
                transaction.Commit(); 
            }
        }

        public IEnumerable<IArticleDetail> GetByCompanyId(int companyId)
        {
            return printDB.ArticleDetails.Where(a=>a.CompanyID == companyId);  
        }

        public IArticleDetail GetById(int id)
        {
          return  printDB.ArticleDetails.FirstOrDefault(a=> a.ID == id);
        }

        public IEnumerable<ArticleDetailDTO> GetByProviderId(int providerId, int companyid)
        {
            var articles = from ar in printDB.ArticleDetails
                           join art in printDB.Articles on ar.ArticleID equals art.ID
                           join pr in printDB.Projects on art.ProjectID equals pr.ID
                           join br in printDB.Brands on pr.BrandID equals br.ID
                           join co in printDB.Companies on br.CompanyID equals co.ID
                           where (co.ID == companyid)
                           where (ar.CompanyID == providerId)
                           where ar.CompanyID == providerId
                           select (new ArticleDetailDTO()
                           {
                               ID = ar.ID,
                               CompanyId = ar.CompanyID,
                               ArticleId = ar.ArticleID,
                               Article = $"{art.Name}-{art.ArticleCode}"
                           });  
            return articles.ToList();
        }

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, ArticleDetail actual, IArticleDetail data)
        {
            
        }
    }
}
