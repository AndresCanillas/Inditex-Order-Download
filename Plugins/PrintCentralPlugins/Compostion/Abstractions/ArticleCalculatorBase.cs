using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.PrintCentral;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class ArticleCalculatorBase
    {
        private IPrinterJobRepository printerJobRepo;
        private IFactory factory;
        private INotificationRepository notificationRepo;
        private IArticleRepository articleRepo;
        private string compoCode; 
        public ArticleCalculatorBase(IPrinterJobRepository printerJobRepo, IFactory factory, INotificationRepository notificationRepo, IArticleRepository articleRepo)
        {
            this.printerJobRepo = printerJobRepo;
            this.factory = factory;
            this.notificationRepo = notificationRepo;
            this.articleRepo = articleRepo;
        }

        public class ArticleCalulatorParams
        {
            public ArticleCompositionConfig ArticleCompositionConfig { get; set; }
            public CompositionDefinition Compo { get; set; }
            public int CompoIndex { get; set; }
            public OrderPluginData Od { get; set; }
            public StringBuilder Additionals { get; set; }
            public int AllCompoPages { get; set; }
            public int AllAdditionalPages { get; set; }
            public int Page1_totallines { get; set; }

        }

        public virtual List<ArticleSizeCategory> Calculate(ArticleCalulatorParams parameters)
        {
            IEnumerable<IPrinterJob> printerjob = printerJobRepo.GetByOrderID(parameters.Compo.OrderID, true);
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            foreach(var job in printerjob)
            {
                articleCategoryList = GetArticleCategoryList(parameters.ArticleCompositionConfig,
                                                            parameters.Additionals,
                                                            parameters.AllCompoPages,
                                                            parameters.AllAdditionalPages,
                                                            parameters.Page1_totallines);
                if(articleCategoryList.Count < 1)
                {
                    SendNotificationArticleCodeNotFound(job, parameters.ArticleCompositionConfig.ArticleCode, parameters.Od);
                }
                var newArticle = articleRepo.GetByCodeInProject(articleCategoryList.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault().ArticleCode,
                                                                parameters.Od.ProjectID
                                                               );
                if(newArticle == null)
                {
                    SendNotificationArticleNotFound(job, parameters.ArticleCompositionConfig.ArticleCode, parameters.Od);
                }

            }
            return articleCategoryList;
        }

        private List<ArticleSizeCategory> GetArticleCategoryList(ArticleCompositionConfig article, StringBuilder additionals, int allCompoPages, int allAdditionalPages, int page1_totallines)
        {


            switch(article.ArticleCompositionCalculationType)
            {
                case ArticleCompostionCalculationType.Default:
                    return DefaultCompositionCalculation(article, additionals, allCompoPages, allAdditionalPages, page1_totallines);

                case ArticleCompostionCalculationType.NotIncludeAdditionalWithCompo:
                    return NotIncludeAdditionalWithCompoCalculation(article, additionals, allCompoPages, allAdditionalPages, page1_totallines);

                case ArticleCompostionCalculationType.CanIncludeAdditionalWithCompo:
                    return CanIncludeAdditionalWithCompoCalculation(article, additionals, allCompoPages, allAdditionalPages, page1_totallines);
                default:
                    return null;


            }

        }

        private List<ArticleSizeCategory> CanIncludeAdditionalWithCompoCalculation(ArticleCompositionConfig article, StringBuilder additionals, int allCompoPages, int allAdditionalPages, int page1_totallines)
        {
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            CultureInfo culturaActual = CultureInfo.CurrentCulture;
            string separadorDecimal = culturaActual.NumberFormat.NumberDecimalSeparator;
            
            int PagesSize = 0;
            int sum_pages_compo_addcare = 0;
            compoCode = "";
            sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

            if(additionals.Length == 0 && allCompoPages == 1 && page1_totallines <= article.MaxLinesToIncludeAdditional)
            {
                compoCode = "0";
                articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = article.ArticleCode + "_" + compoCode, PageQuantity = 0 });

            }
            else
            {
                //var result = GetCompoCodeAndPagesSize(sum_pages_compo_addcare);

                if(sum_pages_compo_addcare >= 1 && sum_pages_compo_addcare <= article.MaxPages)
                {
                    compoCode = ((double)(sum_pages_compo_addcare ) / 2).ToString().Replace(separadorDecimal[0], '-');
                    articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = article.ArticleCode + "_" + compoCode, PageQuantity = sum_pages_compo_addcare });
                }

            }
            return articleCategoryList;
        }

        private List<ArticleSizeCategory> NotIncludeAdditionalWithCompoCalculation(ArticleCompositionConfig article, StringBuilder additionals, int allCompoPages, int allAdditionalPages, int page1_totallines)
        {
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            CultureInfo culturaActual = CultureInfo.CurrentCulture;
            string separadorDecimal = culturaActual.NumberFormat.NumberDecimalSeparator;
            
            int PagesSize = 0;
            int sum_pages_compo_addcare = 0;
            compoCode = "";
            sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

            if(sum_pages_compo_addcare >= 1 && sum_pages_compo_addcare <= article.MaxPages)
            {
                compoCode = ((double)(sum_pages_compo_addcare - 1) / 2).ToString().Replace(separadorDecimal[0], '-');
                articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = article.ArticleCode + "_" + compoCode, PageQuantity = sum_pages_compo_addcare - 1 });
            }

            return articleCategoryList;
        }

        private List<ArticleSizeCategory> DefaultCompositionCalculation(ArticleCompositionConfig article, StringBuilder additionals, int allCompoPages, int allAdditionalPages, int page1_totallines)
        {
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = article.ArticleCode, PageQuantity = 1 });
            return articleCategoryList;
        }

        private void SendNotificationArticleNotFound(IPrinterJob job, string articleCode, OrderPluginData od)
        {
            string roles = string.Join(
            Notification.ROLE_SEPARATOR,
            new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

            var title = $"Article not found";
            var message = $"Error when trying to get the article code, check if the article exists";
            var nkey = message.GetHashCode().ToString();
            notificationRepo.AddNotification(
                companyid: job.CompanyID
                , type: NotificationType.OrderTracking
                , intendedRoles: roles
                , intendedUser: null
                , nkey: nkey + job.CompanyOrderID
                , source: "ZaraCompoPlugin"
                , title: title
                , message: message
                , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = articleCode + "_" + compoCode }
                , autoDismiss: false
                , locationID: null
                , projectID: od.ProjectID
                , actionController: null);

            throw new Exception("An error occurred while saving the composition.");
        }

        private void SendNotificationArticleCodeNotFound(IPrinterJob job, string articleCode, OrderPluginData od)
        {
            string roles = string.Join(
                                        Notification.ROLE_SEPARATOR,
                                        new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });
            var title = $"It was not possible to determine the article for the exposed composition";
            var message = $"Error when trying to get the article code, check if the composition is correct for this label";
            var nkey = message.GetHashCode().ToString();
            var userData = factory.GetInstance<IUserData>();
            notificationRepo.AddNotification(
                                            companyid: job.CompanyID
                                            , type: NotificationType.OrderTracking
                                            , intendedRoles: roles
                                            , intendedUser: userData.UserName
                                            , nkey: nkey + job.CompanyOrderID
                                            , source: "CompoPlugin"
                                            , title: title
                                            , message: message
                                            , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = articleCode + "_" + compoCode }
                                            , autoDismiss: false
                                            , locationID: null
                                            , projectID: od.ProjectID
                                            , actionController: null
                                            );

            throw new Exception("An error occurred while saving the composition.");

        }

    }
}
