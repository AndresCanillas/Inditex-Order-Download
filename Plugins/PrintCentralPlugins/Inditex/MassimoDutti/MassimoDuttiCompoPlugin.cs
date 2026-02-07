using LinqKit;
using Microsoft.CodeAnalysis;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.WizardPlugins
{
    [FriendlyName("Inditex.MassimoDutti(old) - Composition Text Plugin")]
    [Description("Inditex.MassimoDutti(old) - Composition Plugin")]
    public class MassimoDuttiCompoPlugin : IWizardCompositionPlugin
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
            public float WidthAdditional { get; internal set; }

            public bool WithSeparatedPercentage = true;

            public int DefaultCompresion = -1;
            public int PPI = 96;
        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            var od = orderData.FirstOrDefault();
            var translations = GetLanguageDictionary();

            if(isLoad)
            {
                var composition = orderUtilService
                            .GetComposition(od.OrderGroupID, true, translations, OrderUtilService.LANG_SEPARATOR)
                            .OrderBy(c => c.ID).FirstOrDefault(c => c.ID == id);
                var compositionData = new Dictionary<string, string>();
                if(composition != null)
                {
                    compositionData = orderUtilService.GetCompositionData(orderData.FirstOrDefault().ProjectID, composition.ID);
                    if(compositionData != null)
                    {
                        var article = GetArticleCode(od.OrderID);
                        var articleConfig = GetArticleConfig(article);
                        int fiberCompress = GetFiberCompress(compositionData);
                        int additionalsCompress = GetAdditionalsCompress(compositionData);
                        string FiberInSpecificLang = GetTextInSpecificLang(compositionData);     
                        return new List<PluginCompoPreviewData>() {
                            new PluginCompoPreviewData()
                            {
                                Lines = articleConfig.LineNumber,
                                Width = articleConfig.WidthInches,
                                Heigth= articleConfig.HeightInInches,
                                WidthAdditional = articleConfig.WidthAdditional,
                                CompoData = compositionData ,
                                CompositionText = GetCompostionText( compositionData, composition, articleConfig.WithSeparatedPercentage),
                                FiberCompress = fiberCompress == -1 ? articleConfig.DefaultCompresion: fiberCompress,
                                AdditionalsCompress = additionalsCompress == -1 ?  articleConfig.DefaultCompresion : additionalsCompress,
                                PPIValue = articleConfig.PPI,
                                FibersInSpecificLang = FiberInSpecificLang   

                            }
                        };
                    }

                }
            }

            GenerateCompositionTextByCompoId(orderData, id);

            return PluginCompoPreviewData;
        }

        private int GetFiberCompress(Dictionary<string, string> compositionData)
        {
            int fiberCompress = -1;
            if(compositionData.TryGetValue("FiberCompress", out string value))
            {
                if(!string.IsNullOrEmpty(value))
                    int.TryParse(value, out fiberCompress);
            }

            return fiberCompress;

        }

        private string GetTextInSpecificLang (Dictionary<string,string> compositionData)
        {
            string text = string.Empty;
            if (compositionData.TryGetValue("FibersInSpecificLang", out string value))
            {
                text = value;
            }

            return text; 
        }

        private int GetAdditionalsCompress(Dictionary<string, string> compositionData)
        {
            int additionalCompress = -1;
            if(compositionData.TryGetValue("AdditionalCompress", out string value))
            {
                if(!string.IsNullOrEmpty(value))
                    int.TryParse(value, out additionalCompress);
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


        public MassimoDuttiCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService, IPrinterJobRepository printerJobRepo, IArticleRepository articleRepo, INotificationRepository notificationRepo, IDBConnectionManager connManager, ICatalogRepository catalogRepo, IFactory factory, IUserManager userManager, IOrderRepository orderRepo, IUserData userData)
        {
            this.events = events;
#if DEBUG
            this.log = log.GetSection("Debug");
#endif

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

        private void GenerateCompositionTextByCompoId(List<OrderPluginData> orderData, int id)
        {
            LogMessage("MASSIMO DUTTY - GenerateCompositionTextByCompoId ");

            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            int indexCompo = 0;
            float widthInInches = 0;
            float heightInInches = 0;
            float widthAdditionalInInches = 0;
            string SelectedLanguage = "English";

            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? Environment.NewLine : projectData.SectionsSeparator;
            SECTION_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBER_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? Environment.NewLine : projectData.FibersSeparator;
            FIBER_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;

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
            font = new Font("Arial Unicode MS", 6, FontStyle.Regular);// the font using on the labels

            //create list fiber per composition
            foreach(var od in orderData)
            {
                var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR).Where(c => c.ID == id);

                totalCompositions = compositions.Count();

                IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);



                foreach(var compo in compositions)
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

                    foreach(var job in printer_job)
                    {
                        currentArticle = articleRepo.GetByID(job.ArticleID);
                        artCode = currentArticle.ArticleCode.Contains("_") ? currentArticle.ArticleCode.Substring(0, currentArticle.ArticleCode.LastIndexOf("_")) : currentArticle.ArticleCode;
                    }

                    var artConfig = GetArticleConfig(artCode);


                    switch(artCode)
                    {
                        case "COMPO-110x25-BLACK":
                        case "COMPO-110x25-WHITE":

                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);  //compo
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 3.6466f, 0.7874f); // area floating arround triman symbol
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            widthAdditionalInInches = materialsFaces2.WidthInInches;
                            withSeparatedPercentage = artConfig.WithSeparatedPercentage;
                            IsSimpleAdditional = false;
                            SelectedLanguage = "English";
                            break;

                        case "COMPO-60x40-BLACK":
                        case "COMPO-60x40-WHITE":


                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            materialsFaces2 = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            widthAdditionalInInches = 1.895f;
                            SelectedLanguage = "English";
                            IsSimpleAdditional = false;
                            break;

                        case "24MDC-PP-CARE":

                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            materialsFaces2 = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            widthAdditionalInInches = materialsFaces.WidthInInches;
                            withSeparatedPercentage = artConfig.WithSeparatedPercentage;
                            IsSimpleAdditional = true;
                            SelectedLanguage = "English";
                            break;
                        case "24MDH-0201-CW":
                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            materialsFaces2 = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            widthAdditionalInInches = 1.895f;
                            SelectedLanguage = "English";
                            IsSimpleAdditional = false;
                            withSeparatedPercentage = artConfig.WithSeparatedPercentage;
                            break;
                    }

                    //get list Fibers
                    listfibers = CreateFiberList(compo, od.FillingWeightId, od.FillingWeightText, withSeparatedPercentage, FibersLanguage.ToList(), SelectedLanguage);
                    listcareinstructions = CreateCareInstructionsList(compo);

                    CompositionText = new List<PluginCompositionTextPreviewData>();
                    foreach(var fiber in listfibers)
                    {
                        CompositionText.Add(new PluginCompositionTextPreviewData()
                        {
                            FiberType = string.IsNullOrEmpty(fiber.Percent) && string.IsNullOrEmpty(fiber.FiberType) ? "TITLE" : fiber.FiberType,
                            Langs = fiber.Langs,
                            Percent = withSeparatedPercentage ? fiber.Percent : string.Empty,
                            Text = fiber.Text,
                            IsTitle = fiber.TextType == TextType.Title,
                            SectionFibersText = fiber.SectionFibersText,
                        });
                    }
                    CareInstructionsText = new List<PluginCompositionTextPreviewData>();
                    foreach(var ciText in listcareinstructions)
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


                    CalculateComposition(compo, compositionData, listfibers, orderData, materialsFaces, allowedLinesByPage, withSeparatedPercentage);

                    SetFibersInSpecificLanguage(compositionData, listfibers);

                    if(IsSimpleAdditional)
                    {
                        AdditionalSimple(compo, careInstructions, additionals, Symbols, CI_LANG_SEPARATOR, CI_SEPARATOR, compositionData);
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
                    if(compositionData.TryGetValue("AdditionalsNumber", out additionalnumber))
                        total_addcare_pages = int.Parse(additionalnumber);

                    CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);

                    SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);
                    if(PluginCompoPreviewData == null)
                    {
                        PluginCompoPreviewData = new List<PluginCompoPreviewData>();
                    }



                    PluginCompoPreviewData.Add(new PluginCompoPreviewData()
                    {
                        CompoData = compositionData,
                        CompositionText = CompositionText,
                        CareInstructionsText = CareInstructionsText,
                        Symbols = Symbols.ToString(),
                        Lines = allowedLinesByPage,
                        Heigth = heightInInches,
                        Width = widthInInches,
                        WidthAdditional = widthAdditionalInInches,
                        WithSeparatedPercentage = withSeparatedPercentage,
                        PPIValue = artConfig.PPI,
                        FibersInSpecificLang = compositionData["FibersInSpecificLang"]

                    });

                    indexCompo++;
                }

                ChangeArticle(od, printer_job.ElementAt(0));
            }

        }

        private void SetFibersInSpecificLanguage(Dictionary<string, string> compositionData, List<CompositionTextDTO> listfibers)
        {
            
            var sb = new StringBuilder(); 
            foreach(var fiber in listfibers)
            {
                if (string.IsNullOrEmpty(fiber.TextSelectedLanguage))
                    continue;
                sb.Append(fiber.TextSelectedLanguage + Environment.NewLine); 
            }

            compositionData.Add("FibersInSpecificLang", sb.ToString());
        }


        #region Methods

        private RibbonFace GetRibbonFace(string artCode)
        {

            font = new Font("Arial Unicode MS", 6, FontStyle.Regular);

            RibbonFace ribbonFace = null;

            var artConfig = GetArticleConfig(artCode);

            ribbonFace = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);  //compo

            return ribbonFace;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {

            return;

            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            int indexCompo = 0;
            float widthInInches = 0;
            float heightInInches = 0;
            string selectedLanguage = "English";    

            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            SECTION_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBER_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            FIBER_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;

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
            foreach(var od in orderData)
            {
                var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, ZaraLanguage, OrderUtilService.LANG_SEPARATOR);

                totalCompositions = compositions.Count();

                IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);



                foreach(var compo in compositions)
                {
                    bool withSeparatedPercentage = true;
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

                    foreach(var job in printer_job)
                    {
                        currentArticle = articleRepo.GetByID(job.ArticleID);
                        artCode = currentArticle.ArticleCode.Contains("_") ? currentArticle.ArticleCode.Substring(0, currentArticle.ArticleCode.LastIndexOf("_")) : currentArticle.ArticleCode;
                    }

                    var artConfig = GetArticleConfig(artCode);

                    switch(artCode)
                    {
                        case "COMPO-110x25-BLACK":
                        case "COMPO-110x25-WHITE":
                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);  //compo
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 3.6466f, 0.7874f); // area floating arround triman symbol
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            selectedLanguage = "English";
                            IsSimpleAdditional = false;
                            withSeparatedPercentage = false;
                            break;

                        // 60x25
                        case "COMPO-60x40-BLACK":
                        case "COMPO-60x40-WHITE":


                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.9732f, 1.4350f);  //46x23mm compo 
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9732f, 1.4350f);  //50x23mm aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = 15;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            selectedLanguage = "English";

                            IsSimpleAdditional = false;
                            break;

                        case "24MDC-PP-CARE":
                            materialsFaces = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);  //compo
                            materialsFaces2 = new RibbonFace(font, artConfig.WidthInches, artConfig.HeightInInches);
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            selectedLanguage = "English";
                            IsSimpleAdditional = false;
                            withSeparatedPercentage = false;
                            break;
                        case "24MDH-0201-CW":
                            font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                            materialsFaces = new RibbonFace(font, 1.1024f, 1.4350f);  //28x23mm compo 
                            materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.1024f, 1.4350f);  //28x23mm aqui se restaron 6 pixeles al ancho
                            allowedLinesByPage = artConfig.LineNumber;
                            heightInInches = materialsFaces.HeightInInches;
                            widthInInches = materialsFaces.WidthInInches;
                            selectedLanguage = "English";
                            withSeparatedPercentage = false; 
                            IsSimpleAdditional = false;
                            break;
                    }

                    //get list Fibers
                    listfibers = CreateFiberList(compo, od.FillingWeightId, od.FillingWeightText, withSeparatedPercentage, FibersLanguage.ToList(), selectedLanguage);
                    CompositionText = new List<PluginCompositionTextPreviewData>();
                    foreach(var fiber in listfibers)
                    {
                        CompositionText.Add(new PluginCompositionTextPreviewData()
                        {
                            FiberType = fiber.FiberType,
                            Langs = fiber.Langs,
                            Percent = withSeparatedPercentage ? fiber.Percent : string.Empty,
                            Text = fiber.Text,
                            IsTitle = fiber.TextType == TextType.Title,
                            SectionFibersText = fiber.SectionFibersText,
                        });
                    }

                    CalculateComposition(compo, compositionData, listfibers, orderData, materialsFaces, allowedLinesByPage, withSeparatedPercentage);
                    SetFibersInSpecificLanguage(compositionData, listfibers);
                    if(IsSimpleAdditional)
                    {
                        AdditionalSimple(compo, careInstructions, additionals, Symbols, CI_LANG_SEPARATOR, CI_SEPARATOR, compositionData);
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
                    if(compositionData.TryGetValue("AdditionalsNumber", out additionalnumber))
                        total_addcare_pages = int.Parse(additionalnumber);

                    CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);

                    //SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);
                    if(PluginCompoPreviewData == null)
                    {
                        PluginCompoPreviewData = new List<PluginCompoPreviewData>();
                    }
                    PluginCompoPreviewData.Add(new PluginCompoPreviewData() { 
                        CompoData = compositionData, 
                        CompositionText = CompositionText, 
                        Symbols = Symbols.ToString(), 
                        CareInstructions = careInstructions.ToString(), 
                        Lines = allowedLinesByPage, 
                        Heigth = heightInInches, 
                        Width = widthInInches,
                        WithSeparatedPercentage = withSeparatedPercentage,
                        PPIValue = artConfig.PPI,
                        FibersInSpecificLang = compositionData["FibersInSpecificLang"]
                    });

                    indexCompo++;
                }

                ChangeArticle(od, printer_job.ElementAt(0));
            }
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
            CompositionDefinition compo = new CompositionDefinition();
            var projectData = orderUtilService.GetProjectById(od.ProjectID);
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();

            string artCode = GetArticleCode(od.OrderID);
            Dictionary<CompoCatalogName, IEnumerable<string>> translations = GetLanguageDictionary();
            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, translations, OrderUtilService.LANG_SEPARATOR);

            var compoSource = compositions.FirstOrDefault(c => c.ID == sourceId);
            if(compoSource == null)
            {
                return;
            }
            foreach(var composition in compositions)
            {
                if(targets.Any(t => t == composition.ID))
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
                                      string[] compoArray,
                                      string[] percentArray,
                                      string[] leatherArray,
                                      string[] additionalArray,
                                      int labelLines,
                                      int ID,
                                      int additionalsCompress,
                                      int fiberCompress, 
                                      string FiberInSpecificLang)
        {

            LogMessage("MASSIMO DUTTY - SaveCompoPreview");


            CompositionDefinition compo = new CompositionDefinition();
            Dictionary<string, string> compositionData = new Dictionary<string, string>();
            var IsSimpleAdditional = false;
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();
            var additionals = new StringBuilder();
            int totalPages = 10;
            //int lineNumber = 0;
            int indexCompo = 0;
            var font = new Font("Arial Unicode MS", 6, FontStyle.Regular);// XXX: esta variable es redundante

            var projectData = orderUtilService.GetProjectById(od.ProjectID);
            var ciSeparator = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_SEPARATOR = ciSeparator;
            string artCode = GetArticleCode(od.OrderID);

            Dictionary<CompoCatalogName, IEnumerable<string>> translations = GetLanguageDictionary();

            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, translations, OrderUtilService.LANG_SEPARATOR);
            compo = compositions.FirstOrDefault(c => c.ID == ID);
            if(compo == null)
            {
                return;
            }

            int index, acumLabels;
            GetCompositionDataLeatherLines(leatherArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataCompoLines(compoArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataPercentLines(percentArray, labelLines, compositionData, out index, out acumLabels);

            compositionData.Add("AdditionalCompress", additionalsCompress.ToString());
            compositionData.Add("FiberCompress", fiberCompress.ToString());
            compositionData.Add("FibersInSpecificLang", FiberInSpecificLang);   

            var labelsCount = acumLabels - 1 == 0 ? 1 : acumLabels - 1;
            IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);
            compositionData.Add("ComposNumber", (acumLabels - 1).ToString());

            List<OrderPluginData> orderData = new List<OrderPluginData> { od };
            RibbonFace materialsFaces2 = new RibbonFace();
            materialsFaces2.Font = font;
            var ciLanguageSeparator = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;

            if(IsSimpleAdditional)
            {
                AdditionalSimple(compo, careInstructions, additionals, Symbols, ciLanguageSeparator, ciSeparator, compositionData);

            }
            else
            {
                ClearAdditionalPages(od.ProjectID, compo.ID, totalPages);
                if(additionalArray.Count() > 0)
                {
                    GetSymbols(compo, Symbols, compositionData);
                    GetAdditionalTextLines(additionalArray, labelLines, additionals, compositionData, out index, out acumLabels);
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
            if(compositionData.TryGetValue("AdditionalsNumber", out string additionalnumber))
                total_addcare_pages = int.Parse(additionalnumber);
            GetNumberOfLinesPage1(compoArray, labelLines, total_compo_pages);
            CalculateArticle(artCode, compo, indexCompo, od, additionals, total_compo_pages, total_addcare_pages);
            if(page1_totallines == 0)
            {
                throw new Exception("Page 1 without lines"); 
            }
            SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, CI_SEPARATOR);

            var compositionsSaved = orderUtilService.GetComposition(od.OrderGroupID, true, translations, OrderUtilService.LANG_SEPARATOR);

            ChangeArticle(od, printer_job.ElementAt(0));
        }

        private void GetNumberOfLinesPage1(string[] compoArray, int labelLines, int totalCompoPages)
        {
            page1_totallines = 0;
            if(totalCompoPages > 1)
            {
                page1_totallines = labelLines;
            } else
            {

                for(int i = 0; i < compoArray.Length; i++)
                {
                    if(!string.IsNullOrEmpty(compoArray[i].Trim()))
                    {
                        page1_totallines++;
                    }
                }

            }




        }

        private void GetCareInstructions(CompositionDefinition compo, StringBuilder careInstructions, Dictionary<string, string> compositionData, RibbonFace materialsFaces, int linenumber)
        {
            var symbols = new List<string>();
            var addExc = new List<string>(); // additionals and exceptions
            var basic = new List<string>();

            var addExcTable = new Dictionary<int, List<string>>();

            //foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if(ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // split by page additionals and exceptions
            //List<List<string>> filledPages;
            //var strategy = 1;
            //if (!FillCareInstructionsPagesOneByOne(addExc, materialsFaces, linenumber, out filledPages))
            //{
            //    if (!FillCareInstructionsPagesByTranslations(addExcTable, materialsFaces, linenumber, out filledPages))
            //    {
            //        throw new Exception("ZaraCompoPlugin ERROR - Can't set Additional text");
            //    }

            //    strategy = 2;
            //}


            //// set where page text will be saved
            //for (int j = 0; j < filledPages.Count; j++)
            //{
            //    string f = string.Empty;

            //    if (strategy == 1)
            //        f = string.Join(Environment.NewLine, filledPages[j]);
            //    else
            //        f = string.Join(String.Empty, filledPages[j]);

            //    var add = "AdditionalPage" + (j + 1);
            //    compositionData.Add(add, f);
            //}



            // the keys of the composition data is a hardcode definition for CompositionLabel table for the current project
            careInstructions.Append(string.Join(CI_SEPARATOR, basic));
            compositionData.Add("FullCareInstructions", string.Join(CI_SEPARATOR, basic));


        }

        private void GetAdditionalTextLines(string[] additionalArray, int labelLines, StringBuilder additionals, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;

            while(index < additionalArray.Length)
            {
                var additionalLabel = additionalArray.Skip(index).Take(labelLines);
                StringBuilder additionals_label = new StringBuilder();
                foreach(var item in additionalLabel)
                {
                    additionals_label.Append($"{item}{Environment.NewLine}");
                    additionals.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"AdditionalPage{acumLabels.ToString()}", additionals_label.ToString());
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

            foreach(var job in printer_job)
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
            while(index < percentArray.Length)
            {
                var percentLabel = percentArray.Skip(index).Take(labelLines);
                StringBuilder percentLabelText = new StringBuilder();
                foreach(var item in percentLabel)
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
            for(int i = 0; i < quantityOfPages; i++)
            {
                var leatherPositionStrings = string.Empty;
                int lineNumber = 0;
                for(int j = lines; j < (i * labelLines) + labelLines; j++)
                {
                    if(leatherArray[j] == "1")
                    {
                        if(string.IsNullOrEmpty(leatherPositionStrings))
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
            while(index < compoArray.Length)
            {
                var compolabel = compoArray.Skip(index).Take(labelLines);
                StringBuilder labelText = new StringBuilder();
                foreach(var item in compolabel)
                {
                    //labelText.Append(item.ToString().Concat(Environment.NewLine));
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
            bool withSeparatedPercentage = true;
            int defaultCompression = 0;
            int ppivalue = 96;

            switch(artCode)
            {
                case "COMPO-110x25-BLACK":
                case "COMPO-110x25-WHITE":
                    // new RibbonFace(font, 3.6466f, 0.92756f);
                    lineNumber = 9;
                    widthInInches = 3.6466f;
                    heightInInches = 0.92756f;
                    withSeparatedPercentage = false;
                    ppivalue = 96;
                    break;


                // 60x25
                case "COMPO-60x40-BLACK":
                case "COMPO-60x40-WHITE":


                    //  font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    //  materialsFaces = new RibbonFace(font, 1.825f, 2f);  //46x23mm compo 
                    widthInInches = 1.9732f;
                    heightInInches = 1.4350f;
                    ppivalue = 96;

                    //   materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 0.9055f);  //50x23mm 1.9685f adicionales V (Cristina)
                    lineNumber = 15;
                    //  IsSimpleAdditional = false;
                    break;

                case "24MDC-PP-CARE":
                    widthInInches = 1.6433f;
                    //widthInInches = 1.9733f;
                    heightInInches = 0.5283f;
                    lineNumber = 19;
                    withSeparatedPercentage = false;
                    defaultCompression = 70;
                    ppivalue = 163;
                    break;
                case "24MDH-0201-CW":
                    lineNumber = 19;
                    widthInInches = 1.773f;
                    heightInInches = 0.92756f;
                    withSeparatedPercentage = false;
                    ppivalue = 163;
                    break;
                   

            }

            return new ArticleConfig()
            {
                LineNumber = lineNumber,
                HeightInInches = heightInInches,
                WidthInches = widthInInches,
                WithSeparatedPercentage = withSeparatedPercentage,
                WidthAdditional = widthInInches,
                DefaultCompresion = defaultCompression,
                PPI = ppivalue

            };
        }

        private Dictionary<CompoCatalogName, IEnumerable<string>> GetLanguageDictionary()
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> Translations = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            Translations.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            Translations.Add(CompoCatalogName.FIBERS, FibersLanguage);
            Translations.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            Translations.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            Translations.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);
            return Translations;
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

            for(int pos = 0; pos < listfibers.Count(); pos++)
            {
                var currentText = listfibers[pos];

                if(currentText.TextType == TextType.CareInstruction)
                {
                    currentPage = new List<string>(); // new page
                    allPages.Add(currentPage);
                }

                //var pageText = string.Join(Environment.NewLine, currentPage.Append(currentText.Text));

                var fitResult = ContentFitsByLayout(materialsFaces, currentText.Text, allowedLines);

                // if single fiber text not fit, change strategy from block to phrase
                int appendStrategy = fitResult.Fit ? 0 : 1;

                if(appendStrategy == 0)
                {
                    AppendTextByBlock(allPages, currentPage, listfibers, pos, materialsFaces, allowedLines);
                }
                else
                {
                    AppendTextByPhrase(allPages, currentPage, listfibers, pos, materialsFaces, allowedLines);
                }


            }

            ClearCompositionLabels(orderData[0].ProjectID, compo.ID, 9, string.Empty, string.Empty);

            for(int j = 0; j < allPages.Count; j++)
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

            if(currentText.TextType == TextType.Title)
            {
                // si es un titulo revisar que hace fit con al menos una fibra que es el siguiente texto en la lista de fibras
                // ???: nunca llega un titulo sin fibras, de lo contrario sería necesario validar que existe el elemento "pos + 1", esto ya se valida en la definicion de la composición
                var nextText = listfibers[pos + 1];

                fitResult = ContentFitsByLayout(materialsFaces, string.Format("{0}{1}{2}", pageText, Environment.NewLine, nextText.Text), allowedLines);

                emptyFitResult = ContentFitsByLayout(materialsFaces, string.Format("{0}{1}{2}", currentText.Text, Environment.NewLine, nextText.Text), allowedLines);
            }


            // si no cabe el texto ni solo ni junto, cambio de estrategia
            if((currentPage.Count == 0 && fitResult.Fit == false) || (currentText.TextType == TextType.Title && emptyFitResult.Fit == false)) return false; // change strategy


            if(fitResult.Fit == false)
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

            if(currentText.TextType == TextType.Fiber)
            {
                LOCAL_SEPARATOR = FIBER_SEPARATOR;

                LOCAL_LANG_SEPARATOR = FIBER_LANG_SEPARATOR;
            }

            if(currentText.TextType == TextType.CareInstruction)
            {
                LOCAL_SEPARATOR = CI_SEPARATOR;

                LOCAL_LANG_SEPARATOR = CI_LANG_SEPARATOR;
            }

            foreach(var text in currentText.Langs)
            {
                var testingText = currentPage.Count > 0 ? LOCAL_SEPARATOR + text : text;

                var pageText = string.Join("", currentPage.Append(testingText));

                FitObj fitResult = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                var isLastASeparator = currentPage.Count < 1 && (currentPage.Last() == LOCAL_SEPARATOR || currentPage.Last() == LOCAL_LANG_SEPARATOR);

                if(fitResult.Fit == false)
                {

                    if(isLastASeparator == true)
                        currentPage.RemoveAt(currentPage.Count - 1);

                    currentPage = new List<string>(); // new page

                    allPages.Add(currentPage);

                }

                if(currentPage.Count > 0 && isLastASeparator == false)
                    currentPage.Add(LOCAL_LANG_SEPARATOR);

                currentPage.Add(text);
            }

            currentPage.Add(LOCAL_SEPARATOR);
        }




        public void CalculateComposition(CompositionDefinition compo, Dictionary<string, string> compositionData, List<CompositionTextDTO> listfibers, List<OrderPluginData> orderData, RibbonFace materialsFaces, int linenumber, bool isSeparatedPercentage = true)
        {
            //Fibers Algorithm
            var concatFibers = SplitComposition(listfibers);
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

                if(!isSeparatedPercentage && isFiber)
                {
                    text = $"{listfibers[pos].Percent} {text}"; // add % value to the text
                }

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

                        if(isSeparatedPercentage)
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

        public void AdditionalSimple(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder Symbols, string ciLanguageSeparator, string ciSeparator, Dictionary<string, string> compositionData)
        {
            foreach(var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            {
                var langsList = ci.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                if(ci.Symbol != string.Empty)
                {
                    //Symbols.Append(ci.Symbol); // TODO: now, always use FONT 
                    Symbols.Append(ci.Symbol + ",");
                }

                var translations = langsList.Length > 1 ? string.Join(ciLanguageSeparator, langsList) : langsList[0];

                if(ci.Category != "Additional" && ci.Category != "Exception")
                {
                    careInstructions.Append(translations);
                    careInstructions.Append(ciSeparator);
                }
                else
                {
                    additionals.Append(translations);
                    additionals.Append(ciSeparator);
                }
            }

            //set Additionals
            //compositionData.Add("FullAdditionals", additionals.ToString().TrimEnd(char.Parse(ciSeparator)));
            compositionData.Add("FullAdditionals", Regex.Replace(additionals.ToString().Trim(), $"({CI_SEPARATOR})+$", string.Empty));
        }
        private static (string compoCode, int PagesSize)? GetCompoCodeAndPagesSize(int sum_pages_compo_addcare)
        {
            if(sum_pages_compo_addcare < 1)
            {
                return null;
            }

            int PagesSize = sum_pages_compo_addcare - 1;

            double compoCodeValue = PagesSize / 2.0;

            // Formatear compoCode
            string compoCode = compoCodeValue % 1 == 0
                ? compoCodeValue.ToString("0")
                : compoCodeValue.ToString("0.0").Replace('.', '-');



            return (compoCode, PagesSize);
        }
        public void CalculateArticle(string ArticleCode, CompositionDefinition compo, int compoIndex, OrderPluginData od, StringBuilder additionals, int allCompoPages, int allAdditionalPages)
        {

            LogMessage("MASSIMO DUTTY - CALCULANDO ARTICULO [{0}] allCompoPages [{1}] allAdditionalPages[{2}]", ArticleCode, allCompoPages, allAdditionalPages);
            IEnumerable<IPrinterJob> printerjob = printerJobRepo.GetByOrderID(compo.OrderID, true);

            foreach(var job in printerjob)
            {
                //search new article
                var compoCode = "";
                int sum_pages_compo_addcare = 0;
                int PagesSize = 0;
                IArticle newarticle = null;

                switch(ArticleCode)
                {

                    case "COMPO-60x40-BLACK":
                    case "COMPO-60x40-WHITE":
                    case "COMPO-110x25-WHITE":
                    case "COMPO-110x25-BLACK":
                  

                        compoCode = "";
                        sum_pages_compo_addcare = allCompoPages + allAdditionalPages;

                        //if (allCompoPages == 1 && page1_totallines <= 3)
                        //{
                        //    sum_pages_compo_addcare -= 1;
                        //}

                        // TODO: use formula: compoCode ((sum_pages_compo_addcare - 1 )/ 2).ToString("0.0")
                        // pageSize Math.Floor(Convert.ToDecimal(compoCode) * 2)

                        if(additionals.Length == 0 && allCompoPages == 1 && page1_totallines <= 3)
                        {
                            compoCode = "0";
                            ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = 0 });

                        } else
                        {
                            //var result = GetCompoCodeAndPagesSize(sum_pages_compo_addcare);

                            if(sum_pages_compo_addcare == 1)
                            {
                                compoCode = "0-5";
                                PagesSize = 1;
                            }
                            else if(sum_pages_compo_addcare == 2)
                            {
                                compoCode = "1";
                                PagesSize = 2;
                            }
                            else if(sum_pages_compo_addcare == 3)
                            {
                                compoCode = "1-5";
                                PagesSize = 3;
                            }
                            else if(sum_pages_compo_addcare == 4)
                            {
                                compoCode = "2";
                                PagesSize = 4;
                            }
                            else if(sum_pages_compo_addcare == 5)
                            {
                                compoCode = "2-5";
                                PagesSize = 5;
                            }
                            else if(sum_pages_compo_addcare == 6)
                            {
                                compoCode = "3";
                                PagesSize = 6;
                            }
                            else if(sum_pages_compo_addcare == 7)
                            {
                                compoCode = "3-5";
                                PagesSize = 7;
                            }
                            else if(sum_pages_compo_addcare == 8)
                            {
                                compoCode = "4";
                                PagesSize = 8;
                            }
                            else if(sum_pages_compo_addcare == 9)
                            {
                                compoCode = "4-5";
                                PagesSize = 9;
                            }
                            else if(sum_pages_compo_addcare == 10)
                            {
                                compoCode = "5";
                                PagesSize = 10;
                            }
                            else if(sum_pages_compo_addcare == 11)
                            {
                                compoCode = "5-5";
                                PagesSize = 11;
                            }
                            else if(sum_pages_compo_addcare == 12)
                            {
                                compoCode = "6";
                                PagesSize = 12;
                            }

                            ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode + "_" + compoCode, PageQuantity = PagesSize });

                        }


                        break;

                    case "24MDC-PP-CARE":
                    case "24MDH-0201-CW":
                        ArticleCategoryLst.Add(new ArticleSizeCategory { ArticleCode = ArticleCode, PageQuantity = 1 });
                        break;

                }

                if(ArticleCategoryLst.Count < 1)
                {
                    string roles = string.Join(
                           Notification.ROLE_SEPARATOR,
                           new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

                    var title = $"It was not possible to determine the article  [{ArticleCode}_{compoCode}] for the exposed composition";
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
                        , source: "MassimoDuttiCompoPlugin"
                        , title: title
                        , message: message
                        , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = ArticleCode + "_" + compoCode }
                        , autoDismiss: false
                        , locationID: null
                        , projectID: od.ProjectID
                        , actionController: null);

                    throw new Exception($"It was not possible to determine the article  [{ArticleCode}_{compoCode}]");
                }


                var lst = ArticleCategoryLst.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault();

                newarticle = articleRepo.GetByCodeInProject(lst.ArticleCode, od.ProjectID);

                if(newarticle == null)
                {
                    string roles = string.Join(
                    Notification.ROLE_SEPARATOR,
                    new List<string> { Roles.IDTCostumerService, Roles.SysAdmin });

                    var title = $"MassimoDutti Composition Article [{ArticleCode}_{compoCode}] not found";
                    var message = $"Error when trying to get the article code, check if the article exists";
                    var nkey = message.GetHashCode().ToString();

                    //if article not found throw Exception
                    notificationRepo.AddNotification(
                        companyid: job.CompanyID
                        , type: NotificationType.OrderTracking
                        , intendedRoles: roles
                        , intendedUser: null
                        , nkey: nkey + job.CompanyOrderID
                        , source: "MassimoDuttiCompoPlugin"
                        , title: title
                        , message: message
                        , data: new { Error = $"There is an error with the number of sheets generated for the label", ArticleCode = ArticleCode + "_" + compoCode }
                        , autoDismiss: false
                        , locationID: null
                        , projectID: od.ProjectID
                        , actionController: null);

                    throw new Exception($"MassimoDutti Composition Article [{ArticleCode}_{compoCode}] not found");
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

            using(var ctx = factory.GetInstance<PrintDB>())
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

                using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
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
            LogMessage($"Save Generic Compo for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {compo.ID} )");
            //Symbols.Remove(Symbols.Length - 1, 1);


            compositionData.Add("ArticleCodeSelected", ArticleCategoryLst.Last().ArticleCode);
            var cleanedCi = Regex.Replace(careInstructions.ToString().Trim(), ciSeparator + "$", string.Empty);// PERSONALIZED TRIM END
            orderUtilService.SaveComposition(orderData[0].ProjectID, compo.ID, compositionData, cleanedCi, Symbols.ToString());
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

            //LogMessage($"Buscando articulo default se encontraron : [{found.Count()}], Se han agregado Articulos ?: [{containsArticles}]");

            if(containsArticles)
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
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != "Additional" && ci.Category != "Exception")
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
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if(ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // split by page additionals and exceptions
            List<List<string>> filledPages;
            var strategy = 1;
            if(!FillCareInstructionsPagesOneByOne(addExc, materialsFaces, linenumber, out filledPages))
            {
                if(!FillCareInstructionsPagesByTranslations(addExcTable, materialsFaces, linenumber, out filledPages))
                {
                    throw new Exception("MassimoDuttiCompoPlugin ERROR - Can't set Additional text");
                }

                strategy = 2;
            }


            // set where page text will be saved
            for(int j = 0; j < filledPages.Count; j++)
            {
                string f = string.Empty;

                if(strategy == 1)
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

            for(int pos = 0; pos < joinedTranslations.Count; pos++)
            {
                var text = joinedTranslations[pos];
                var pageText = string.Join(CI_SEPARATOR, currentPage.Append(text));

                FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                if(obj.Fit == false)
                {
                    // check if make fit inner blank page the current text
                    if(currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
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

            foreach(var ciAllLangs in ciText.Values)
            {

                foreach(var text in ciAllLangs)
                {
                    var testingText = currentPage.Count > 0 ? CI_LANG_SEPARATOR + text : text;
                    var pageText = string.Join("", currentPage.Append(testingText));

                    FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                    var isLastASeparator = currentPage.Count < 1 ? false : currentPage.Last() == CI_SEPARATOR || currentPage.Last() == CI_LANG_SEPARATOR;

                    if(obj.Fit == false)
                    {
                        // check if make fit inner blank page the current text
                        if(currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
                        {
                            if(isLastASeparator)
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

                    if(currentPage.Count > 0 && isLastASeparator == false)
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
            if(sectionValue.IndexOf('/') < 0)
            {
                return new List<string>() { sectionValue };
            }

            List<string> sections = new List<string>();
            string[] outerArray = sectionValue.Split('-');

            foreach(string part in outerArray)
            {
                var wordsOfSection = part.Split('/');
                int countOfSections = 0;
                foreach(string word in wordsOfSection)
                {
                    if(sections.Count < countOfSections + 1)
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
            for(var f = 0; f < section.Fibers.Count; f++)
            {
                var langsListFiber = section.Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                var fiberValue = (langsListFiber.Count() > 1 ? String.Join(FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First());
                list.Add(fiberValue);
            }
            return list;
        }

        private string GetSectionNameByLanguage(Section section, string lang, List<string> languages)
        {
            var text = string.Empty; 
            if (string.IsNullOrEmpty(lang) || section == null || !languages.Any())
            {
                return text;
            }

            var index = languages.Select(l => l.ToUpper()).ToList().IndexOf(lang.ToUpper()); 
            text = section.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct().ElementAtOrDefault(index);
            return text; 
        }
       
        private List<string> GetSectionFiberByLang(Section section, string lang, List<string>languages)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(lang) || section == null || !languages.Any())
            {
                return list;
            }

            var index = languages.Select(l=>l.ToUpper()).ToList().IndexOf(lang.ToUpper());     

            for(var f = 0; f < section.Fibers.Count; f++)
            {
                var langsListFiber = section.Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                list.Add(langsListFiber.ElementAtOrDefault(index));
            }

            return list; 
        }

        public List<CompositionTextDTO> CreateFiberList(WebLink.Contracts.Models.CompositionDefinition compo,
                                                        int fillingWeightId, 
                                                        string fillingWeightText, 
                                                        bool isSeparatedPercent,
                                                        List<string> fibersLanguage, 
                                                        string language)
        {
            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            for(var i = 0; i < compo.Sections.Count; i++)
            {
                var langsListSection = compo.Sections[i].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                var sectionValue = langsListSection.Count() > 1 ? String.Join(SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();

                //if composition have one section
                if(compo.Sections.Count > 1)
                {
                    var sectionText = ReArrangeSection(sectionValue);
                    if(sectionText.Count == 1)
                    {
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = sectionText.First(),
                            FiberType = "TITLE",
                            TextType = TextType.Title,
                            Langs = langsListSection.ToList(),
                            SectionFibersText = GetSectionFibers(compo.Sections[i]),
                            TextSelectedLanguage = GetSectionNameByLanguage(compo.Sections[i], language, fibersLanguage)
                        });
                    }
                    else
                    {
                        bool first = true;
                        foreach(var section in sectionText)
                        {
                            if(first)
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "TITLE",
                                    TextType = TextType.Title,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i]),
                                    TextSelectedLanguage = GetSectionNameByLanguage(compo.Sections[i], language, fibersLanguage)
                                });
                                first = false;
                            }
                            else
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "MERGETITLE",
                                    TextType = TextType.MergeTitle,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i]),
                                    TextSelectedLanguage = GetSectionNameByLanguage(compo.Sections[i], language, fibersLanguage)
                                });
                            }

                        }
                    }
                }

                for(var f = 0; f < compo.Sections[i].Fibers.Count; f++)
                {
                    var langsListFiber = compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                    var langsAllListFiber = compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                    var fiberValue = (langsListFiber.Count() > 1 ? String.Join(FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First());

                    list.Add(new CompositionTextDTO
                    {
                        Percent = isSeparatedPercent ? compo.Sections[i].Fibers[f].Percentage + "%" : string.Empty,
                        Text = isSeparatedPercent ? fiberValue : $"{compo.Sections[i].Fibers[f].Percentage + "%"} {fiberValue}",
                        FiberType = compo.Sections[i].Fibers[f].FiberType,
                        TextType = TextType.Fiber,
                        Langs = langsListFiber.ToList(), 
                        TextSelectedLanguage = $"{compo.Sections[i].Fibers[f].Percentage + "%"} {langsAllListFiber.Select(l=>l.ToUpper()).ToList().ElementAtOrDefault(fibersLanguage.IndexOf(language)) }"

                    });
                }

                // Add Exceptions only to the first section
                if(i == 0)
                {
                    compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.EXCEPTION).ForEach(ci =>
                    {

                        var langsList = ci.AllLangs
                        .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct();

                        var translated = langsList.Count() > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList.First();
                        if(fillingWeightId != 1 && fillingWeightId == ci.Instruction)
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

        public List<CompositionTextDTO> CreateCareInstructionsList(WebLink.Contracts.Models.CompositionDefinition compo)
        {

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

            for(var i = 0; i < lst.Count; i++)
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

            for(int j = 0; j < totalfibers; j++)
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

            for(int j = 0; j < totalpages; j++)
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
            stringSize = g.MeasureString(text, materials.Font == null ? new Font("Arial Unicode MS", 6, FontStyle.Regular) : materials.Font, addSize, sfFmt, out charactersFitted, out linesFilled);

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


        #region Logging

        public void LogMessage(string msg, params object[] args)
        {
#if DEBUG
            log.LogMessage(msg, args);
#endif
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {

            try
            {
                
                SaveCompoPreview(od,
                                data.compoArray,
                                data.percentArray,
                                data.leatherArray,
                                data.additionalArray,
                                data.labelLines,
                                data.ID,
                                data.AdditionalsCompress,
                                data.FiberCompress,
                                data.FibersInSpecificLang);


            }
            catch(Exception ex)
            {
                log.LogException($"Error saving composition preview. Compo array Length {data.compoArray.Length}", ex);
                throw new Exception ("Error saving composition preview", ex);
            }
        }

        #endregion
    }







}
