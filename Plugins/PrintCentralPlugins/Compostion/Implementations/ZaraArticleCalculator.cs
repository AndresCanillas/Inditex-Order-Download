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
    public class ZaraArticleCalculator
    {
        private IPrinterJobRepository printerJobRepo;
        private IFactory factory;
        private INotificationRepository notificationRepo;
        private IArticleRepository articleRepo;

        public ZaraArticleCalculator(IPrinterJobRepository printerJobRepo, IFactory factory, INotificationRepository notificationRepo, IArticleRepository articleRepo)
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

            switch(articleCode)
            {
                case "CLZCALL001SUR":
                case "CLZCALL001":

                case "D-CLZCALL001SUR":
                case "D-CLZCALL001":
                    if(additionals.Length == 0 && allCompoPages == 1 && page1_totallines <= 5)
                    {
                        articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = articleCode + "_" + "0", PageQuantity = 0 });
                    }
                    else
                    {
                        if(allCompoPages <= 1 && allCompoPages <= 12)
                        {
                            compoCode = ((double)allCompoPages / 2).ToString().Replace(separadorDecimal[0], '-');
                            articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = articleCode + "_" + compoCode, PageQuantity = allCompoPages });
                        }
                        else
                        {
                            articleCategoryList.Add(new ArticleSizeCategory { ArticleCode = articleCode + "_0", PageQuantity = 0 });
                        }
                    }
                    break;
                case "CLZCALL027":
                case "CLZCALL027SUR":
                case "CLZCALL028":
                case "CLZCALL028SUR":
                case "CLZCALL029":
                case "CLZCALL029SUR":
                case "CLZCALL030":
                case "CLZCALL030SUR":
                case "CLZCALL031":
                case "CLZCALL031SUR":
                case "CLZCALL032":
                case "CLZCALL032SUR":
                case "CLZCALL033":
                case "CLZCALL033SUR":

                case "D-CLZCALL027":
                case "D-CLZCALL027SUR":
                case "D-CLZCALL028":
                case "D-CLZCALL028SUR":
                case "D-CLZCALL029":
                case "D-CLZCALL029SUR":
                case "D-CLZCALL030":
                case "D-CLZCALL030SUR":
                case "D-CLZCALL031":
                case "D-CLZCALL031SUR":
                case "D-CLZCALL032":
                case "D-CLZCALL032SUR":
                case "D-CLZCALL033":
                case "D-CLZCALL033SUR":
                case "CLZCALL020":
                case "CLZCALL020SUR":
                case "CLZCALL021":
                case "CLZCALL021SUR":
                case "CLZCALL022":
                case "CLZCALL022SUR":
                case "CLZCALL023":
                case "CLZCALL023SUR":
                case "CLZCALL024":
                case "CLZCALL024SUR":
                case "CLZCALL025":
                case "CLZCALL025SUR":
                case "CLZCALL026":
                case "CLZCALL026SUR":

                case "D-CLZCALL020":
                case "D-CLZCALL020SUR":
                case "D-CLZCALL021":
                case "D-CLZCALL021SUR":
                case "D-CLZCALL022":
                case "D-CLZCALL022SUR":
                case "D-CLZCALL023":
                case "D-CLZCALL023SUR":
                case "D-CLZCALL024":
                case "D-CLZCALL024SUR":
                case "D-CLZCALL025":
                case "D-CLZCALL025SUR":
                case "D-CLZCALL026":
                case "D-CLZCALL026SUR":
                    compoCode = "";
                    var sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

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
