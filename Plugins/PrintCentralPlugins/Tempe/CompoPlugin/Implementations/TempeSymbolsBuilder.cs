using Newtonsoft.Json;
using Remotion.Linq.Clauses;
using Service.Contracts;
using Service.Contracts.Database;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.PuntoRoma.WizardPlugins;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Tempe.CompoPlugin.Implementations
{
    public  class TempeSymbolsBuilder: SymbolsBuilderBase 
    {
        private readonly IConnectionManager connManager;
        private IFactory factory;
        private readonly ICatalogRepository catalogRepository;
        private readonly IOrderRepository orderRepository;

        private readonly string BASEDATA_TABLENAME = "BaseData";

        public TempeSymbolsBuilder(IConnectionManager connManager, IFactory factory, ICatalogRepository catalogRepository, IOrderRepository orderRepository)
        {
            this.connManager = connManager;
            this.factory = factory;
            this.catalogRepository = catalogRepository;
            this.orderRepository = orderRepository;
        }

        public class Symbols
        {
            public string TXT_INSCONS_0 { get; set; }
            public string TXT_INSCONS_1 { get; set; }
            public string TXT_INSCONS_2 { get; set; }
            public string TXT_INSCONS_3 { get; set; }
            public string TXT_INSCONS_4 { get; set; }

            public string InstructionImage_1_1 { get; set; }
            public string InstructionImage_1_2 { get; set; }
            public string InstructionImage_1_3 { get; set; }
            public string InstructionImage_1_4 { get; set; }
            public string InstructionImage_1_5 { get; set; }

            public string InstructionImage_2_1 { get; set; }
            public string InstructionImage_2_2 { get; set; }
            public string InstructionImage_2_3 { get; set; }
            public string InstructionImage_2_4 { get; set; }
            public string InstructionImage_2_5 { get; set; }

            public string InstructionImage_3_1 { get; set; }
            public string InstructionImage_3_2 { get; set; }
            public string InstructionImage_3_3 { get; set; }
            public string InstructionImage_3_4 { get; set; }
            public string InstructionImage_3_5 { get; set; }


        }

        public override void Build(CompositionDefinition compo, StringBuilder Symbols, Dictionary<string, string> compositionData, Separators separators)
        {
            var data = new Dictionary<string, string>();
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {

                ICatalog ordersCatalog, detailCatalog, variableDataCatalog, compositionLabelCatalog, baseDataCatalog;

                var order = orderRepository.GetByID(ctx,compo.OrderID);
                GetCatalogs(order.ProjectID, out ordersCatalog, out detailCatalog, out variableDataCatalog, out compositionLabelCatalog, out baseDataCatalog);
                var relField = JsonConvert.DeserializeObject<List<FieldDefinition>>(ordersCatalog.Definition).First(w => w.Name == "Details");

                var symbols = GetSymbols(compo, ordersCatalog, detailCatalog, variableDataCatalog, compositionLabelCatalog, baseDataCatalog, relField);

                if(symbols == null)
                    throw new Exception("Washing symbols not found");

                Symbols.Append($"{symbols?.TXT_INSCONS_0},{symbols?.TXT_INSCONS_1},{symbols?.TXT_INSCONS_2},{symbols?.TXT_INSCONS_3},{symbols?.TXT_INSCONS_4}");

                compositionData.Remove("Symbols");
                compositionData.Add("Symbols", Symbols.ToString());
            }
        }

        private CareInstructionsSymbols GetSymbols(CompositionDefinition compo, ICatalog ordersCatalog, ICatalog detailCatalog, ICatalog variableDataCatalog, ICatalog compositionLabelCatalog, ICatalog baseDataCatalog, FieldDefinition relField)
        {
            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {

                return dynamicDb.SelectOne<CareInstructionsSymbols>(
                                        $@"SELECT bd.TXT_INSCONS_0,bd.TXT_INSCONS_1,bd.TXT_INSCONS_2,bd.TXT_INSCONS_3,bd.TXT_INSCONS_4 
                                            FROM {detailCatalog.TableName} d
                                            INNER JOIN {variableDataCatalog.TableName} v ON d.Product = v.ID
                                            INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                                            WHERE d.ID = {compo.ProductDataID}");

            }
        }

        private void GetCatalogs(int projectId, out ICatalog ordersCatalog, out ICatalog detailCatalog, out ICatalog variableDataCatalog, out ICatalog compositionLabelCatalog, out ICatalog baseDataCatalog)
        {
            var catalogs = catalogRepository.GetByProjectID(projectId, true);
            ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            baseDataCatalog = catalogs.First(f => f.Name.Equals(BASEDATA_TABLENAME));
        }

    }
}
