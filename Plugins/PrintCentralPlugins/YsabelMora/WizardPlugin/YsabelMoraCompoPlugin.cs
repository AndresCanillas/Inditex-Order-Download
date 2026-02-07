using LinqKit;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("YsabelMora.Plugin.CompositionText")]
    [Description("YsabelMora.Plugin.CompositionText")]
    public class YsabelMoraPluginCompositionText : IWizardCompositionPlugin
    {
        private IOrderUtilService orderUtilService;
        private readonly IDBConnectionManager connManager;
        private readonly IArticleRepository articleRepo;
        private readonly ICatalogRepository catalogRepo;
        private readonly IPrinterJobRepository printerJobRepo;
        private readonly INotificationRepository notificationRepo;
        private readonly int MAX_SHOES_FIBERS = 5;
        private readonly int MAX_SHOES_SECTIONS = 3;
        private readonly string EMPTY_CODE = "0";

        string[] SectionsLanguage = { "Spanish", "Bulgarian", "Czech", "English", "French", "German", "Greek", "Hungarian", "Italian", "Polish", "Romanian", "Russian", "Slovak", "Slovenian" };
        string[] FibersLanguage = { "Spanish", "Bulgarian", "English", "French", "Greek", "Hungarian", "Polish", "Italian", "Russian" };

        string[] AdditionalsLanguage = { "Spanish", "English", "French" };
        string[] ExceptionsLanguage = { "Spanish", "French", "French" };

        string[] CareInstructionsLanguage = { "Spanish", "English", "French" };


        public string SECTION_SEPARATOR { get; set; }
        public string SECTION_LANG_SEPARATOR { get; set; }
        public string FIBER_SEPARATOR { get; set; }
        public string FIBER_LANG_SEPARATOR { get; set; }
        public string CI_SEPARATOR { get; set; }
        public string CI_LANG_SEPARATOR { get; set; }

        public YsabelMoraPluginCompositionText(IOrderUtilService orderUtilService, IDBConnectionManager connManager, IArticleRepository articleRepo, ICatalogRepository catalogRepo, IPrinterJobRepository printerJobRepo, INotificationRepository notificationRepo)
        {
            this.orderUtilService = orderUtilService;
            this.connManager = connManager;
            this.articleRepo = articleRepo;
            this.catalogRepo = catalogRepo;
            this.printerJobRepo = printerJobRepo;
            this.notificationRepo = notificationRepo;
        }

        private void InitializceSeparator(IProject projectData)
        {
            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            SECTION_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBER_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            FIBER_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);// always all orders belong to the same project

            InitializceSeparator(projectData);

            var removeDuplicates = projectData.RemoveDuplicateTextFromComposition;

            var ciTemplateCatalog = catalogRepo.GetByName(projectData.ID, Catalog.BRAND_CAREINSTRUCTIONS_TEMPLATES_CATALOG);


            Dictionary<CompoCatalogName, IEnumerable<string>> brandLangs = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            brandLangs.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            brandLangs.Add(CompoCatalogName.FIBERS, FibersLanguage);
            brandLangs.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            brandLangs.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            brandLangs.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);


            // TODO: this loop can be a parallel task, because each of the compositions are independent
            foreach(var od in orderData)
            {
                var composition = orderUtilService.GetComposition(od.OrderGroupID, true, brandLangs, OrderUtilService.LANG_SEPARATOR);

                foreach(var c in composition)
                {
                    var compositionData = new Dictionary<string, string>();

                    // exist a fond called "SIMBOLOS CALZADO.otf" created by IDT designers
                    // 1->A, 2->B, 3->C
                    //StringBuilder ShoesSymbols = new StringBuilder(string.Empty, 15);
                    List<string> ShoesSymbols;
                    var validSymbolsValue = new Dictionary<char, string> { { '1', "A" }, { '2', "B" }, { '3', "C" } };
                    var sb = new StringBuilder();
                    var startIndex = 0;
                    var pageNumber = 0;


                    foreach(var sectionWithFiber in c.Sections.Where(w => !w.IsBlank && w.Fibers != null && w.Fibers.Count > 0))
                    {
                        List<string> langsList;
                        var titleValue = string.Empty;
                        var fiberValue = new List<string>();


                        ShoesSymbols = new List<string>() { "", "", "", "", "" };

                        var index = c.Sections.IndexOf(sectionWithFiber);

                        var sectionsOfPage = c.Sections.ToList().GetRange(startIndex, index - startIndex + 1);// from start to current not blank fibers

                        startIndex = index + 1;

                        var currentText = string.Empty;

                        var titleKey = "title_" + (pageNumber + 1);
                        var fiberKey = "fibers_" + (pageNumber + 1);
                        pageNumber++;

                        foreach(var sop in sectionsOfPage)
                        {
                            langsList = ExtractTranslations(removeDuplicates, sop.AllLangs);

                            if(sop.Code != EMPTY_CODE)
                            {
                                currentText = langsList.Count > 1 ? $"{String.Join(SECTION_LANG_SEPARATOR, langsList)}" : langsList[0];

                                if(sop.IsMainTitle)
                                {
                                    titleValue = currentText;
                                }
                                else
                                {
                                    fiberValue.Add(currentText);
                                }
                            }

                            // loop fibers
                            var fibers = sop.Fibers != null ? sop.Fibers : new List<Fiber>();

                            foreach(var fb in fibers)
                            {
                                langsList = ExtractTranslations(removeDuplicates, fb.AllLangs);

                                if(fb.Code != EMPTY_CODE)
                                {
                                    currentText = langsList.Count > 1 ? $"{String.Join(FIBER_LANG_SEPARATOR, langsList)}" : langsList[0];

                                    fiberValue.Add($"{fb.Percentage}% {currentText}");
                                }

                            }

                        }



                        compositionData.Add(titleKey, titleValue);
                        compositionData.Add(fiberKey, string.Join(FIBER_SEPARATOR, fiberValue));

                    }

                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder(string.Empty, 10);

                    foreach(var ci in c.CareInstructions)
                    {
                        var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                        if(removeDuplicates)
                            langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();


                        Symbols.Append(ci.Symbol);// TODO: now, always use FONT 

                        var translations = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList[0];
                        careInstructions.Append(translations);
                        careInstructions.Append(CI_SEPARATOR);
                    }

                    // Symbols Image from Template selected
                    using(var dynamicDB = connManager.CreateDynamicDB())
                    {
                        var row = dynamicDB.SelectOne(ciTemplateCatalog.CatalogID, c.WrTemplate);

                        compositionData["SymbolsImage"] = row.GetValue("SymbolsImage", string.Empty);
                    }

                    orderUtilService.SaveComposition(projectData.ID, c.ID, compositionData, careInstructions.ToString(), Symbols.ToString());

                    // TODO: set article code: 2 or 3 sheets
                    SetArticleCode(od, pageNumber);

                } // end loop over availables compositions



            }
        }

        private void SetArticleCode(OrderPluginData od, int pageNumber)
        {
            // calculate article code based on number of pages
            var articleCode = "YMO_CLX001";

            // the first sheet is always mandatory for barcode and made in
            if(pageNumber <= 4) articleCode = "YMO_CLX001_3";// label with 2 sheets for compo
            if(pageNumber <= 2) articleCode = "YMO_CLX001_2";// label with 1 sheets for compo
            // +--------------------------------------------------------+
            //
            //  YMO_CLX001_2               YMO_CLX001_3
            //  +-------+ +-------+        +-------+ +-------+ +-------+
            //  | F     | | F     |        | F     | | F     | | F     |
            //  +-------+ +-------+        +-------+ +-------+ +-------+
            //  +-------+ +-------+        +-------+ +-------+ +-------+
            //  | B     | | B     |        | B     | | B     | | B     |
            //  +-------+ +-------+        +-------+ +-------+ +-------+
            //     1      2(compo)         1         2(compo)  3(compo)
            // +--------------------------------------------------------+

            ChangeArticle(od, articleCode);
        }

        public void ChangeArticle(OrderPluginData od, string articleCode)
        {
            IEnumerable<IPrinterJob> printerJobs = printerJobRepo.GetByOrderID(od.OrderID, true);
            IArticle newarticle = null;

            // TODO, complete od data because become whith default value for CompanyID
            od.CompanyID = printerJobs.First().CompanyID;

            newarticle = articleRepo.GetByCodeInProject(articleCode, od.ProjectID);

            if(newarticle == null) HandleArticleNotFoundException(od, articleCode);

            var catalogs = catalogRepo.GetByProjectID(od.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));




            printerJobs.ForEach((job) =>
            {
                //update printdata by articlecode
                printerJobRepo.UpdateArticle(job.ID, newarticle.ID);


                var printerDetails = printerJobRepo.GetJobDetails(job.ID, false);

                //var printerDetails = ctx.PrinterJobDetails
                //    .Join(ctx.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj => ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj })
                //    .Where(w => w.PrinterJob.CompanyOrderID == job.CompanyOrderID)
                //    .Select(s => s.PrinterJobDetail)
                //    .ToList();



                // bulk update
                var allIds = printerDetails.Select(s => s.ProductDataID);

                using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
                {
                    dynamicDB.Conn.ExecuteNonQuery(
                    $@"UPDATE d SET                                       
                                    ArticleCode = @ArticleCode
                                FROM {detailCatalog.TableName} d
                                WHERE d.ID in  ({string.Join(',', allIds)})", newarticle.ArticleCode);
                }


            });



        }

        private void HandleArticleNotFoundException(OrderPluginData od, string articleCode)
        {

            string roles = string.Join(
            Notification.ROLE_SEPARATOR,
            new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

            var title = $"Error Generating Composition Text for {od.OrderNumber} with Article {articleCode}";
            var message = $"Error when trying to get the article code, check if the article exists";
            var nkey = message.GetHashCode().ToString();

            //if article not found throw Exception
            notificationRepo.AddNotification(
                companyid: od.CompanyID
                , type: NotificationType.OrderTracking
                , intendedRoles: roles
                , intendedUser: null
                , nkey: nkey + od.OrderID
                , source: this.GetType().Name
                , title: title
                , message: message
                , data: new { Error = $"Article Code not found to generate composition text", ArticleCode = articleCode }
                , autoDismiss: false
                , locationID: null
                , projectID: od.ProjectID
                , actionController: null);

            throw new ArticleCodeNotFoundException("Article Code not found", articleCode);

        }

        private static List<string> ExtractTranslations(bool removeDuplicates, string allLangs)
        {
            List<string> langsList = allLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(removeDuplicates)
                langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            return langsList;
        }

        public void Dispose()
        {
        }
        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }
        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
        }
        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
    }
}
