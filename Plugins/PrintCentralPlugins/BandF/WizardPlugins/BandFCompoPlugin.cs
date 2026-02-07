using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.BandF.WizardPlugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.OrderPlugins
{
    // scalpercompoplugin cloned - remove titles
    /**
     * B&F only have 2 sections -> layer1 and layer2
     * require 2 fields with full fibers concatented text
     * 
     * */
    [FriendlyName("B&F - Composition Text Plugin")]
    [Description("B&F Composition WizardPlugin")]
    public class BandFCompoPlugin : IWizardCompositionPlugin
    {
        private IEventQueue events;
        private ILogSection log;
        private IOrderUtilService orderUtilService;
        private readonly int MAX_SHOES_FIBERS = 5;
        private readonly int MAX_SHOES_SECTIONS = 3;
        private readonly string EMPTY_CODE = "0";
        private readonly int MAX_CHARACTERS_COMPO = 1124;
        private readonly int MAX_CHARACTERS_CARE_INSTRUCTIONS = 1021;
        private readonly IConnectionManager connManager;
        private readonly ICatalogRepository catalogRepo;



        public BandFCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService, ICatalogRepository catalogRepo, IConnectionManager connManager)
        {
            this.events = events;
            this.log = log.GetSection("B&F - CompoPlugin");
            this.orderUtilService = orderUtilService;
            this.catalogRepo = catalogRepo;
            this.connManager = connManager;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            var sectionSeparator = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            var sectionLanguageSeparator = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            var fibersSeparator = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            var fiberLanguageSeparator = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            var ciSeparator = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            var ciLanguageSeparator = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;
            var englishLangPosition = 0;



            foreach(var od in orderData)
            {
                var composition = orderUtilService.GetComposition(od.OrderGroupID);

                foreach(var c in composition)
                {
                    var compositionData = new Dictionary<string, string>();
                    compositionData["FullComposition"] = string.Empty;
                    var fibersEnglishValue = new List<string>();

                    // exist a fond called "SIMBOLOS CALZADO.otf" created by IDT designers
                    // 1->A, 2->B, 3->C
                    //StringBuilder ShoesSymbols = new StringBuilder(string.Empty, 15);
                    List<string> ShoesSymbols;
                    var validSymbolsValue = new Dictionary<char, string> { { '1', "A" }, { '2', "B" }, { '3', "C" } };

                    for(var i = 0; i < c.Sections.Count; i++)
                    {
                        //var title = "title_" + (i + 1);
                        var symbolShoe = "symbols_shoes_" + (i + 1);
                        //var titleValue = string.Empty;
                        ShoesSymbols = new List<string>() { "", "", "", "", "" };

                        // AllLangs column was added manually in query using ',' as separator
                        //var langsList = c.Sections[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        //if (c.Sections[i].Code != EMPTY_CODE)
                        //    titleValue = langsList.Length > 1 ? c.Sections[i].AllLangs.Replace(",", $" { sectionLanguageSeparator } ") : langsList[0];

                        //compositionData.Add(title, titleValue);

                        //section fibers
                        var fibers = c.Sections[i].Fibers != null ? c.Sections[i].Fibers : new List<Fiber>();
                        var fiber = "layer_" + (i + 1);
                        var fiberValue = string.Empty;


                        for(var f = 0; f < fibers.Count; f++)
                        {

                            if(!string.IsNullOrEmpty(fibers[f].FiberType) && validSymbolsValue.TryGetValue(fibers[f].FiberType[0], out var keyLetter))
                            {
                                if(f < MAX_SHOES_FIBERS)
                                    ShoesSymbols[f] = keyLetter;
                            }

                            var langsList = fibers[f].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            if(fibers[f].Code != EMPTY_CODE)
                            {
                                fiberValue += $"{fibers[f].Percentage}% " + (langsList.Length > 1 ? $" {string.Join(fiberLanguageSeparator, langsList)} " : langsList[0]);
                                fiberValue += fibersSeparator;
                                // especial case for B&F
                                fibersEnglishValue.Add($"{fibers[f].Percentage}% " + langsList[englishLangPosition]);
                            }
                        }

                        compositionData.Add(fiber, fiberValue);



                        // only 3 first sections, fibers contains shoes symbols
                        if(i < MAX_SHOES_SECTIONS)
                            compositionData.Add(symbolShoe, string.Join(";", ShoesSymbols));

                    }


                    //add care instructions
                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder(string.Empty, 10);

                    foreach(var ci in c.CareInstructions)
                    {
                        var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        Symbols.Append(ci.Symbol);// TODO: now, always use FONT 

                        //var translations = langsList.Length > 1 ? ci.AllLangs.Replace(",", $" { ciLanguageSeparator } ") : langsList[0];
                        var translations = langsList.Length > 1 ? string.Join(ciLanguageSeparator, langsList) : langsList[0];
                        careInstructions.Append(translations);
                        careInstructions.Append(ciSeparator);
                    }

                    compositionData["FullComposition"] = string.Join(',', fibersEnglishValue); // all fibers for all layers inner one field
                    var counterFacesCareInstructions = SplitCareInstructions(careInstructions.ToString(), compositionData);
                    var counterFacesCompositions = SplitCompositions(compositionData, od.ProjectID, c.ID);

                    compositionData.Add("COUNTER_FACES_CARE_INSTRUCTIONS", counterFacesCareInstructions.ToString());
                    compositionData.Add("COUNTER_FACES_COMPOSITIONS", counterFacesCompositions.ToString());

                    orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compositionData, careInstructions.ToString(), Symbols.ToString());

                    log.LogMessage($"Saved Composition for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {c.ID} )");
                }

            }
        }

        private int SplitCompositions(Dictionary<string, string> compositionData, int projectID, int compositionID)
        {
            var decorativeText = AddDecorativeValue(projectID, compositionID);

            string composition = string.Empty;
            int layerCount = 0;
            CounterLayers(compositionData, ref composition, ref layerCount);
            composition = GenerateCompoLayer2(compositionData, composition, layerCount);

            composition += "\n" + decorativeText;

            if(composition.Length > MAX_CHARACTERS_COMPO * 5)
                log.LogWarning($"Compositions exceeds the maximum allowed length of {MAX_CHARACTERS_COMPO * 3} characters. Therefore it has been truncated and characters have been lost.");

            int start = 0;
            var counterFaces = 0;
            string chunk = string.Empty;
            for(int i = 0; i < 5 && start < composition.Length; i++)
            {
                int remaining = composition.Length - start;

                if(remaining <= MAX_CHARACTERS_COMPO)
                {
                    chunk = composition.Substring(start).Trim();
                    compositionData[$"COMPO{i + 1}"] = chunk;
                    counterFaces++;
                    return counterFaces;
                }

                int maxLength = Math.Min(MAX_CHARACTERS_COMPO, composition.Length - start);
                int searchEnd = start + maxLength;

                int lastSlashIndex = composition.LastIndexOf(' ', searchEnd - 1, maxLength);

                int end;
                if(lastSlashIndex >= start)
                    end = lastSlashIndex;
                else
                    end = searchEnd;

                int length = end - start;
                chunk = composition.Substring(start, length).Trim();

                if(!string.IsNullOrEmpty(chunk))
                    counterFaces++;

                compositionData[$"COMPO{i + 1}"] = chunk;
                start = end + 1;
            }

            return counterFaces;
        }

        private static string GenerateCompoLayer2(Dictionary<string, string> compositionData, string composition, int layerCount)
        {
            if(layerCount == 2)
            {
                if(!string.IsNullOrWhiteSpace(compositionData["layer_2"]))
                {
                    if(compositionData["layer_2"].Contains("LINING"))
                        composition += "\n" + compositionData["layer_2"]?.ToString()?.Trim();
                    else
                        composition += "\n" + "LINING:" + "\n" + compositionData["layer_2"]?.ToString()?.Trim();
                }
            }

            return composition;
        }

        private static void CounterLayers(Dictionary<string, string> compositionData, ref string composition, ref int layerCount)
        {
            foreach(var kvp in compositionData)
            {
                if(kvp.Key.StartsWith("layer_"))
                    layerCount++;
                if(kvp.Key.Contains("layer_1"))
                    composition += kvp.Value;
            }
        }

        private string AddDecorativeValue(int projectID, int compositionID)
        {
            var decorativeValue = GetValueDecorative(projectID, compositionID);

            using(var dynamicDB = connManager.OpenDB("CatalogDB"))
            {
                string translations = string.Empty;

                if(decorativeValue != "NO")
                {
                    translations = "GREEK: ΤΑ ΔΙΑΚΟΣΜΗΤΙΚΑ ΣΤΟΙΧΕΙΑ ΔΕΝ ΠΕΡΙΛΑΜΒΑΝΟΝΤΑΙ ΣΤΗ ΣΥΝΘΕΣΗ.\n";
                    translations += "ENGLISH: DECORATIVE ELEMENTS ARE NOT INCLUDED IN THE COMPOSITION.\n";
                    translations += "TURKISH: DEKORATİF ÖĞELER BİLEŞİME DAHİL DEĞİLDİR.\n";
                    translations += "ROMANIAN: ELEMENTELE DECORATIVE NU SUNT INCLUSE ÎN COMPOZIȚIE.\n";
                    translations += "RUSSIAN: ДЕКОРАТИВНЫЕ ЭЛЕМЕНТЫ НЕ ВКЛЮЧЕНЫ В СОСТАВ.\n";
                    translations += "GERMAN: DEKORATIVE ELEMENTE SIND NICHT IN DER ZUSAMMENSETZUNG ENTHALTEN.\n";
                    translations += "SPANISH: LOS ELEMENTOS DECORATIVOS NO ESTÁN INCLUIDOS EN LA COMPOSICIÓN.\n";
                    translations += "FRENCH: LES ÉLÉMENTS DÉCORATIFS NE SONT PAS INCLUS DANS LA COMPOSITION.\n";
                    translations += "BULGARIAN: ДЕКОРАТИВНИТЕ ЕЛЕМЕНТИ НЕ СА ВКЛЮЧЕНИ В СЪСТАВА.\n";
                    translations += "ARABIC: العناصر الزخرفية غير مُدرَجة في التَّركيبة.\n";
                    translations += "ITALIAN: GLI ELEMENTI DECORATIVI NON SONO INCLUSI NELLA COMPOSIZIONE.\n";
                    translations += "PORTUGUESE: OS ELEMENTOS DECORATIVOS NÃO ESTÃO INCLUÍDOS NA COMPOSIÇÃO.";

                }
                return translations;
            }


        }

        private string GetValueDecorative(int projectID, int compositionID)
        {
            var catalogs = catalogRepo.GetByProjectID(projectID, true);

            var ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            var orderDetailsCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            var baseDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.BASEDATA_CATALOG));
            var relField = ordersCatalog.Fields.First(w => w.Name == "Details");

            using(var catalogDbConn = connManager.OpenDB("CatalogDB"))
            {
                var decorativeDTO = catalogDbConn.SelectOne<DecorativeDTO>(
                                        $@"SELECT bd.Decorative AS Decorative
                                            FROM {variableDataCatalog.TableName} v
                                            INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                                            WHERE v.HasComposition = {compositionID}");

                return decorativeDTO?.Decorative?.ToString()?.ToUpper();
            }
        }

        private int SplitCareInstructions(string careInstructions, Dictionary<string, string> compositionData)
        {

            if(careInstructions.Length > MAX_CHARACTERS_CARE_INSTRUCTIONS * 5)
                log.LogWarning($"Care Instructions exceeds the maximum allowed length of {MAX_CHARACTERS_CARE_INSTRUCTIONS * 4} characters. Therefore it has been truncated and characters have been lost.");

            int start = 0;
            var counterFaces = 0;
            for(int i = 0; i < 5 && start < careInstructions.Length; i++)
            {
                int maxLength = Math.Min(MAX_CHARACTERS_CARE_INSTRUCTIONS, careInstructions.Length - start);
                int searchEnd = start + maxLength;

                // Buscar el último '/' dentro del rango permitido
                int lastSlashIndex = careInstructions.LastIndexOf('/', searchEnd - 1, maxLength);

                int end;
                if(lastSlashIndex >= start)
                    end = lastSlashIndex;
                else
                    end = searchEnd; // Si no hay '/', cortamos hasta el final del rango permitido


                int length = end - start;
                string chunk = careInstructions.Substring(start, length);

                if(!string.IsNullOrEmpty(chunk))
                    counterFaces++;

                compositionData[$"CARE_INSTRUCTIONS{i + 1}"] = chunk;
                start = end;
            }

            return counterFaces;
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
