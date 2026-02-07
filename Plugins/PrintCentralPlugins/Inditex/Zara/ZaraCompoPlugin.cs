//using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
//using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
//using static System.Runtime.CompilerServices.RuntimeHelpers;
using LinqKit;
//using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.CodeAnalysis;
using Service.Contracts;
using Service.Contracts.Authentication;
//using System.Drawing.Imaging;
//using System.IO;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
//using Service.Contracts.PrintServices.PrintCentral.OrderPlugins;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
//using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.WizardPlugins
{
    [FriendlyName("Zara2 - Composition Text Plugin")]
    [Description("Inditex.Zara - Composition Plugin")]
    public class ZaraCompoPlugin : IWizardCompositionPlugin
    {
        private IEventQueue events;
        private ILogSection log;
        private IUserData userData;
        private IOrderUtilService orderUtilService;
        private readonly int MAX_SHOES_FIBERS = 5;
        private readonly int MAX_SHOES_SECTIONS = 3;
        private IPrinterJobRepository printerJobRepo;
        private IArticleRepository articleRepo;
        private INotificationRepository notificationRepo;
        private IDBConnectionManager connManager;
        private ICatalogRepository catalogRepo;
        private IFactory factory;
        private IUserManager userManager;
        private IOrderRepository orderRepo;
        private List<PluginCompoPreviewData> PluginCompoPreviewData { get; set; }

        protected class ArticleConfig
        {
            public float HeightInInches { get; set; }
            public float WidthInches { get; set; } = 0;
            public int LineNumber { get; set; }
            public bool WithSeparatedPercentage = true;

        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            var od = orderData.FirstOrDefault();
            var ZaraLanguage = GetZaraLanguageDictionary();

            if (isLoad)
            {
                var composition = orderUtilService
                            .GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR)
                            .OrderBy(c => c.ID).FirstOrDefault(c=>c.ID==id);
                var compositionData = new Dictionary<string, string>();
                if (composition != null)
                {
                    compositionData = orderUtilService.GetCompositionData(orderData.FirstOrDefault().ProjectID, composition.ID);
                    if (compositionData != null)
                    {
                        var article = GetArticleCode(od.OrderID);
                        var articleConfig = GetArticleConfig(article);

                        return new List<PluginCompoPreviewData>() { new PluginCompoPreviewData()
                    {
                        Lines = articleConfig.LineNumber,
                        Width = articleConfig.WidthInches,
                        Heigth= articleConfig.HeightInInches,
                        CompoData = compositionData,
                        AdditionalsCompress = GetAdditionalsCompress(compositionData), 
                        FiberCompress = GetFiberCompress(compositionData), 
                        CompositionText = GetCompostionText( compositionData, composition, articleConfig.WithSeparatedPercentage),
                        ExceptionsLocation = GetExceptionLocation (compositionData)

                    }};
                    }

                }
            }
            GenerateCompositionTextByCompoId(orderData, id);
            return PluginCompoPreviewData;
        }

        private int GetExceptionLocation(Dictionary<string, string> compositionData)
        {
            int exceptionsLocation = 0; 
            if (compositionData.TryGetValue("ExceptionsLocation", out string value))
            {
                int.TryParse(value, out exceptionsLocation);
            }
            return exceptionsLocation; 
        }

        private int GetFiberCompress(Dictionary<string, string> compositionData)
        {
            int fiberCompress = 0;  
            if (compositionData.TryGetValue("FiberCompress", out string value))
            {
                 int.TryParse(value, out fiberCompress);    
            }

            return fiberCompress; 

        }

        private int GetAdditionalsCompress(Dictionary<string, string> compositionData)
        {
            int additionalCompress = 0;
            if (compositionData.TryGetValue("AdditionalCompress", out string value))
            {
                int.TryParse (value, out additionalCompress);
            }
            return additionalCompress;
        }

        private List<PluginCompositionTextPreviewData> GetCompostionText(Dictionary<string, string> compositionData, CompositionDefinition composition, bool separatePercent = true)
        {
            var output = new List<PluginCompositionTextPreviewData>();

            composition.Sections.ForEach(s =>
            {
                s.Fibers.ForEach(f =>
                {
                    var fiber = new PluginCompositionTextPreviewData()
                    {
                        FiberType = f.FiberType,
                        Percent = separatePercent ? f.Percentage : string.Empty,

                    };
                });
            });

            return output;
        }

        string[] SectionsLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN" };
        string[] FibersLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN" };

        string[] AdditionalsLanguage = { "Spanish", "English", "French", "Portuguese", "Polish", "Romanian", "Indonesian", "Arabic", "Galician", "Catalan", "Basque" };
        string[] ExceptionsLanguage = { "Spanish", "French", "English", "Portuguese", "Dutch", "Italian", "Greek", "Japanese", "German", "Danish", "Slovenian", "Chinese", "Korean", "Indonesian", "Arabic", "Galician", "Catalan", "Basque", "SLOVAK", "CROATIAN" };

        string[] CareInstructionsLanguage = { "Spanish", "English", "Portuguese" };

        List<int> OrdersToClone = new List<int>();

        public Font font;
        public Graphics g;
        public Bitmap bmp;
        int totalCompositions = 0;
        int page1_totallines = 0;

        List<ArticleSizeCategory> ArticleCategoryLst = new List<ArticleSizeCategory>();

        //public string SECTION_SEPARATOR {
        //    get {
        //        return ProjectData == null || string.IsNullOrEmpty(ProjectData.SectionsSeparator) ? "\n" : ProjectData.SectionsSeparator;
        //    }
        //}


        public string SECTION_SEPARATOR { get; set; }
        public string SECTION_LANG_SEPARATOR { get; set; }
        public string FIBER_SEPARATOR { get; set; }
        public string FIBER_LANG_SEPARATOR { get; set; }
        public string CI_SEPARATOR { get; set; }
        public string CI_LANG_SEPARATOR { get; set; }


        public ZaraCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService, IPrinterJobRepository printerJobRepo, IArticleRepository articleRepo, INotificationRepository notificationRepo, IDBConnectionManager connManager, ICatalogRepository catalogRepo, IFactory factory, IUserManager userManager, IOrderRepository orderRepo, IUserData userData)
        {
            this.events = events;
            this.log = log.GetSection("Zara - CompoPlugin");
            this.orderUtilService = orderUtilService;
            this.printerJobRepo = printerJobRepo;
            this.articleRepo = articleRepo;
            this.notificationRepo = notificationRepo;
            this.connManager = connManager;
            this.catalogRepo = catalogRepo;
            this.factory = factory;
            this.userManager = userManager;
            this.orderRepo = orderRepo;
            this.userData = userData;
            this.PluginCompoPreviewData = new List<PluginCompoPreviewData>();
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

        private void GenerateCompositionTextByCompoId (List<OrderPluginData> orderData, int id)
        {
            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            int indexCompo = 0;
            float widthInInches = 0;
            float heightInInches = 0;
            float widthAdditionalInInches = 0;

            InitializceSeparator(projectData);

            
            Dictionary<CompoCatalogName, IEnumerable<string>> ZaraLanguage = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            ZaraLanguage.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.FIBERS, FibersLanguage);
            ZaraLanguage.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);
            List<PluginCompositionTextPreviewData> CompositionText = new List<PluginCompositionTextPreviewData>();
            List<PluginCompositionTextPreviewData> CareInstructionsText = new List<PluginCompositionTextPreviewData>();

            //configuration
            bmp = new Bitmap(100, 100);
            g = Graphics.FromImage(bmp);

            //create list fiber per composition
            foreach (var od in orderData)
            {
                var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR).Where(c=> c.ID==id);

                totalCompositions = compositions.Count();

                IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);



                foreach (var compo in compositions)
                {
                    bool withSeparatedPercentage = true;
                    RibbonFace materialsFaces = new RibbonFace();
                    RibbonFace materialsFaces2 = new RibbonFace();
                    List<CompositionTextDTO> listfibers = new List<CompositionTextDTO>();
                    List<CompositionTextDTO> listcareinstructions = new List<CompositionTextDTO>();

                    // add care instructions
                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder additionals = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder();

                    int total_compo_pages = 0;
                    int total_addcare_pages = 0;
                    string componumber;
                    string additionalnumber;
                    int allowedLinesByPage = 0;
                    bool IsSimpleAdditional = true;
                    int totalPages = 10;

                    var compositionData = new Dictionary<string, string>();

                    IArticle currentArticle;
                    string artCode = string.Empty;

                    foreach (var job in printer_job)
                    {
                        currentArticle = articleRepo.GetByID(job.ArticleID);
                        artCode = currentArticle.ArticleCode.Contains("_") ? currentArticle.ArticleCode.Substring(0, currentArticle.ArticleCode.LastIndexOf("_")) : currentArticle.ArticleCode;
                    }

                    //get list Fibers
                    //listfibers = CreateFiberList(compo, od.FillingWeightId, od.FillingWeightText);
                    //listcareinstructions = CreateCareInstructionsList(compo);

                    //CompositionText = new List<PluginCompositionTextPreviewData>();
                    //foreach (var fiber in listfibers)
                    //{
                    //    CompositionText.Add(new PluginCompositionTextPreviewData()
                    //    {
                    //        FiberType = string.IsNullOrEmpty(fiber.Percent) && string.IsNullOrEmpty(fiber.FiberType) ? "TITLE" : fiber.FiberType,
                    //        Langs = fiber.Langs,
                    //        Percent = fiber.Percent,
                    //        Text = fiber.Text,
                    //        IsTitle = fiber.TextType == TextType.Title, 
                    //        SectionFibersText   = fiber.SectionFibersText,
                    //    });
                    //}
                    //CareInstructionsText = new List<PluginCompositionTextPreviewData>();
                    //foreach(var ciText in listcareinstructions)
                    //{
                    //    CareInstructionsText.Add(new PluginCompositionTextPreviewData
                    //    {
                    //        FiberType = ciText.FiberType,
                    //        Langs = ciText.Langs,
                    //        Percent = ciText.Percent,
                    //        Text = ciText.Text,
                    //        IsTitle = false

                    //    });
                    //}



                    switch (artCode)
                    {
                        case "D-CLZCALL001SUR":
                        case "CLZCALL001SUR":
                        case "D-CLZCALL001":
                        case "CLZCALL001":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta                          
                            materialsFaces = new RibbonFace(font, 3.7700f, 0.9374f);
                            heightInInches = 0.9374f;
                            widthInInches = 3.7700f;
                            allowedLinesByPage = 9;
                            IsSimpleAdditional = true;

                            break;


                        case "XXXX - 60x25":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.8503f, 0.9634f);  //46x23mm compo
                            heightInInches = 0.9634f;
                            widthInInches = 1.8503f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9685f, 0.9055f);  //50x23mm adicionales V (Cristina)
                            //CalculateCompositionSmall(compo, indexCompo, sectionLanguageSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator, od, orderData, materialsFaces, artCode,9);
                            allowedLinesByPage = 10;
                            IsSimpleAdditional = false;
                            widthAdditionalInInches = 1.9685f;

                            break;

                        case "XXXX-ALL 60x40":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                            materialsFaces = new RibbonFace(font, 1.9732f, 1.4350f);  //46x36.45mm compo
                            heightInInches = 1.4350f;
                            widthInInches = 1.9732f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.4440f);  //50.16x36.68mm adicionales V (Eric)
                            allowedLinesByPage = 15;
                            IsSimpleAdditional = false;
                            widthAdditionalInInches = 1.9748f;

                            break;



                        // CORRECION MEDIDAS 2023-10
                        // 60x40
                        case "CLZCALL021":
                        case "CLZCALL021SUR":
                        case "D-CLZCALL021":
                        case "D-CLZCALL021SUR":
                        case "CLZCALL023":
                        case "CLZCALL023SUR":
                        case "D-CLZCALL023":
                        case "D-CLZCALL023SUR":
                        case "CLZCALL024":
                        case "CLZCALL024SUR":
                        case "D-CLZCALL024":
                        case "D-CLZCALL024SUR":

                        case "CLZCALL020":
                        case "CLZCALL020SUR":
                        case "CLZCALL022":
                        case "CLZCALL022SUR":
                        case "CLZCALL025":
                        case "CLZCALL025SUR":
                        case "CLZCALL026":
                        case "CLZCALL026SUR":
                        case "D-CLZCALL020":
                        case "D-CLZCALL020SUR":
                        case "D-CLZCALL022":
                        case "D-CLZCALL022SUR":
                        case "D-CLZCALL025":
                        case "D-CLZCALL025SUR":
                        case "D-CLZCALL026":
                        case "D-CLZCALL026SUR":

                            // ribbon face size updated at 2023-10-11
                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                            //44.96(1.7701)x37.79 (1.5665)mm  - height is ignored, line numbers is the real height
                            // se aumento de 1.7701 hasta 1.825f para incrementar el ancho del rectangulo en 5 pixeles
                            materialsFaces = new RibbonFace(font, 1.825f, 2f);
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.895f, 2f);  //50.16x37.79mm (1.92708fx 1.739583f)aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = 15;
                            heightInInches = 2f;
                            widthInInches = 1.825f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.5665f);  //50.16x37.79mm adicionales V (Eric)
                            widthAdditionalInInches = 1.9748f;
                            IsSimpleAdditional = false;
                            break;

                        // 60x25
                        case "D-CLZCALL027":
                        case "CLZCALL027":
                        case "D-CLZCALL027SUR":
                        case "CLZCALL027SUR":

                        case "D-CLZCALL028":
                        case "CLZCALL028":
                        case "D-CLZCALL028SUR":
                        case "CLZCALL028SUR":
                        case "D-CLZCALL029":
                        case "CLZCALL029":
                        case "D-CLZCALL029SUR":
                        case "CLZCALL029SUR":
                        case "D-CLZCALL030":
                        case "CLZCALL030":
                        case "D-CLZCALL030SUR":
                        case "CLZCALL030SUR":
                        case "D-CLZCALL031":
                        case "CLZCALL031":
                        case "D-CLZCALL031SUR":
                        case "CLZCALL031SUR":
                        case "D-CLZCALL032":
                        case "CLZCALL032":
                        case "D-CLZCALL032SUR":
                        case "CLZCALL032SUR":
                        case "D-CLZCALL033":
                        case "CLZCALL033":
                        case "D-CLZCALL033SUR":
                        case "CLZCALL033SUR":


                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.825f, 2f);  //46x23mm compo 
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.895f, 2f);  //50x23mm aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = 9;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            widthAdditionalInInches = 1.895f;

                            IsSimpleAdditional = false;
                            break;
                    }

                    //get list Fibers
                    listfibers = CreateFiberList(compo, od.FillingWeightId, od.FillingWeightText, withSeparatedPercentage, exceptionsLocation:od.ExceptionsLocation);
                    listcareinstructions = CreateCareInstructionsList(compo);

                    CompositionText = new List<PluginCompositionTextPreviewData>();
                    foreach (var fiber in listfibers)
                    {
                        CompositionText.Add(new PluginCompositionTextPreviewData()
                        {
                            FiberType = string.IsNullOrEmpty(fiber.Percent) && string.IsNullOrEmpty(fiber.FiberType) ? "TITLE" : fiber.FiberType,
                            Langs = fiber.Langs,
                            Percent = withSeparatedPercentage ?  fiber.Percent : string.Empty,
                            Text = fiber.Text,
                            IsTitle = fiber.TextType == TextType.Title,
                            SectionFibersText = fiber.SectionFibersText,
                        });
                    }
                    CareInstructionsText = new List<PluginCompositionTextPreviewData>();
                    foreach (var ciText in listcareinstructions)
                    {
                        CareInstructionsText.Add(new PluginCompositionTextPreviewData
                        {
                            FiberType = ciText.FiberType,
                            Langs = ciText.Langs,
                            Percent = ciText.Percent,
                            Text = ciText.Text,
                            IsTitle = false

                        });
                    }

                    CalculateComposition(compo, compositionData, listfibers, orderData, materialsFaces, allowedLinesByPage);

                    if (IsSimpleAdditional)
                    {
                        AdditionalSimple(compo, careInstructions, additionals, Symbols, compositionData);
                    }
                    else
                    {
                        ClearAdditionalPages(orderData[0].ProjectID, compo.ID, totalPages);
                        AdditionalByPage(compo, careInstructions, additionals, Symbols, compositionData, materialsFaces2, allowedLinesByPage);
                    }


                    //change articles
                    total_compo_pages = 0;
                    compositionData.TryGetValue("ComposNumber", out componumber);
                    total_compo_pages = int.Parse(componumber);

                    total_addcare_pages = 0;
                    if (compositionData.TryGetValue("AdditionalsNumber", out additionalnumber))
                        total_addcare_pages = int.Parse(additionalnumber);

                    CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);



                    SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);
                    if (PluginCompoPreviewData == null)
                    {
                        PluginCompoPreviewData = new List<PluginCompoPreviewData>();
                    }

                    

                    PluginCompoPreviewData.Add(new PluginCompoPreviewData() { 
                        CompoData = compositionData, 
                        CompositionText = CompositionText, 
                        CareInstructionsText = CareInstructionsText,
                        Symbols = Symbols.ToString(), 
                        Lines = allowedLinesByPage, 
                        Heigth = heightInInches, 
                        Width = widthInInches, 
                        WidthAdditional = widthAdditionalInInches, 
                        WithSeparatedPercentage = withSeparatedPercentage,  
                    });

                    indexCompo++;
                }

                ChangeArticle(od, printer_job.ElementAt(0));
            }

        }


        #region Methods

        private RibbonFace GetRibbonFace(string artCode)
        {

            RibbonFace ribbonFace = null; 
            switch (artCode)
            {
                case "D-CLZCALL001SUR":
                case "CLZCALL001SUR":
                case "D-CLZCALL001":
                case "CLZCALL001":
                    break;
                case "XXXX - 60x25":

                    font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    ribbonFace = new RibbonFace(font, 1.9685f, 0.9055f);  //50x23mm adicionales V (Cristina)
                    break;

                case "XXXX-ALL 60x40":

                    font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                    ribbonFace = new RibbonFace(font, 1.9748f, 1.4440f);  //50.16x36.68mm adicionales V (Eric)
                    break;



                // CORRECION MEDIDAS 2023-10
                // 60x40
                case "CLZCALL021":
                case "CLZCALL021SUR":
                case "D-CLZCALL021":
                case "D-CLZCALL021SUR":
                case "CLZCALL023":
                case "CLZCALL023SUR":
                case "D-CLZCALL023":
                case "D-CLZCALL023SUR":
                case "CLZCALL024":
                case "CLZCALL024SUR":
                case "D-CLZCALL024":
                case "D-CLZCALL024SUR":

                case "CLZCALL020":
                case "CLZCALL020SUR":
                case "CLZCALL022":
                case "CLZCALL022SUR":
                case "CLZCALL025":
                case "CLZCALL025SUR":
                case "CLZCALL026":
                case "CLZCALL026SUR":
                case "D-CLZCALL020":
                case "D-CLZCALL020SUR":
                case "D-CLZCALL022":
                case "D-CLZCALL022SUR":
                case "D-CLZCALL025":
                case "D-CLZCALL025SUR":
                case "D-CLZCALL026":
                case "D-CLZCALL026SUR":

                    // ribbon face size updated at 2023-10-11
                    font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                                                                               //44.96(1.7701)x37.79 (1.5665)mm  - height is ignored, line numbers is the real height
                                                                               // se aumento de 1.7701 hasta 1.825f para incrementar el ancho del rectangulo en 5 pixeles

                    
                    ribbonFace = new RibbonFace(font, 1.9748f, 1.5665f);  //50.16x37.79mm adicionales V (Eric)
                    
                    break;

                // 60x25
                case "D-CLZCALL027":
                case "CLZCALL027":
                case "D-CLZCALL027SUR":
                case "CLZCALL027SUR":

                case "D-CLZCALL028":
                case "CLZCALL028":
                case "D-CLZCALL028SUR":
                case "CLZCALL028SUR":
                case "D-CLZCALL029":
                case "CLZCALL029":
                case "D-CLZCALL029SUR":
                case "CLZCALL029SUR":
                case "D-CLZCALL030":
                case "CLZCALL030":
                case "D-CLZCALL030SUR":
                case "CLZCALL030SUR":
                case "D-CLZCALL031":
                case "CLZCALL031":
                case "D-CLZCALL031SUR":
                case "CLZCALL031SUR":
                case "D-CLZCALL032":
                case "CLZCALL032":
                case "D-CLZCALL032SUR":
                case "CLZCALL032SUR":
                case "D-CLZCALL033":
                case "CLZCALL033":
                case "D-CLZCALL033SUR":
                case "CLZCALL033SUR":
                    font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    ribbonFace = new RibbonFace(font, 1.895f, 2f);  //50x23mm aqui se restaron 6 pixeles al ancho
                    break;
            }
            return ribbonFace; 
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {

            return; // disable method for Zara, Composition Text already generates from UI and saved in Database

            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            int indexCompo = 0;
            float widthInInches = 0;
            float heightInInches = 0;

            InitializceSeparator(projectData);

            Dictionary<CompoCatalogName, IEnumerable<string>> ZaraLanguage = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            ZaraLanguage.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.FIBERS, FibersLanguage);
            ZaraLanguage.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);
            List<PluginCompositionTextPreviewData> CompositionText = new List<PluginCompositionTextPreviewData>();
            //configuration
            bmp = new Bitmap(100, 100);
            g = Graphics.FromImage(bmp);

            //create list fiber per composition
            foreach (var od in orderData)
            {
                var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR);

                totalCompositions = compositions.Count();

                IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);



                foreach (var compo in compositions)
                {
                    bool withSeparatedPercent = true; 
                    RibbonFace materialsFaces = new RibbonFace();
                    RibbonFace materialsFaces2 = new RibbonFace();
                    List<CompositionTextDTO> listfibers = new List<CompositionTextDTO>();

                    // add care instructions
                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder additionals = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder();

                    int total_compo_pages = 0;
                    int total_addcare_pages = 0;
                    string componumber;
                    string additionalnumber;
                    int allowedLinesByPage = 0;
                    bool IsSimpleAdditional = true;
                    int totalPages = 10;

                    var compositionData = new Dictionary<string, string>();

                    IArticle currentArticle;
                    string artCode = string.Empty;

                    foreach (var job in printer_job)
                    {
                        currentArticle = articleRepo.GetByID(job.ArticleID);
                        artCode = currentArticle.ArticleCode.Contains("_") ? currentArticle.ArticleCode.Substring(0, currentArticle.ArticleCode.LastIndexOf("_")) : currentArticle.ArticleCode;
                    }



                    switch (artCode)
                    {
                        case "D-CLZCALL001SUR":
                        case "CLZCALL001SUR":
                        case "D-CLZCALL001":
                        case "CLZCALL001":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta                          
                            materialsFaces = new RibbonFace(font, 3.7700f, 0.9374f);
                            heightInInches = 0.9374f;
                            widthInInches = 3.7700f;
                            allowedLinesByPage = 9;
                            IsSimpleAdditional = true;

                            break;


                        case "XXXX - 60x25":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.8503f, 0.9634f);  //46x23mm compo
                            heightInInches = 0.9634f;
                            widthInInches = 1.8503f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9685f, 0.9055f);  //50x23mm adicionales V (Cristina)
                            //CalculateCompositionSmall(compo, indexCompo, sectionLanguageSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator, od, orderData, materialsFaces, artCode,9);
                            allowedLinesByPage = 10;
                            IsSimpleAdditional = false;

                            break;

                        case "XXXX-ALL 60x40":

                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                            materialsFaces = new RibbonFace(font, 1.9732f, 1.4350f);  //46x36.45mm compo
                            heightInInches = 1.4350f;
                            widthInInches = 1.9732f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.4440f);  //50.16x36.68mm adicionales V (Eric)
                            allowedLinesByPage = 15;
                            IsSimpleAdditional = false;

                            break;



                        // CORRECION MEDIDAS 2023-10
                        // 60x40
                        case "CLZCALL021":
                        case "CLZCALL021SUR":
                        case "D-CLZCALL021":
                        case "D-CLZCALL021SUR":
                        case "CLZCALL023":
                        case "CLZCALL023SUR":
                        case "D-CLZCALL023":
                        case "D-CLZCALL023SUR":
                        case "CLZCALL024":
                        case "CLZCALL024SUR":
                        case "D-CLZCALL024":
                        case "D-CLZCALL024SUR":

                        case "CLZCALL020":
                        case "CLZCALL020SUR":
                        case "CLZCALL022":
                        case "CLZCALL022SUR":
                        case "CLZCALL025":
                        case "CLZCALL025SUR":
                        case "CLZCALL026":
                        case "CLZCALL026SUR":
                        case "D-CLZCALL020":
                        case "D-CLZCALL020SUR":
                        case "D-CLZCALL022":
                        case "D-CLZCALL022SUR":
                        case "D-CLZCALL025":
                        case "D-CLZCALL025SUR":
                        case "D-CLZCALL026":
                        case "D-CLZCALL026SUR":

                            // ribbon face size updated at 2023-10-11
                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                            //44.96(1.7701)x37.79 (1.5665)mm  - height is ignored, line numbers is the real height
                            // se aumento de 1.7701 hasta 1.825f para incrementar el ancho del rectangulo en 5 pixeles
                            materialsFaces = new RibbonFace(font, 1.825f, 2f);
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.895f, 2f);  //50.16x37.79mm (1.92708fx 1.739583f)aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = 15;
                            heightInInches = 2f;
                            widthInInches = 1.825f;

                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.5665f);  //50.16x37.79mm adicionales V (Eric)
                            IsSimpleAdditional = false;
                            break;

                        // 60x25
                        case "D-CLZCALL027":
                        case "CLZCALL027":
                        case "D-CLZCALL027SUR":
                        case "CLZCALL027SUR":

                        case "D-CLZCALL028":
                        case "CLZCALL028":
                        case "D-CLZCALL028SUR":
                        case "CLZCALL028SUR":
                        case "D-CLZCALL029":
                        case "CLZCALL029":
                        case "D-CLZCALL029SUR":
                        case "CLZCALL029SUR":
                        case "D-CLZCALL030":
                        case "CLZCALL030":
                        case "D-CLZCALL030SUR":
                        case "CLZCALL030SUR":
                        case "D-CLZCALL031":
                        case "CLZCALL031":
                        case "D-CLZCALL031SUR":
                        case "CLZCALL031SUR":
                        case "D-CLZCALL032":
                        case "CLZCALL032":
                        case "D-CLZCALL032SUR":
                        case "CLZCALL032SUR":
                        case "D-CLZCALL033":
                        case "CLZCALL033":
                        case "D-CLZCALL033SUR":
                        case "CLZCALL033SUR":


                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.825f, 2f);  //46x23mm compo 
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.895f, 2f);  //50x23mm aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = 10;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;

                            
                            IsSimpleAdditional = false;
                            break;
                    }
                    //get list Fibers
                    listfibers = CreateFiberList(compo, od.FillingWeightId, od.FillingWeightText);
                    CompositionText = new List<PluginCompositionTextPreviewData>();
                    foreach (var fiber in listfibers)
                    {
                        CompositionText.Add(new PluginCompositionTextPreviewData()
                        {
                            FiberType = fiber.FiberType,
                            Langs = fiber.Langs,
                            Percent = fiber.Percent,
                            Text = fiber.Text,
                            IsTitle = fiber.TextType == TextType.Title,
                            SectionFibersText = fiber.SectionFibersText,
                        });
                    }

                    CalculateComposition(compo, compositionData, listfibers, orderData, materialsFaces, allowedLinesByPage);

                    if (IsSimpleAdditional)
                    {
                        AdditionalSimple(compo, careInstructions, additionals, Symbols, compositionData);
                    }
                    else
                    {
                        ClearAdditionalPages(orderData[0].ProjectID, compo.ID, totalPages);
                        AdditionalByPage(compo, careInstructions, additionals, Symbols, compositionData, materialsFaces2, allowedLinesByPage);
                    }


                    //change articles
                    total_compo_pages = 0;
                    compositionData.TryGetValue("ComposNumber", out componumber);
                    total_compo_pages = int.Parse(componumber);

                    total_addcare_pages = 0;
                    if (compositionData.TryGetValue("AdditionalsNumber", out additionalnumber))
                        total_addcare_pages = int.Parse(additionalnumber);

                    CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);

                    //SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);
                    if (PluginCompoPreviewData == null)
                    {
                        PluginCompoPreviewData = new List<PluginCompoPreviewData>();
                    }
                    PluginCompoPreviewData.Add(new PluginCompoPreviewData() { CompoData = compositionData, CompositionText = CompositionText, Symbols = Symbols.ToString(), CareInstructions = careInstructions.ToString(), Lines = allowedLinesByPage, Heigth = heightInInches, Width = widthInInches });

                    indexCompo++;
                }

                ChangeArticle(od, printer_job.ElementAt(0));
            }
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string,string> compositionDataSource, List<int> targets)
        {
            CompositionDefinition compo = new CompositionDefinition();
            var projectData = orderUtilService.GetProjectById(od.ProjectID);
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();

            InitializceSeparator(projectData);


            string artCode = GetArticleCode(od.OrderID);
            Dictionary<CompoCatalogName, IEnumerable<string>> ZaraLanguage = GetZaraLanguageDictionary();
            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR);

            var compoSource = compositions.FirstOrDefault(c => c.ID == sourceId); 
            if (compoSource == null)
            {
                return; 
            }
            foreach (var composition in compositions)
            {
                if (targets.Any(t => t == composition.ID))
                {
                    List<OrderPluginData> orderData = new List<OrderPluginData> { od };
                    TargetCompositionMapping(compoSource, composition);
                    compositionDataSource["ID"] = composition.ID.ToString();
                    GetSymbols(composition, Symbols, compositionDataSource);
                    //SaveComposition(composition, od, compositionDataSource, Symbols, careInstructions, orderData, CI_SEPARATOR);
                    var cleanedCi = Regex.Replace(careInstructions.ToString().Trim(), CI_SEPARATOR + "$", string.Empty);// PERSONALIZED TRIM END
                    orderUtilService.SaveComposition(orderData[0].ProjectID, compo.ID, compositionDataSource, cleanedCi, Symbols.ToString());
                }
            }
        }

        private static void TargetCompositionMapping(CompositionDefinition compoSource, CompositionDefinition composition)
        {
            composition.CareInstructions = compoSource.CareInstructions;
            composition.Sections = compoSource.Sections;
            composition.ArticleCode = compoSource.ArticleCode;
            composition.EnableComposition = compoSource.EnableComposition;
            composition.Product = compoSource.Product;
            composition.OrderID = compoSource.OrderID;
            composition.EnableExceptions = compoSource.EnableExceptions;
            composition.ArticleID = compoSource.ArticleID;
            composition.TargetArticle = compoSource.TargetArticle;
        }

        public void SaveCompoPreview(OrderPluginData od,
                                     PluginCompoPreviewInputData data

            )
        {
            string[] compoArray = data.compoArray;
            string[] percentArray = data.percentArray;  
            string[] leatherArray = data.leatherArray;
            string[] additionalArray = data.additionalArray;
            int labelLines = data.labelLines;
            int ID = data.ID; 


            CompositionDefinition compo = new CompositionDefinition();
            Dictionary<string, string> compositionData = new Dictionary<string, string>();
            var IsSimpleAdditional = false;
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();
            var additionals = new StringBuilder();
            int totalPages = 10;
            //int lineNumber = 0;
            int indexCompo = 0;
            var font = new Font("Arial Unicode MS", 6, FontStyle.Regular);

            var projectData = orderUtilService.GetProjectById(od.ProjectID);
            InitializceSeparator(projectData);

            string artCode = GetArticleCode(od.OrderID);

            Dictionary<CompoCatalogName, IEnumerable<string>> ZaraLanguage = GetZaraLanguageDictionary();

            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR);
            compo = compositions.FirstOrDefault(c => c.ID == ID);
            if (compo == null)
            {
                return;
            }

            int index, acumLabels;
            GetCompositionDataLeatherLines(leatherArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataCompoLines(compoArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataPercentLines(percentArray, labelLines, compositionData, out index, out acumLabels);

            compositionData.Add("AdditionalCompress", data.AdditionalsCompress.ToString());
            compositionData.Add("FiberCompress", data.FiberCompress.ToString()); 
            compositionData.Add("ExceptionsLocation", od.ExceptionsLocation.ToString());


            var labelsCount = acumLabels - 1 == 0 ? 1 : acumLabels - 1;
            IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);
            compositionData.Add("ComposNumber", (acumLabels - 1).ToString());

            List<OrderPluginData> orderData = new List<OrderPluginData> { od };
            RibbonFace materialsFaces2 = new RibbonFace();
            materialsFaces2.Font = font; 
            
            if (IsSimpleAdditional)
            {
                AdditionalSimple(compo, careInstructions, additionals, Symbols, compositionData);
                
            }
            else
            {
                ClearAdditionalPages(od.ProjectID, compo.ID, totalPages);
               if (additionalArray.Count() > 0)
               {
                    GetSymbols(compo, Symbols, compositionData);
                    GetAdditionalTextLines(additionalArray, labelLines, compositionData, out index, out acumLabels);
                    GetCareInstructions(compo, careInstructions, compositionData, GetRibbonFace(artCode), labelLines);
                }
                else
               {
                    AdditionalByPage(compo, careInstructions, additionals, Symbols, compositionData, GetRibbonFace(artCode), labelLines);
               }
            }
            int total_compo_pages;
            
            compositionData.TryGetValue("ComposNumber", out string componumber);
            total_compo_pages = int.Parse(componumber);

            total_compo_pages = total_compo_pages == 0 ? 1 : total_compo_pages;

            int total_addcare_pages = 0;
            if (compositionData.TryGetValue("AdditionalsNumber", out string additionalnumber))
                total_addcare_pages = int.Parse(additionalnumber);

            CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);
            SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);

            var compositionsSaved = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR);

            ChangeArticle(od, printer_job.ElementAt(0));
        }

        private void GetCareInstructions(CompositionDefinition compo, StringBuilder careInstructions, Dictionary<string, string> compositionData, RibbonFace materialsFaces, int linenumber)
        {
            var symbols = new List<string>();
            var addExc = new List<string>(); // additionals and exceptions
            var basic = new List<string>();

            var addExcTable = new Dictionary<int, List<string>>();

            //foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            foreach (var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if (ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if (ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // the keys of the composition data is a hardcode definition for CompositionLabel table for the current project
            careInstructions.Append(string.Join(CI_SEPARATOR, basic));
            compositionData.Add("FullCareInstructions", string.Join(CI_SEPARATOR, basic));


        }

        private void GetAdditionalTextLines(string[] additionalArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0; 
            acumLabels = 1;

            while (index < additionalArray.Length)
            {
                var additionalLabel = additionalArray.Skip(index).Take(labelLines);
                StringBuilder additionalLabelText = new StringBuilder();
                foreach (var item in additionalLabel)
                {
                    additionalLabelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"AdditionalPage{acumLabels.ToString()}", additionalLabelText.ToString());
                acumLabels++;
                index += labelLines; 
            }

            compositionData.Add($"AdditionalsNumber", (acumLabels - 1).ToString());
        }

        private string GetArticleCode(int orderId)
        {
            IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(orderId, true);
            IArticle currentArticle;
            string artCode = string.Empty;

            foreach (var job in printer_job)
            {
                currentArticle = articleRepo.GetByID(job.ArticleID);
                artCode = currentArticle.ArticleCode.Contains("_") ? currentArticle.ArticleCode.Substring(0, currentArticle.ArticleCode.LastIndexOf("_")) : currentArticle.ArticleCode;
            }

            return artCode;
        }

        private static void GetCompositionDataPercentLines(string[] percentArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            while (index < percentArray.Length)
            {
                var percentLabel = percentArray.Skip(index).Take(labelLines);
                StringBuilder percentLabelText = new StringBuilder();
                foreach (var item in percentLabel)
                {
                    percentLabelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"Page{acumLabels.ToString()}_percent", percentLabelText.ToString());
                acumLabels++;
                index += labelLines;
            }
        }

        private static void GetCompositionDataLeatherLines(string[] leatherArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            var quantityOfPages = leatherArray.Length / labelLines;
            int lines = 0;
            for (int i = 0; i < quantityOfPages; i++)
            {
                var leatherPositionStrings = string.Empty;
                int lineNumber = 0;
                for (int j = lines; j < (i * labelLines) + labelLines; j++)
                {
                    if (leatherArray[j] == "1")
                    {
                        if (string.IsNullOrEmpty(leatherPositionStrings))
                        {
                            leatherPositionStrings = lineNumber.ToString();
                        }
                        else
                        {
                            leatherPositionStrings += $",{lineNumber.ToString()}";
                        }
                    }
                    lineNumber++;

                }
                lines = lines + labelLines;
                acumLabels = i + 1;
                compositionData.Add($"Page{acumLabels.ToString()}_leather", leatherPositionStrings);
            }
        }

        private static void GetCompositionDataCompoLines(string[] compoArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            while (index < compoArray.Length)
            {
                var compolabel = compoArray.Skip(index).Take(labelLines);
                StringBuilder labelText = new StringBuilder();
                foreach (var item in compolabel)
                {
                    //  labelText.Append(item.ToString().Concat(Environment.NewLine));
                    labelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"Page{acumLabels.ToString()}_compo", labelText.ToString());
                acumLabels++;
                index += labelLines;
            }
        }

        private ArticleConfig GetArticleConfig(string artCode)
        {
            float heightInInches = 0;
            float widthInInches = 0;
            int lineNumber = 0;
            bool withSeparatePercent = true; 

            switch (artCode)
            {
                case "D-CLZCALL001SUR":
                case "CLZCALL001SUR":
                case "D-CLZCALL001":
                case "CLZCALL001":

                    //font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta                          
                    //materialsFaces = new RibbonFace(font, 3.7700f, 0.9374f);
                    heightInInches = 0.9374f;
                    widthInInches = 3.7700f;
                    lineNumber = 9;
                    //IsSimpleAdditional = true;

                    break;


                case "XXXX - 60x25":

                    // font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    // materialsFaces = new RibbonFace(font, 1.8503f, 0.9634f);  //46x23mm compo
                    heightInInches = 0.9634f;
                    widthInInches = 1.8503f;

                    //materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9685f, 0.9055f);  //50x23mm adicionales V (Cristina)
                    //CalculateCompositionSmall(compo, indexCompo, sectionLanguageSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator, od, orderData, materialsFaces, artCode,9);
                    lineNumber = 10;
                    //IsSimpleAdditional = false;

                    break;

                case "XXXX-ALL 60x40":

                    font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                                                                               // materialsFaces = new RibbonFace(font, 1.9732f, 1.4350f);  //46x36.45mm compo
                    heightInInches = 1.4350f;
                    widthInInches = 1.9732f;

                    // materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.4440f);  //50.16x36.68mm adicionales V (Eric)
                    lineNumber = 15;
                    // IsSimpleAdditional = false;

                    break;



                // CORRECION MEDIDAS 2023-10
                // 60x40
                case "CLZCALL021":
                case "CLZCALL021SUR":
                case "D-CLZCALL021":
                case "D-CLZCALL021SUR":
                case "CLZCALL023":
                case "CLZCALL023SUR":
                case "D-CLZCALL023":
                case "D-CLZCALL023SUR":
                case "CLZCALL024":
                case "CLZCALL024SUR":
                case "D-CLZCALL024":
                case "D-CLZCALL024SUR":

                case "CLZCALL020":
                case "CLZCALL020SUR":
                case "CLZCALL022":
                case "CLZCALL022SUR":
                case "CLZCALL025":
                case "CLZCALL025SUR":
                case "CLZCALL026":
                case "CLZCALL026SUR":
                case "D-CLZCALL020":
                case "D-CLZCALL020SUR":
                case "D-CLZCALL022":
                case "D-CLZCALL022SUR":
                case "D-CLZCALL025":
                case "D-CLZCALL025SUR":
                case "D-CLZCALL026":
                case "D-CLZCALL026SUR":

                    // ribbon face size updated at 2023-10-11
                    //  font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                    //44.96(1.7701)x37.79 (1.5665)mm  - height is ignored, line numbers is the real height
                    // se aumento de 1.7701 hasta 1.825f para incrementar el ancho del rectangulo en 5 pixeles
                    //  materialsFaces = new RibbonFace(font, 1.825f, 2f);
                    heightInInches = 2f;
                    widthInInches = 1.825f;

                    //  materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.5665f);  //50.16x37.79mm adicionales V (Eric)
                    lineNumber = 15;
                    //  IsSimpleAdditional = false;
                    break;

                // 60x25
                case "D-CLZCALL027":
                case "CLZCALL027":
                case "D-CLZCALL027SUR":
                case "CLZCALL027SUR":

                case "D-CLZCALL028":
                case "CLZCALL028":
                case "D-CLZCALL028SUR":
                case "CLZCALL028SUR":
                case "D-CLZCALL029":
                case "CLZCALL029":
                case "D-CLZCALL029SUR":
                case "CLZCALL029SUR":
                case "D-CLZCALL030":
                case "CLZCALL030":
                case "D-CLZCALL030SUR":
                case "CLZCALL030SUR":
                case "D-CLZCALL031":
                case "CLZCALL031":
                case "D-CLZCALL031SUR":
                case "CLZCALL031SUR":
                case "D-CLZCALL032":
                case "CLZCALL032":
                case "D-CLZCALL032SUR":
                case "CLZCALL032SUR":
                case "D-CLZCALL033":
                case "CLZCALL033":
                case "D-CLZCALL033SUR":
                case "CLZCALL033SUR":


                    //  font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    //  materialsFaces = new RibbonFace(font, 1.825f, 2f);  //46x23mm compo 
                    heightInInches = 2f;
                    widthInInches = 1.825f;
                    
                    //   materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 0.9055f);  //50x23mm 1.9685f adicionales V (Cristina)
                    lineNumber = 9;
                   
                    //  IsSimpleAdditional = false;
                    break;
            }

            return new ArticleConfig()
            {
                LineNumber = lineNumber,
                HeightInInches = heightInInches,
                WidthInches = widthInInches,
                WithSeparatedPercentage = withSeparatePercent,   
            };
        }

        private Dictionary<CompoCatalogName, IEnumerable<string>> GetZaraLanguageDictionary()
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> ZaraLanguage = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            ZaraLanguage.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.FIBERS, FibersLanguage);
            ZaraLanguage.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            ZaraLanguage.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);
            return ZaraLanguage;
        }

        public void CalculateCompositionNew(CompositionDefinition compo, Dictionary<string, string> compositionData, List<CompositionTextDTO> listfibers, List<OrderPluginData> orderData, RibbonFace materialsFaces, int allowedLines)
        {
            var concatFibers = SplitComposition(listfibers);
            var allCompo = concatFibers.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            var allPages = new List<List<string>>();
            var allPercentages = new List<List<string>>() { new List<string>() { "" } };
            var allSymbols = new List<string>() { "" };

            var currentPage = new List<string>();
            var currentPercentagesPage = new List<string>() { "" };
            var currentSymbolPage = string.Empty;

            //add default empty page
            allPages.Add(currentPage);

            for (int pos = 0; pos < listfibers.Count(); pos++)
            {
                var currentText = listfibers[pos];

                if (currentText.TextType == TextType.CareInstruction)
                {
                    currentPage = new List<string>(); // new page
                    allPages.Add(currentPage);
                }

                //var pageText = string.Join(Environment.NewLine, currentPage.Append(currentText.Text));

                var fitResult = ContentFitsByLayout(materialsFaces, currentText.Text, allowedLines);

                // if single fiber text not fit, change strategy from block to phrase
                int appendStrategy = fitResult.Fit ? 0 : 1;

                if (appendStrategy == 0)
                {
                    AppendTextByBlock(allPages, currentPage, listfibers, pos, materialsFaces, allowedLines);
                }
                else
                {
                    AppendTextByPhrase(allPages, currentPage, listfibers, pos, materialsFaces, allowedLines);
                }


            }

            ClearCompositionLabels(orderData[0].ProjectID, compo.ID, 9, string.Empty, string.Empty);

            for (int j = 0; j < allPages.Count; j++)
            {
                var p = string.Join("", allPercentages.ElementAtOrDefault(j));
                var s = allSymbols[j];
                var f = string.Join(Environment.NewLine, allPages.ElementAtOrDefault(j));

                var percent = "Page" + (j + 1) + "_percent";
                var fibertype = "Page" + (j + 1) + "_leather";
                var fiber = "Page" + (j + 1) + "_compo";

                compositionData.Add(percent, p);
                compositionData.Add(fibertype, s);
                compositionData.Add(fiber, f);
            }

            //compo number
            compositionData.Add("ComposNumber", allPages.Count.ToString());

        }


        private bool AppendTextByBlock(List<List<string>> allPages, List<string> currentPage, List<CompositionTextDTO> listfibers, int pos, RibbonFace materialsFaces, int allowedLines)
        {
            var currentText = listfibers[pos];

            var pageText = string.Join(Environment.NewLine, currentPage.Append(currentText.Text));

            FitObj fitResult = ContentFitsByLayout(materialsFaces, pageText, allowedLines);

            FitObj emptyFitResult = ContentFitsByLayout(materialsFaces, currentText.Text, allowedLines);

            if (currentText.TextType == TextType.Title)
            {
                // si es un titulo revisar que hace fit con al menos una fibra que es el siguiente texto en la lista de fibras
                // ???: nunca llega un titulo sin fibras, de lo contrario sería necesario validar que existe el elemento "pos + 1", esto ya se valida en la definicion de la composición
                var nextText = listfibers[pos + 1];

                fitResult = ContentFitsByLayout(materialsFaces, string.Format("{0}{1}{2}", pageText, Environment.NewLine, nextText.Text), allowedLines);

                emptyFitResult = ContentFitsByLayout(materialsFaces, string.Format("{0}{1}{2}", currentText.Text, Environment.NewLine, nextText.Text), allowedLines);
            }


            // si no cabe el texto ni solo ni junto, cambio de estrategia
            if ((currentPage.Count == 0 && fitResult.Fit == false) || (currentText.TextType == TextType.Title && emptyFitResult.Fit == false)) return false; // change strategy


            if (fitResult.Fit == false)
            {
                currentPage = new List<string>();

                allPages.Add(currentPage);

                // TODO, falta los symbolos y el tipo de fibra
            }

            currentPage.Add(currentText.Text);

            return true;
        }

        private void AppendTextByPhrase(List<List<string>> allPages, List<string> currentPage, List<CompositionTextDTO> listfibers, int pos, RibbonFace materialsFaces, int allowedLines)
        {
            var fit = true;

            var currentText = listfibers[pos];

            var LOCAL_SEPARATOR = SECTION_SEPARATOR;

            var LOCAL_LANG_SEPARATOR = SECTION_LANG_SEPARATOR;

            if (currentText.TextType == TextType.Fiber)
            {
                LOCAL_SEPARATOR = FIBER_SEPARATOR;

                LOCAL_LANG_SEPARATOR = FIBER_LANG_SEPARATOR;
            }

            if (currentText.TextType == TextType.CareInstruction)
            {
                LOCAL_SEPARATOR = CI_SEPARATOR;

                LOCAL_LANG_SEPARATOR = CI_LANG_SEPARATOR;
            }

            foreach (var text in currentText.Langs)
            {
                var testingText = currentPage.Count > 0 ? LOCAL_SEPARATOR + text : text;

                var pageText = string.Join("", currentPage.Append(testingText));

                FitObj fitResult = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                var isLastASeparator = currentPage.Count < 1 && (currentPage.Last() == LOCAL_SEPARATOR || currentPage.Last() == LOCAL_LANG_SEPARATOR);

                if (fitResult.Fit == false)
                {

                    if (isLastASeparator == true)
                        currentPage.RemoveAt(currentPage.Count - 1);

                    currentPage = new List<string>(); // new page

                    allPages.Add(currentPage);

                }

                if (currentPage.Count > 0 && isLastASeparator == false)
                    currentPage.Add(LOCAL_LANG_SEPARATOR);

                currentPage.Add(text);
            }

            currentPage.Add(LOCAL_SEPARATOR);
        }




        public void CalculateComposition(CompositionDefinition compo, Dictionary<string, string> compositionData, List<CompositionTextDTO> listfibers, List<OrderPluginData> orderData, RibbonFace materialsFaces, int linenumber)
        {

            //Fibers Algorithm
            var concatFibers = SplitComposition(listfibers);

            try
            {
                var allCompo = concatFibers.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

                var allPages = new List<List<string>>();
                var allPercentages = new List<List<string>>();
                var allSymbols = new List<string>();
               
                var currentPage = new List<string>();
                var currentPercentagesPage = new List<string>();
                var currentSymbolPage = string.Empty;

                //add default empty page
                allPages.Add(currentPage);
               
                for(int pos = 0; pos < allCompo.Count; pos++)
                {
                    var count = 0;
                    bool isFiber = listfibers[pos].Percent != "";
                    var text = allCompo[pos];
                  
                       

                    var auxtext = string.Empty;
                    FitObj secondaryText = new FitObj();

                    //get currente section/fiber
                    var pageText = string.Join(Environment.NewLine, currentPage.Append(text));

                    //se calcula el fit del elemento actual
                    //FitObj primaryText = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText,linenumber);

                    if(materialsFaces.Font == null)
                    {
                        materialsFaces.Font = new Font("Arial Unicode MS", 6, FontStyle.Regular);
                    }

                    FitObj primaryText = ContentFitsByLayout(materialsFaces, pageText, linenumber);

                    //se calcula el fit del elemento siguiente cuando sea una seccion
                    if(!isFiber)
                    {
                        var compoLength = allCompo.Count;
                        var nextindex = pos + 1;
                        var nextfiber = string.Empty;

                        //se valida si la fibra futura hace fit despues de una seccion
                        if(nextindex <= compoLength)
                        {
                            if(nextindex < allCompo.Count)
                            {
                                nextfiber = allCompo[nextindex];
                            }

                        }
                        else
                        {
                            nextfiber = allCompo[compoLength];
                        }

                        auxtext = pageText + Environment.NewLine + nextfiber;
                        //secondaryText = ContentFitsByLines(materialsFaces.Font, materialsFaces, auxtext, linenumber);
                        secondaryText = ContentFitsByLayout(materialsFaces, auxtext, linenumber);
                    }


                    if(primaryText.Fit || (!primaryText.Fit && currentPage.Count == 0))
                    {
                        if(!isFiber)
                        {
                            //sirve para calcular la ultima seccion de una pagina
                            if(!secondaryText.Fit && currentPage.Count > 0)
                            {
                                currentPage = new List<string>();
                                allPages.Add(currentPage);


                                List<string> clonedList = new List<string>(currentPercentagesPage);
                                allPercentages.Add(clonedList);
                                currentPercentagesPage.Clear();

                                //symbols
                                allSymbols.Add(currentSymbolPage);
                                currentSymbolPage = string.Empty;

                                //se agregan los espacios de la nueva seccion
                                count = primaryText.Lines - clonedList.Count;

                                if(count > 0)
                                {
                                    for(int i = 0; i < count; i++)
                                    {
                                        currentPercentagesPage.Add(Environment.NewLine);
                                    }
                                }
                            }
                            else
                            {
                                //se agregan los espacios de la nueva seccion
                                count = primaryText.Lines - currentPercentagesPage.Count;

                                if(count > 0)
                                {
                                    for(int i = 0; i < count; i++)
                                    {
                                        currentPercentagesPage.Add(Environment.NewLine);
                                    }
                                }
                            }
                        }
                        else
                        {
                            count = primaryText.Lines - currentPercentagesPage.Count;
                            currentPercentagesPage.Add(listfibers[pos].Percent + Environment.NewLine);

                            //symbol
                            if(listfibers[pos].FiberType == "1")
                            {
                                currentSymbolPage += string.Format("{1}{0}", currentPercentagesPage.Count, string.IsNullOrEmpty(currentSymbolPage) ? string.Empty : ",");
                            }

                            if(count > 0)
                            {
                                for(int i = 0; i < count - 1; i++)
                                {
                                    currentPercentagesPage.Add(Environment.NewLine);
                                }
                            }
                        }
                    }
                    else
                    {
                        //se crea nueva pagina y se calculan los espacios para los porcentajes
                        currentPage = new List<string>();
                        allPages.Add(currentPage);
                        List<string> clonedList = new List<string>(currentPercentagesPage);
                        allPercentages.Add(clonedList);
                        currentPercentagesPage.Clear();

                        //symbols
                        allSymbols.Add(currentSymbolPage);
                        currentSymbolPage = "";

                        if(!isFiber)
                        {
                            count = primaryText.Lines - clonedList.Count;

                            if(count > 0)
                            {
                                for(int i = 0; i < count; i++)
                                {
                                    currentPercentagesPage.Add(Environment.NewLine);
                                }
                            }
                        }
                        else
                        {
                            count = primaryText.Lines - clonedList.Count;
                            currentPercentagesPage.Add(listfibers[pos].Percent + Environment.NewLine);

                            //symbol
                            if(listfibers[pos].FiberType == "1")
                            {
                                currentSymbolPage += string.Format("{1}{0}", currentPercentagesPage.Count, string.IsNullOrEmpty(currentSymbolPage) ? string.Empty : ",");
                            }

                            if(count > 0)
                            {
                                for(int i = 0; i < count - 1; i++)
                                {
                                    currentPercentagesPage.Add(Environment.NewLine);
                                }
                            }
                        }
                    }


                    currentPage.Add(text);

                    //llena la ultima pagina de porcentajes
                    if(pos + 1 == allCompo.Count)
                    {
                        List<string> clonedList = new List<string>(currentPercentagesPage);
                        allPercentages.Add(clonedList);
                        currentPercentagesPage.Clear();

                        //symbols
                        allSymbols.Add(currentSymbolPage);
                        currentSymbolPage = "";


                        //for labels with compo in code sections
                        if(allPages.Count == 1)
                        {
                            page1_totallines = primaryText.Lines;
                        }
                    }
                }

                //limpiar fribras
                ClearCompositionLabels(orderData[0].ProjectID, compo.ID, 9, string.Empty, string.Empty);

                for(int j = 0; allPages.Count > 0 && j < allPages.Count; j++)
                {
                    var p = string.Empty;

                    if(allPercentages.Any())
                    {
                        p = string.Join("", allPercentages.ElementAtOrDefault(j));
                    }

                    var f = string.Join(Environment.NewLine, allPages.ElementAtOrDefault(j));

                    var s = string.Empty;
                    if(allSymbols.Any())
                    {
                        s = allSymbols[j];
                    }
                    var percent = "Page" + (j + 1) + "_percent";
                    var fiber = "Page" + (j + 1) + "_compo";
                    var fibertype = "Page" + (j + 1) + "_leather";

                    compositionData.Add(percent, p);
                    compositionData.Add(fiber, f);
                    compositionData.Add(fibertype, s);
                }

                //compo number
                compositionData.Add("ComposNumber", allPages.Count.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en {concatFibers}");
            }
        }

        public void AdditionalSimple(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder Symbols, Dictionary<string, string> compositionData)
        {
            foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            {
                var langsList = ci.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                if (ci.Symbol != string.Empty)
                {
                    //Symbols.Append(ci.Symbol); // TODO: now, always use FONT 
                    Symbols.Append(ci.Symbol + ",");
                }

                var translations = langsList.Length > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList[0];

                if (ci.Category != "Additional" && ci.Category != "Exception")
                {
                    careInstructions.Append(translations);
                    careInstructions.Append(CI_SEPARATOR);
                }
                else
                {
                    additionals.Append(translations);
                    additionals.Append(CI_SEPARATOR);
                }
            }

            //set Additionals only Zara
            compositionData.Add("FullAdditionals", additionals.ToString().TrimEnd(CI_SEPARATOR.ToCharArray()));
        }

        public void CalculateArticle(string ArticleCode, CompositionDefinition compo, int compoIndex, OrderPluginData od, StringBuilder additionals, int allCompoPages, int allAdditionalPages)
        {
            IEnumerable<IPrinterJob> printerjob = printerJobRepo.GetByOrderID(compo.OrderID, true);

            foreach (var job in printerjob)
            {
                //search new article
                var compoCode = "";
                int sum_pages_compo_addcare = 0;
                int PagesSize = 0;
                IArticle newarticle = null;

                switch (ArticleCode)
                {
                    case "CLZCALL001SUR":
                    case "CLZCALL001":

                    case "D-CLZCALL001SUR":
                    case "D-CLZCALL001":

                        //CASO no adicionales solo 1 fibra
                        if (additionals.Length == 0 && allCompoPages == 1 && page1_totallines <= 5)
                        {
                            compoCode = "0";
                            ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 0 });

                        }
                        else
                        {
                            switch (allCompoPages)
                            {
                                case 1:
                                    compoCode = "0-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 1 });
                                    break;
                                case 2:
                                    compoCode = "1";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 2 });
                                    break;
                                case 3:
                                    compoCode = "1-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 3 });
                                    break;
                                case 4:
                                    compoCode = "2";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 4 });
                                    break;
                                case 5:
                                    compoCode = "2-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 5 });
                                    break;
                                case 6:
                                    compoCode = "3";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 6 });
                                    break;
                                case 7:
                                    compoCode = "3-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 7 });
                                    break;
                                case 8:
                                    compoCode = "4";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 8 });
                                    break;
                                case 9:
                                    compoCode = "4-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 9 });
                                    break;
                                case 10:
                                    compoCode = "5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 10 });
                                    break;

                                case 11:
                                    compoCode = "5-5";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 11 });
                                    break;
                                case 12:
                                    compoCode = "6";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 12 });
                                    break;
                                default:
                                    compoCode = "0";
                                    ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 0 });
                                    break;
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

                        compoCode = "";
                        sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

                        // TODO: use formula: compoCode ((sum_pages_compo_addcare - 1 )/ 2).ToString("0.0")
                        // pageSize Math.Floor(Convert.ToDecimal(compoCode) * 2)


                        if (sum_pages_compo_addcare == 1)
                        {
                            compoCode = "0";
                            PagesSize = 0;
                        }
                        else if (sum_pages_compo_addcare == 2)
                        {
                            compoCode = "0-5";
                            PagesSize = 1;
                        }
                        else if (sum_pages_compo_addcare == 3)
                        {
                            compoCode = "1";
                            PagesSize = 2;
                        }
                        else if (sum_pages_compo_addcare == 4)
                        {
                            compoCode = "1-5";
                            PagesSize = 3;
                        }
                        else if (sum_pages_compo_addcare == 5)
                        {
                            compoCode = "2";
                            PagesSize = 4;
                        }
                        else if (sum_pages_compo_addcare == 6)
                        {
                            compoCode = "2-5";
                            PagesSize = 5;
                        }
                        else if (sum_pages_compo_addcare == 7)
                        {
                            compoCode = "3";
                            PagesSize = 6;
                        }
                        else if (sum_pages_compo_addcare == 8)
                        {
                            compoCode = "3-5";
                            PagesSize = 7;
                        }
                        else if (sum_pages_compo_addcare == 9)
                        {
                            compoCode = "4";
                            PagesSize = 8;
                        }
                        else if (sum_pages_compo_addcare == 10)
                        {
                            compoCode = "4-5";
                            PagesSize = 9;
                        }
                        else if (sum_pages_compo_addcare == 11)
                        {
                            compoCode = "5";
                            PagesSize = 10;
                        }
                        else if (sum_pages_compo_addcare == 12)
                        {
                            compoCode = "5-5";
                            PagesSize = 11;
                        }
                        else if (sum_pages_compo_addcare == 13)
                        {
                            compoCode = "6";
                            PagesSize = 12;
                        }

                        ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = PagesSize });
                        break;

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
                        sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

                        if (sum_pages_compo_addcare == 1)
                        {
                            compoCode = "0";
                            PagesSize = 0;
                        }
                        else if (sum_pages_compo_addcare == 2)
                        {
                            compoCode = "0-5";
                            PagesSize = 1;
                        }
                        else if (sum_pages_compo_addcare == 3)
                        {
                            compoCode = "1";
                            PagesSize = 2;
                        }
                        else if (sum_pages_compo_addcare == 4)
                        {
                            compoCode = "1-5";
                            PagesSize = 3;
                        }
                        else if (sum_pages_compo_addcare == 5)
                        {
                            compoCode = "2";
                            PagesSize = 4;
                        }
                        else if (sum_pages_compo_addcare == 6)
                        {
                            compoCode = "2-5";
                            PagesSize = 5;
                        }
                        else if (sum_pages_compo_addcare == 7)
                        {
                            compoCode = "3";
                            PagesSize = 6;
                        }
                        else if (sum_pages_compo_addcare == 8)
                        {
                            compoCode = "3-5";
                            PagesSize = 7;
                        }
                        else if (sum_pages_compo_addcare == 9)
                        {
                            compoCode = "4";
                            PagesSize = 8;
                        }
                        else if (sum_pages_compo_addcare == 10)
                        {
                            compoCode = "4-5";
                            PagesSize = 9;
                        }
                        else if (sum_pages_compo_addcare == 11)
                        {
                            compoCode = "5";
                            PagesSize = 10;
                        }
                        else if (sum_pages_compo_addcare == 12)
                        {
                            compoCode = "5-5";
                            PagesSize = 11;
                        }

                        ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = PagesSize });
                        break;
                }

                if (ArticleCategoryLst.Count < 1)
                {
                    string roles = string.Join(
                           Notification.ROLE_SEPARATOR,
                           new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

                    var title = $"It was not possible to determine the article for the exposed composition";
                    var message = $"Error when trying to get the article code, check if the composition is correct for this label";
                    var nkey = message.GetHashCode().ToString();
                    var userData = factory.GetInstance<IUserData>();
                    // AppUser appUser = userManager.FindByNameAsync(userData.UserName).Result;

                    //if article not found throw Exception
                    notificationRepo.AddNotification(
                        companyid: job.CompanyID
                        , type: NotificationType.OrderTracking
                        , intendedRoles: roles
                        , intendedUser: userData.UserName
                        , nkey: nkey + job.CompanyOrderID
                        , source: "ZaraCompoPlugin"
                        , title: title
                        , message: message
                        , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = ArticleCode + "_" + compoCode }
                        , autoDismiss: false
                        , locationID: null
                        , projectID: od.ProjectID
                        , actionController: null);

                    throw new Exception("An error occurred while saving the composition.");
                }


                var lst = ArticleCategoryLst.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault();

                newarticle = articleRepo.GetByCodeInProject(lst.ArticleCode, od.ProjectID);

                if (newarticle == null)
                {
                    string roles = string.Join(
                    Notification.ROLE_SEPARATOR,
                    new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

                    var title = $"Article not found";
                    var message = $"Error when trying to get the article code, check if the article exists";
                    var nkey = message.GetHashCode().ToString();

                    //if article not found throw Exception
                    notificationRepo.AddNotification(
                        companyid: job.CompanyID
                        , type: NotificationType.OrderTracking
                        , intendedRoles: roles
                        , intendedUser: null
                        , nkey: nkey + job.CompanyOrderID
                        , source: "ZaraCompoPlugin"
                        , title: title
                        , message: message
                        , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = ArticleCode + "_" + compoCode }
                        , autoDismiss: false
                        , locationID: null
                        , projectID: od.ProjectID
                        , actionController: null);

                    throw new Exception("An error occurred while saving the composition.");
                }

            }
        }

        public void ChangeArticle(OrderPluginData od, IPrinterJob job)
        {
            IArticle newarticle = null;

            var lst = ArticleCategoryLst.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault();

            if(lst == null) return;

            newarticle = articleRepo.GetByCodeInProject(lst.ArticleCode, od.ProjectID);

            if(newarticle == null) return;
            
            //update printdata by articlecode
            printerJobRepo.UpdateArticle(job.ID, newarticle.ID);

            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var printerDetails = ctx.PrinterJobDetails
                    .Join(ctx.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj => ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj })
                    .Where(w => w.PrinterJob.CompanyOrderID == job.CompanyOrderID)
                    .Select(s => s.PrinterJobDetail)
                    .ToList();

                var catalogs = catalogRepo.GetByProjectID(ctx, od.ProjectID, true);
                var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));

                // bulk update
                var allIds = printerDetails.Select(s => s.ProductDataID);

                using (DynamicDB dynamicDB = connManager.CreateDynamicDB())
                {
                    dynamicDB.Conn.ExecuteNonQuery(
                    $@"UPDATE d SET                                       
                                    ArticleCode = @ArticleCode
                                FROM {detailCatalog.TableName} d
                                WHERE d.ID in  ({string.Join(',', allIds)})", newarticle.ArticleCode);
                }
            }
            
        }

        public void SaveComposition(CompositionDefinition compo, OrderPluginData od, Dictionary<string, string> compositionData, StringBuilder Symbols, StringBuilder careInstructions, List<OrderPluginData> orderData, string ciSeparator)
        {


            //Save composition
            log.LogMessage($"Save Generic Compo for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {compo.ID} )");
            //Symbols.Remove(Symbols.Length - 1, 1);


            compositionData.Add("ArticleCodeSelected", ArticleCategoryLst.Last().ArticleCode);
            var cleanedCi = Regex.Replace(careInstructions.ToString().Trim(), ciSeparator + "$", string.Empty);// PERSONALIZED TRIM END
            orderUtilService.SaveComposition(orderData[0].ProjectID, compo.ID, compositionData, cleanedCi , Symbols.ToString());
        }

        private void AddOrRemoveEmptyArticle(PrintDB ctx, IOrder baseOrder/*, IList<OrderDetailDTO> artilcesInOrder*/, bool containsArticles)
        {

            // if contain an article remove EMPTY_ARTICLE_CODE, else keep EMPTY_ARTICLE_CODE
            //var found = artilcesInOrder.Where(w => w.ArticleCode.Equals(Article.EMPTY_ARTICLE_CODE)).ToList();
            //if (found.Count() > 0)
            //{

            //    var o = found.First();
            //    orderRepo.ChangeStatus(o.OrderID, OrderStatus.Cancelled);
            //}

            //// always return a record for Scalpers
            //var found = ctx.CompanyOrders
            //        .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { CompanyOrders = o, PrinterJobs = j })
            //        .Join(ctx.Articles, join1 => join1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.CompanyOrders, j1.PrinterJobs, Articles = a })
            //        .Join(ctx.OrderUpdateProperties, join2 => join2.CompanyOrders.ID, props => props.OrderID, (j2,p) => new { j2.CompanyOrders, j2.PrinterJobs, j2.Articles, OrderUpdateProperties = p })
            //        .Where(w => w.CompanyOrders.OrderGroupID.Equals(baseOrder.OrderGroupID) && w.Articles.ArticleCode.Equals(Article.EMPTY_ARTICLE_CODE))
            //        .Where(w => w.OrderUpdateProperties.IsRejected != true)
            //        .Select(s => s.CompanyOrders.ID).ToList();

            //log.LogMessage($"Buscando articulo default se encontraron : [{found.Count()}], Se han agregado Articulos ?: [{containsArticles}]");

            if (containsArticles)
            {
                //var o = found.First();
                orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.Cancelled);
            }
            else
            {
                //var o = found.First();
                orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.InFlow);
            }

        }

        private void GetSymbols(CompositionDefinition compo, StringBuilder Symbols, Dictionary<string, string> compositionData)
        {

            var symbols = new List<string>();
            foreach (var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if (ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    

                }
            }
            Symbols.Append(string.Join(",", symbols)); // TODO: save symbol separator inner configuration

            compositionData.Remove("Symbols"); 
            compositionData.Add("Symbols", Symbols.ToString());
        }
        //}
        /// <summary>
        /// Zara require only the symbols
        /// to be a generic concat the ci text inner FullComposition Field
        /// Show the additionals and Exceptions at the end inner separated pages
        /// 
        /// first option Don't Try to split rules inner multiples pages
        /// 
        /// last option Some times the additional rules not fit inner a page, this require change the strategy fill the page translation by translation
        /// to split the rule
        /// </summary>
        /// <param name="compo"></param>
        /// <param name="careInstructions"></param>
        /// <param name="additionals"></param>
        /// <param name="Symbols"></param>
        /// <param name="ciLanguageSeparator"></param>
        /// <param name="ciSeparator"></param>
        /// <param name="compositionData"></param>
        /// <param name="materialsFaces"></param>
        /// <param name="linenumber"></param>
        public void AdditionalByPage(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder Symbols, Dictionary<string, string> compositionData, RibbonFace materialsFaces, int linenumber)
        {
            var symbols = new List<string>();
            var addExc = new List<string>(); // additionals and exceptions
            var basic = new List<string>();

            var addExcTable = new Dictionary<int, List<string>>();

            //foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
                foreach (var ci in compo.CareInstructions)
                {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if (ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if (ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // split by page additionals and exceptions
            List<List<string>> filledPages;
            var strategy = 1;
            if (!FillCareInstructionsPagesOneByOne(addExc, materialsFaces, linenumber, out filledPages))
            {
                if (!FillCareInstructionsPagesByTranslations(addExcTable, materialsFaces, linenumber, out filledPages))
                {
                    throw new Exception("ZaraCompoPlugin ERROR - Can't set Additional text");
                }

                strategy = 2;
            }


            // set where page text will be saved
            for (int j = 0; j < filledPages.Count; j++)
            {
                string f = string.Empty;

                if (strategy == 1)
                    f = string.Join(Environment.NewLine, filledPages[j]);
                else
                    f = string.Join(String.Empty, filledPages[j]);

                var add = "AdditionalPage" + (j + 1);
                compositionData.Add(add, f);
            }



            // the keys of the composition data is a hardcode definition for CompositionLabel table for the current project

            careInstructions.Append(string.Join(CI_SEPARATOR, basic));

            Symbols.Append(string.Join(",", symbols)); // TODO: save symbol separator inner configuration
            compositionData.Add("Symbols", Symbols.ToString());
            compositionData.Add("FullCareInstructions", string.Join(CI_SEPARATOR, basic));
            
            compositionData.Add("FullAdditionals", Regex.Replace(additionals.ToString().Trim(), CI_SEPARATOR + "$", string.Empty));
            compositionData.Add("AdditionalsNumber", filledPages.Count.ToString());


        }

        private bool FillCareInstructionsPagesOneByOne(IList<string> joinedTranslations, RibbonFace materialsFaces, int allowedLines, out List<List<string>> filledPages)
        {
            filledPages = new List<List<string>>();
            var currentPage = new List<string>();
            var fit = true;

            filledPages.Add(currentPage);

            for (int pos = 0; pos < joinedTranslations.Count; pos++)
            {
                var text = joinedTranslations[pos];
                var pageText = string.Join(CI_SEPARATOR, currentPage.Append(text));

                FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                if (obj.Fit == false)
                {
                    // check if make fit inner blank page the current text
                    if (currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
                    {
                        currentPage = new List<string>(); // new page
                        filledPages.Add(currentPage);
                    }
                    else
                    {
                        // no fit inner blank page
                        fit = false;
                        break;
                    }
                }

                currentPage.Add(text);

            }

            
            if(currentPage.Count < 1 && filledPages.Count > 0)
                filledPages.RemoveAt(filledPages.Count - 1);

            //if (!fit)
            //    filledPages = null;

            return fit;
        }

        private bool FillCareInstructionsPagesByTranslations(Dictionary<int, List<string>> ciText, RibbonFace materialsFaces, int allowedLines, out List<List<string>> filledPages)
        {
            filledPages = new List<List<string>>();
            var currentPage = new List<string>();
            var fit = true;

            filledPages.Add(currentPage);

            foreach (var ciAllLangs in ciText.Values)
            {

                foreach (var text in ciAllLangs)
                {
                    var testingText = currentPage.Count > 0 ? CI_LANG_SEPARATOR + text : text;
                    var pageText = string.Join("", currentPage.Append(testingText));

                    FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                    var isLastASeparator = currentPage.Count < 1 ? false : currentPage.Last() == CI_SEPARATOR || currentPage.Last() == CI_LANG_SEPARATOR;

                    if (obj.Fit == false)
                    {
                        // check if make fit inner blank page the current text
                        if (currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
                        {
                            if (isLastASeparator)
                                currentPage.RemoveAt(currentPage.Count - 1);

                            currentPage = new List<string>(); // new page
                            filledPages.Add(currentPage);
                        }
                        else
                        {
                            // no fit inner blank page
                            fit = false;
                            break;
                        }
                    }

                    if (currentPage.Count > 0 && isLastASeparator == false)
                        currentPage.Add(CI_LANG_SEPARATOR);
                    currentPage.Add(text);
                }

                currentPage.Add(CI_SEPARATOR);

            }

            if(currentPage.Count < 1 && filledPages.Count > 0)
                filledPages.RemoveAt(filledPages.Count);

            return fit;
        }

        private List<string> ReArrangeSection(string sectionValue)
        {
            if (sectionValue.IndexOf('/') < 0)
            {
                return new List<string>() { sectionValue }; 
            }

            List<string> sections = new List<string>();
            string[] outerArray = sectionValue.Split('-');

            foreach (string part in outerArray)
            {
                var wordsOfSection = part.Split('/');
                int countOfSections = 0;
                foreach (string word in wordsOfSection)
                {
                    if (sections.Count < countOfSections + 1)
                    {
                        sections.Add(word.Trim());
                    }
                    else
                    {
                        sections[countOfSections] += "- " + word.Trim();
                    }
                    countOfSections++;
                }
            }

            //var sectionValueReArrange = string.Empty;
            //foreach (string word in sections)
            //{
            //    if (string.IsNullOrEmpty(sectionValueReArrange))
            //    {
            //        sectionValueReArrange = word;
            //    }
            //    else
            //    {
            //        sectionValueReArrange += "- " + word;
            //    }

            //}

            return sections;

        }

        private List<string> GetSectionFibers(Section section)
        {
            var list = new List<string>();  
            for (var f = 0; f < section.Fibers.Count; f++)
            {
                var langsListFiber = section.Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                var fiberValue = (langsListFiber.Count() > 1 ? String.Join(FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First());
                list.Add(fiberValue);
            }
            return list;
        }

        public List<CompositionTextDTO> CreateFiberList(WebLink.Contracts.Models.CompositionDefinition compo, int fillingWeightId = -1, string fillingWeightText= "", bool isSeparatedPercentage = true, int exceptionsLocation = 0)
        {
            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            for (var i = 0; i < compo.Sections.Count; i++)
            {
                var langsListSection = compo.Sections[i].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                var sectionValue = langsListSection.Count() > 1 ? String.Join(SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();

                //if composition have one section
                if (compo.Sections.Count > 1)
                {
                    var sectionText = ReArrangeSection(sectionValue);
                    if (sectionText.Count == 1)
                    {
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = sectionText.First(),
                            FiberType = "TITLE",
                            TextType = TextType.Title,
                            Langs = langsListSection.ToList(), 
                            SectionFibersText = GetSectionFibers(compo.Sections[i])
                        });
                    }else
                    {
                        bool first = true; 
                        foreach (var section in sectionText)
                        {
                            if (first)
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "TITLE",
                                    TextType = TextType.Title,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i])
                                });
                                first = false;
                            }else
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "MERGETITLE",
                                    TextType = TextType.MergeTitle,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i])
                                });
                            }

                        }
                    }
                }

                for (var f = 0; f < compo.Sections[i].Fibers.Count; f++)
                {
                    var langsListFiber = compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                    var fiberValue = (langsListFiber.Count() > 1 ? String.Join(FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First());

                    list.Add(new CompositionTextDTO
                    {
                        Percent = isSeparatedPercentage ? compo.Sections[i].Fibers[f].Percentage + "%": string.Empty,
                        Text = isSeparatedPercentage ? fiberValue : $"{compo.Sections[i].Fibers[f].Percentage + "%"} {fiberValue}",
                        FiberType = compo.Sections[i].Fibers[f].FiberType,
                        TextType = TextType.Fiber,
                        Langs = langsListFiber.ToList()
                    });
                }

                // Add Exceptions only to the first section
                if (i == exceptionsLocation) // i == ExceptionsLocation 
                {
                    compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.EXCEPTION).ForEach(ci =>
                    {

                        var langsList = ci.AllLangs
                        .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct();

                        var translated = langsList.Count() > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList.First();
                        if (fillingWeightId != 1 && fillingWeightId == ci.Instruction)
                        {
                            translated = $"{fillingWeightText} {translated}"; 
                        }
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = translated,
                            FiberType = string.Empty,
                            TextType = TextType.CareInstruction,
                            Langs = langsList.ToList()
                        });
                    });
                }

            }
            return list;
        }

        public List<CompositionTextDTO> CreateCareInstructionsList(WebLink.Contracts.Models.CompositionDefinition compo) {

            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            foreach(var ci in compo.CareInstructions)
            {

                var langsList = ci.AllLangs
                        .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct();

                var translated = langsList.Count() > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList.First();

                list.Add(new CompositionTextDTO
                {
                    Percent = string.Empty,
                    Text = translated,
                    FiberType = ci.Category,
                    TextType = TextType.CareInstruction,
                    Langs = langsList.ToList()
                });
            }
            return list;
        }
        private string SplitComposition(List<CompositionTextDTO> lst)
        {
            string concatFibers = string.Empty;

            for (var i = 0; i < lst.Count; i++)
            {
                concatFibers += string.Format("{0}{1}", lst[i].Text, Environment.NewLine);
            }
            return concatFibers;
        }

        public void Dispose()
        {
        }

        public void ClearCompositionLabels(int projectId, int compoId, int totalfibers, string careInstructions, string symbols)
        {
            var compositionData = new Dictionary<string, string>();

            for (int j = 0; j < totalfibers; j++)
            {
                var percent = "Page" + (j + 1) + "_percent";
                var fiber = "Page" + (j + 1) + "_compo";
                var fibertype = "Page" + (j + 1) + "_leather";

                compositionData.Add(percent, string.Empty);
                compositionData.Add(fiber, string.Empty);
                compositionData.Add(fibertype, string.Empty);
            }

            orderUtilService.SaveComposition(projectId, compoId, compositionData, careInstructions, symbols);
        }

        public void ClearAdditionalPages(int projectId, int compoId, int totalpages)
        {
            var compositionData = new Dictionary<string, string>();

            for (int j = 0; j < totalpages; j++)
            {
                var page = "AdditionalPage" + (j + 1);

                compositionData.Add(page, string.Empty);
            }

            compositionData.Add("AdditionalsNumber", string.Empty);
            orderUtilService.SaveComposition(projectId, compoId, compositionData, string.Empty, string.Empty);
        }

        #endregion

        #region Fit Methods

        public FitObj ContentFitsByLines(Font font, RibbonFace materials, string text, int linenumber)
        {
            StringFormat sfFmt = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
            Graphics g = Graphics.FromImage(new Bitmap(100, 100));

            var iHeight = (decimal)g.MeasureString(text, font, new SizeF(materials.WidthInPixels, materials.HeightInPixels), sfFmt, out var chracterFitted, out int linesFilled).Height;

            var iOneLineHeight = (decimal)g.MeasureString("Z", font, (int)Math.Ceiling(materials.WidthInPixels), sfFmt).Height;

            int iNumLines = (int)Math.Ceiling(((decimal)iHeight / iOneLineHeight));

            return new FitObj { Fit = iNumLines <= linenumber, Lines = iNumLines };
        }

        public bool ContentFits(RibbonFace face, string text)
        {
            var size = g.MeasureString(text, face.Font, (int)face.WidthInPixels, new StringFormat(StringFormatFlags.MeasureTrailingSpaces));
            var result = size.Height < face.HeightInPixels;
            return result;
        }

        public FitObj ContentFitsByLayout(RibbonFace materials, string text, int lines)
        {
            StringFormat sfFmt = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
            var bmp = new Bitmap((int)(materials.WidthInPixels + 100), (int)(materials.HeightInPixels + 100));
            Graphics g = Graphics.FromImage(bmp);

            int charactersFitted;
            int linesFilled;
            SizeF stringSize = new SizeF();
            SizeF addSize = new SizeF(materials.WidthInPixels, materials.HeightInPixels * 5);
            stringSize = g.MeasureString(text, materials.Font== null ? new Font("Arial Unicode MS", 6, FontStyle.Regular) : materials.Font, addSize, sfFmt, out charactersFitted, out linesFilled);

#if DEBUG

            //RectangleF rectF1 = new RectangleF(0, 0, materials.WidthInPixels, materials.HeightInPixels);
            //g.DrawRectangle(Pens.Black, Rectangle.Round(rectF1));
            ////g.DrawString(text, materials.Font, Brushes.Black, 0,0);
            //TextRenderer.DrawText(g, text, materials.Font, rectF1, Color.Blue, TextFormatFlags.WordBreak);

            ////g.Dispose();
            //var tmpServ = factory.GetInstance<TempFileService>();
            //var image = tmpServ.GetTempFileName("compo_zara.bmp");
            //bmp.Save(image);
#endif
            return new FitObj { Fit = linesFilled <= lines, Lines = linesFilled };
        }


        #endregion

    }
    
}
