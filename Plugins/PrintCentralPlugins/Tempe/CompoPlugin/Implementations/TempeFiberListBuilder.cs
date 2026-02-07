using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Tempe.CompoPlugin.Implementations
{
    public class TempeFiberListBuilder : FiberListBuilderBase
    {
        private IDBConnectionManager connManager;
        private IFactory factory;



        public TempeFiberListBuilder(IDBConnectionManager connManager,
                                     IFactory factory)
        {
            this.connManager = connManager;
            this.factory = factory;
        }

        public override List<CompositionTextDTO> Build(FiberListConfig config)
        {
            List<CompositionTextDTO> fiberList = new List<CompositionTextDTO>();

            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var companyOrder = ctx.CompanyOrders.FirstOrDefault(o => o.ID == config.OrderId);

                var FullTranslatedCompositionCatalog = ctx.Catalogs
                                                       .FirstOrDefault(c => c.Name.Equals("FullTranslatedComposition")
                                                                        && c.ProjectID.Equals(companyOrder.ProjectID));

                var baseDataCatalog = ctx.Catalogs
                                    .FirstOrDefault(c => c.Name.Equals("BaseData")
                                    && c.ProjectID.Equals(companyOrder.ProjectID));

                var orderNumber = ctx.CompanyOrders.FirstOrDefault(o => o.OrderGroupID == companyOrder.OrderGroupID)?.OrderNumber;

                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {


                    JArray txt_exceptos = GetExceptos(orderNumber, baseDataCatalog, dynDb);

                    JArray translatedComposition = GetTranslatedComposition(companyOrder, FullTranslatedCompositionCatalog, dynDb);

                    List<FullTranslatedComposition> lines = translatedComposition.ToObject<List<FullTranslatedComposition>>();

                    if(!lines.Any())
                    {
                        throw new Exception($"No composition found for ordernumbrer {companyOrder.OrderNumber} in table FullTranslatedComposition ");
                    }
                    var lstExceptos = txt_exceptos.ToObject<List<BaseData>>();
                    string exceptos = lstExceptos.FirstOrDefault()?.TXT_EXCEPTOS;
                    int mainSectionsCount = 0;
                    var linesSorted = lines.OrderBy(l => l.IdLine).ToList();
                    for(var i = 0; i < linesSorted.Count(); i++)
                    {
                        var langsListSection = linesSorted[i].Text.Split('/', StringSplitOptions.RemoveEmptyEntries).Distinct();
                        var sectionValue = langsListSection.Count() > 1 ? string.Join(config.Separators.SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();

                        if(linesSorted[i].Type == "Section")
                        {
                            mainSectionsCount = AddSection(config, fiberList, linesSorted.ToList(), exceptos, mainSectionsCount, i, langsListSection);
                        }

                        if(linesSorted[i].Type == "Fiber")
                        {
                            AddFiber(config, fiberList, linesSorted.ToList(), i, langsListSection);
                        }
                    }

                }
            }

            return fiberList;
        }

        private JArray GetOrderNumberOld(int orderGroupID, Catalog fullTranslatedCompositionCatalog, DynamicDB dynDb)
        {
            return dynDb.Select(fullTranslatedCompositionCatalog.CatalogID,
                            $@"SELECT TOP (1) OrderNumber FROM #TABLE o WHERE o.OrderGroupID=@OrderGroupID",
                            orderGroupID);
        }

        private static JArray GetTranslatedComposition(Order companyOrder, Catalog FullTranslatedCompositionCatalog, DynamicDB dynDb)
        {
            return dynDb.Select(FullTranslatedCompositionCatalog.CatalogID,
                            $@"SELECT * FROM #TABLE o WHERE o.OrderID=@OrderID",companyOrder.ID);
        }

        private static JArray GetExceptos(string orderNumber, Catalog baseDataCatalog, DynamicDB dynDb)
        {
            return dynDb.Select(baseDataCatalog.CatalogID,
                            $@"SELECT txt_exceptos FROM #TABLE o WHERE o.OrderNumber=@OrderNumber",
                            orderNumber);
        }

        private int AddSection(FiberListConfig config, List<CompositionTextDTO> fiberList, List<FullTranslatedComposition> lines, string exceptos, int mainSectionsCount, int i, IEnumerable<string> langsListSection)
        {
            var section = BuildSection(lines[i], lines);
            if(string.IsNullOrEmpty(lines[i].IdParent))
            {
                if(mainSectionsCount == 1)
                {
                    AddExceptions(exceptos, fiberList);
                }
                mainSectionsCount++;
            }
            fiberList.Add(new CompositionTextDTO
            {
                Percent = string.Empty,
                Text = string.Join('-', langsListSection).TrimEnd(),
                FiberType = "Title",
                TextType = TextType.Title,
                Langs = langsListSection.ToList(),
                SectionFibersText = base.GetSectionFibers(section, config.Separators),
                TextSelectedLanguage = base.GetSectionNameByLanguage(section, config.Language, config.FibersLanguage)
            });
            return mainSectionsCount;
        }

        private static void AddFiber(FiberListConfig config, List<CompositionTextDTO> fiberList, List<FullTranslatedComposition> lines, int i, IEnumerable<string> langsListSection)
        {
            var langsListFiber = lines[i].Text.Replace("/", "__S__").Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
            var langsAllListFiber = lines[i].Text.Replace("/", "__S__").Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            var fiberValue = langsListFiber.Count() > 1 ? string.Join(config.Separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();

            fiberList.Add(new CompositionTextDTO
            {
                Percent = config.IsSeparatedPercentage ? lines[i].Percentage + "%" : string.Empty,
                Text = config.IsSeparatedPercentage ? fiberValue : $"{lines[i].Percentage + "%"} {fiberValue}",
                FiberType = lines[i].IsLeather ? "1" : "2",
                TextType = TextType.Fiber,
                Langs = langsListSection.ToList(),
                TextSelectedLanguage = $"{lines[i].Percentage + "%"} {langsAllListFiber.Select(l => l.ToUpper()).ToList().ElementAtOrDefault(config.FibersLanguage.IndexOf(config.Language))}"
            });
        }

        private void AddExceptions(string exceptos, List<CompositionTextDTO> list)
        {

            if(string.IsNullOrEmpty(exceptos))
            {
                return;
            }

            var langsListSection = exceptos.Split('/', StringSplitOptions.RemoveEmptyEntries).Distinct();
            list.Add(new CompositionTextDTO
            {
                Percent = string.Empty,
                Text = exceptos,
                FiberType = string.Empty,
                TextType = TextType.CareInstruction,
                Langs = langsListSection.ToList()
            });
        }

        private Section BuildSection(FullTranslatedComposition fullTranslatedComposition, List<FullTranslatedComposition> lines)
        {
            Section section = new Section();
            section.AllLangs = fullTranslatedComposition.Text.Replace(" / ", "__S__");
            var listOfSons = lines.Where(l => l.IdParent == fullTranslatedComposition.IdLine && !string.IsNullOrEmpty(l.IdParent));
            if(listOfSons.Any())
            {
                section.Fibers = new List<Fiber>();
                foreach(var l in listOfSons)
                {
                    var text = l.Text.Replace(" / ", "__S__");
                    section.Fibers.Add(new Fiber()
                    {
                        AllLangs = text

                    });
                }

            }
            return section;
        }

        public class BaseData
        {
            public string TXT_EXCEPTOS { get; set; }
        }

        private class FullTranslatedComposition
        {
            public int ID { get; set; }
            public string OrderNumber { get; set; }
            public string Percentage { get; set; }
            public string Text { get; set; }
            public string Type { get; set; }
            public string IdParent { get; set; }
            public string Order { get; set; }
            public bool IsLeather { get; set; }
            public string IsMicroContent { get; set; }

            public string IdCompositionType { get; set; }
            public string IdLine { get; set; }

        }
    }
}
