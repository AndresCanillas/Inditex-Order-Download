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

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class BershkaArticleCalculator
    {
        private IPrinterJobRepository printerJobRepo;
        private IFactory factory;
        private INotificationRepository notificationRepo;
        private IArticleRepository articleRepo;

        public BershkaArticleCalculator(IPrinterJobRepository printerJobRepo, IFactory factory, INotificationRepository notificationRepo, IArticleRepository articleRepo)
        {
            this.printerJobRepo = printerJobRepo;
            this.factory = factory;
            this.notificationRepo = notificationRepo;
            this.articleRepo = articleRepo;
        }

        public List<ArticleSizeCategory> Calculate(string articleCode,
                                        CompositionDefinition compo,
                                        int compoIndex,
                                        OrderPluginData od,
                                        StringBuilder additionals,
                                        int allCompoPages,
                                        int allAdditionalPages,
                                        int page1_totallines)
        {
            IEnumerable<IPrinterJob> printerjob = printerJobRepo.GetByOrderID(compo.OrderID, true);
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            foreach(var job in printerjob)
            {
                articleCategoryList = GetArticleCategoryList(articleCode, additionals, allCompoPages, allAdditionalPages, page1_totallines);
                if(articleCategoryList.Count < 1)
                {
                    SendNotificationArticleCodeNotFound(job, articleCode, od, "0");
                }
                var newArticle = articleRepo.GetByCodeInProject(articleCategoryList.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault().ArticleCode,
                                                                od.ProjectID
                                                               );
                if(newArticle == null)
                {
                    SendNotificationArticleNotFound(job, articleCode, od, "0");
                }

            }
            return articleCategoryList;
        }

        private void SendNotificationArticleNotFound(IPrinterJob job, string articleCode, OrderPluginData od, string compoCode)
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

        private void SendNotificationArticleCodeNotFound(IPrinterJob job, string articleCode, OrderPluginData od, string compoCode)
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

        private List<ArticleSizeCategory> GetArticleCategoryList(string articleCode, StringBuilder additionals, int allCompoPages, int allAdditionalPages, int page1_totallines)
        {
            List<ArticleSizeCategory> articleCategoryList = new List<ArticleSizeCategory>();
            CultureInfo culturaActual = CultureInfo.CurrentCulture;
            string separadorDecimal = culturaActual.NumberFormat.NumberDecimalSeparator;
            string compoCode;
            int sum_pages_compo_addcare = 0;
            int PagesSize = 0;

            switch(articleCode)
            {
                case "COMPO-BLACK":
                case "COMPO-WHITE":

                    compoCode = "";
                    sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

                    // TODO: use formula: compoCode ((sum_pages_compo_addcare - 1 )/ 2).ToString("0.0")
                    // pageSize Math.Floor(Convert.ToDecimal(compoCode) * 2)

                    compoCode = "";
                    sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

                    if(sum_pages_compo_addcare >= 1 && sum_pages_compo_addcare <= 13)
                    {
                        compoCode = ((double)(sum_pages_compo_addcare - 1) / 2).ToString().Replace(separadorDecimal[0], '-');
                        articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = articleCode + "_" + compoCode, PageQuantity = sum_pages_compo_addcare - 1 });
                    }
                    break;


            }
            return articleCategoryList;
        }
    }
}
