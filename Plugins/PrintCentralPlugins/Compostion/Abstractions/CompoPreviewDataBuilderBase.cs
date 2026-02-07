using LinqKit;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using SmartdotsPlugins.Compostion.Implementations;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class CompoPreviewDataBuilderBase
    {

        private Dictionary<CompoCatalogName, IEnumerable<string>> InditexLanguage;
        private IOrderUtilService orderUtilService;
        private ArticleCodeExtractor articleCodeExtractor;
        private ArticleCompositionConfigurationBase articleCompositionConfiguration;
        private InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager;
        private IPrinterJobRepository printerJobRepo;
        private IArticleRepository articleRepo;
        private FiberListBuilderBase fiberListBuilder;
        private SeparatorsInitBase SeparatorsInitializator;
        private CareInstructionsBuilderBase careInstructionsBuilder; 
        public List<PluginCompoPreviewData> PluginCompoPreviewData { get; set; }
        protected CompoPreviewDataBuilderBase(IOrderUtilService orderUtilService,
                    IPrinterJobRepository printerJobRepo,
                    IArticleRepository articleRepo)
        {
            this.orderUtilService = orderUtilService;
            this.printerJobRepo = printerJobRepo;
            this.articleRepo = articleRepo;
        }

        public virtual void SetCustomServices(ArticleCodeExtractor articleCodeExtractor,
                                              ArticleCompositionConfigurationBase articleCompositionConfiguration,
                                              InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager,
                                              FiberListBuilderBase fiberListBuilder,
                                              SeparatorsInitBase separatorsInitializator, 
                                              CareInstructionsBuilderBase  careInstructionsBuilder )


        {
            this.articleCodeExtractor = articleCodeExtractor;
            this.articleCompositionConfiguration = articleCompositionConfiguration;
            this.inditexLanguageDictionaryManager = inditexLanguageDictionaryManager;
            this.fiberListBuilder = fiberListBuilder;
            SeparatorsInitializator = separatorsInitializator;
            this.careInstructionsBuilder = careInstructionsBuilder; 

        }

        public virtual List<PluginCompoPreviewData> Generate(List<OrderPluginData> orderData, int id, bool isLoad)
        {

            InditexLanguage = inditexLanguageDictionaryManager.GetInditexLanguageDictionary();

            if(isLoad)
            {
                return LoadCompoFromDataBase(orderData, id);
            }
            return GenerateCompoFromText(orderData, id);
        }

        #region GenerateFromText

        private Separators Separators { get; set; }

        private List<PluginCompoPreviewData> GenerateCompoFromText(List<OrderPluginData> orderData, int id)
        {
            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            int indexCompo = 0;
            float widthInInches = 0;
            float heightInInches = 0;
            float widthAdditionalInInches = 0;
            int totalCompositions = 0;

            List<PluginCompositionTextPreviewData> CompositionText = new List<PluginCompositionTextPreviewData>();
            List<PluginCompositionTextPreviewData> CareInstructionsText = new List<PluginCompositionTextPreviewData>();

            Separators = SeparatorsInitializator.Init(projectData);

            foreach(var od in orderData)
            {
                var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, InditexLanguage, OrderUtilService.LANG_SEPARATOR).Where(c => c.ID == id || id == 0);
                totalCompositions = compositions.Count();
                //    IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);
                //    var orderDataArticleCode = articleRepo.GetByID(orderData.FirstOrDefault().ArticleID).ArticleCode;
                var orderDataArticleCode = articleCodeExtractor.Extract(od.OrderID, od.ArticleID);
                foreach(var compo in compositions.Where(a => a.ArticleCode.StartsWith(orderDataArticleCode)))
                {
                    if (compo == null)
                        throw new ArgumentNullException(nameof(compo), "Composition cannot be null");

                    if (Separators == null)
                        throw new ArgumentNullException(nameof(Separators), "Separators cannot be null");



                    if (InditexLanguage == null || !InditexLanguage.ContainsKey(CompoCatalogName.FIBERS))
                        throw new ArgumentException("Inditex language dictionary is not properly initialized");

                    List<CompositionTextDTO> listfibers = new List<CompositionTextDTO>();
                    List<CompositionTextDTO> listcareinstructions = new List<CompositionTextDTO>();

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

                    //foreach(var job in printer_job)
                    //{
                    //    currentArticle = articleRepo.GetByID(job.ArticleID);
                    //    artCode = orderDataArticleCode.Contains("_") ? orderDataArticleCode.Substring(0, orderDataArticleCode.LastIndexOf("_")) : orderDataArticleCode;
                    //}
                    artCode = orderDataArticleCode.Contains("_") ? orderDataArticleCode.Substring(0, orderDataArticleCode.LastIndexOf("_")) : orderDataArticleCode;
                    var articleConfig = articleCompositionConfiguration.Retrieve(artCode, od.ProjectID);
                    if(articleConfig == null)
                        throw new ArgumentNullException(nameof(articleConfig), "Article configuration cannot be null");
                    var fiberlistConfig = new FiberListBuilderBase.FiberListConfig()
                    {
                        Compo = compo,
                        Separators = Separators,
                        FillingWeightId = od.FillingWeightId,
                        FillingWeightText = od.FillingWeightText ?? string.Empty,
                        IsSeparatedPercentage = articleConfig.WithSeparatedPercentage,
                        FibersLanguage = InditexLanguage[CompoCatalogName.FIBERS]?.ToList() ?? new List<string>(),
                        Language = articleConfig.SelectedLanguage ?? string.Empty,
                        ExceptionsLocation = od.ExceptionsLocation,
                        OrderId = od.OrderID,
                        ExceptionsComposition = od.ExceptionsComposition ?? new List<ExceptionComposition>(),
                        UsesFreeExceptionComposition = od.UsesFreeExceptionComposition,
                        FiberConcatenation = od.FiberConcatenation // Puede ser null si no se requiere concatenación

                    };
                    listfibers = fiberListBuilder.Build(fiberlistConfig);
                    listcareinstructions = careInstructionsBuilder.Build(compo,Separators);

                    CompositionText = GetFiberCompositionText(listfibers, articleConfig);
                    CareInstructionsText = GetCareInstructionCompositionText(listcareinstructions);

                    if(PluginCompoPreviewData == null)
                    {
                        PluginCompoPreviewData = new List<PluginCompoPreviewData>();
                    }

                    compositionData = orderUtilService.GetCompositionData(orderData.FirstOrDefault().ProjectID, compo.ID);

                    PluginCompoPreviewData.Add(
                        new PluginCompoPreviewData()
                        {
                            Lines = articleConfig.LineNumber,
                            Width = articleConfig.WidthInches,
                            Heigth = articleConfig.HeightInInches,
                            CompoData = compositionData,
                            AdditionalsCompress = GetAdditionalsCompress(compositionData, false, articleConfig.DefaultCompresion),
                            FiberCompress = GetFiberCompress(compositionData, false, articleConfig.DefaultCompresion),
                            CompositionText = CompositionText,
                            CareInstructionsText = CareInstructionsText,
                            WidthAdditional = articleConfig.WidthAdditionalInInches,
                        });
                }
            }

            return PluginCompoPreviewData;
        }

        private static List<PluginCompositionTextPreviewData> GetCareInstructionCompositionText(List<CompositionTextDTO> listcareinstructions)
        {
            List<PluginCompositionTextPreviewData> CareInstructionsText = new List<PluginCompositionTextPreviewData>();
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

            return CareInstructionsText;
        }

        private static List<PluginCompositionTextPreviewData> GetFiberCompositionText(List<CompositionTextDTO> listfibers, ArticleCompositionConfig articleConfig)
        {
            List<PluginCompositionTextPreviewData> CompositionText = new List<PluginCompositionTextPreviewData>();
            foreach(var fiber in listfibers)
            {
                CompositionText.Add(new PluginCompositionTextPreviewData()
                {
                    FiberType = string.IsNullOrEmpty(fiber.Percent) && string.IsNullOrEmpty(fiber.FiberType) ? "TITLE" : fiber.FiberType,
                    Langs = fiber.Langs,
                    Percent = articleConfig.WithSeparatedPercentage ? fiber.Percent : string.Empty,
                    Text = fiber.Text,
                    IsTitle = fiber.TextType == TextType.Title,
                    SectionFibersText = fiber.SectionFibersText,
                });
            }

            return CompositionText;
        }



        private void InitializceSeparator(IProject projectData)
        {
        }
        #endregion
        #region LoadFromDataBase

        private List<PluginCompoPreviewData> LoadCompoFromDataBase(List<OrderPluginData> orderData, int id)
        {
            var od = orderData.FirstOrDefault();
            var composition = orderUtilService
                                .GetComposition(od.OrderGroupID, true, InditexLanguage, OrderUtilService.LANG_SEPARATOR)
                                .OrderBy(c => c.ID).FirstOrDefault(c => c.ID == id);

            if(composition == null)
            {
                return null;
            }
            var compositionData = new Dictionary<string, string>();
            compositionData = orderUtilService.GetCompositionData(orderData.FirstOrDefault().ProjectID, composition.ID);
            if(compositionData == null)
            {
                return null;
            }

            var article = articleCodeExtractor.Extract(od.OrderID);
            var articleConfig = articleCompositionConfiguration.Retrieve(article, od.ProjectID);

            return new List<PluginCompoPreviewData>() { new PluginCompoPreviewData()
                    {
                        Lines = articleConfig.LineNumber,
                        Width = articleConfig.WidthInches,
                        Heigth= articleConfig.HeightInInches,
                        CompoData = compositionData,
                        AdditionalsCompress = GetAdditionalsCompress(compositionData, true),
                        FiberCompress = GetFiberCompress(compositionData, true),
                        CompositionText = GetCompostionText( compositionData, composition, articleConfig.WithSeparatedPercentage),
                        ExceptionsLocation = GetExceptionLocation(compositionData)
                       
            }};
        }

        private int GetExceptionLocation(Dictionary<string, string> compositionData)
        {
            int exceptionsLocation = 0;
            if(compositionData.TryGetValue("ExceptionsLocation", out string value))
            {
                int.TryParse(value, out exceptionsLocation);
            }
            return exceptionsLocation;
        }
        private int GetAdditionalsCompress(Dictionary<string, string> compositionData, bool isLoad, int compress =-1)
        {
            string value = string.Empty;
            if(isLoad)
            {
                if(compositionData.TryGetValue("AdditionalCompress", out value))
                {
                    int.TryParse(value, out compress);
                    return compress;
                }
                return 0;
            }
            else
            {
                if(compositionData.TryGetValue("AdditionalCompress", out value))
                {
                    if(!string.IsNullOrEmpty(value))
                    {
                        return -1; // Ya se ha generado una vez la compresion por tanto no se aplica valor por defecto
                    }
                }
                return compress == -1 ? 0 : compress; // Se aplica valor por defecto si es que existe

            }

        }

        private int GetFiberCompress(Dictionary<string, string> compositionData, bool isLoad, int compress = -1 )
        {
            string value = string.Empty;
            if (isLoad)
            {
                if(compositionData.TryGetValue("FiberCompress", out  value))
                {
                    int.TryParse(value, out compress);
                    return compress; 
                }
                return 0; 
            }else
            {
                if(compositionData.TryGetValue("FiberCompress", out value))
                {
                    if (!string.IsNullOrEmpty(value)) {
                        return -1; // Ya se ha generado una vez la compresion por tanto no se aplica valor por defecto
                    }
                    
                }

                return compress == -1 ? 0 : compress; // Se aplica valor por defecto si es que existe    
            }


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


        #endregion



    }
}
