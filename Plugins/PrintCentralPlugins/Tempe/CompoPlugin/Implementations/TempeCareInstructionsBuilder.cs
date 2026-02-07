using Service.Contracts;
using Service.Contracts.Database;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Tempe.CompoPlugin.Implementations
{
    public class TempeCareInstructionsBuilder : CareInstructionsBuilderBase
    {

        private IDBConnectionManager connManager;
        private IFactory factory;
        public TempeCareInstructionsBuilder(IDBConnectionManager connManager, IFactory factory)
        {
            this.connManager = connManager;
            this.factory = factory;
        }
        public class BaseData
        {
            public string TXT_ADICIONALES { get; set; }
        }

        public override List<CompositionTextDTO> Build(CompositionDefinition compo, Separators separators)
        {

            List<CompositionTextDTO> list = new List<CompositionTextDTO>();
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var companyOrder = ctx.CompanyOrders.FirstOrDefault(o => o.ID == compo.OrderID);
                var baseDataCatalog = ctx.Catalogs
                    .FirstOrDefault(c => c.Name.Equals("BaseData")
                                    && c.ProjectID.Equals(companyOrder.ProjectID));

                var FullTranslatedCompositionCatalog = ctx.Catalogs
                                                       .FirstOrDefault(c => c.Name.Equals("FullTranslatedComposition")
                                                                        && c.ProjectID.Equals(companyOrder.ProjectID));

                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {
                    var orderNumber = ctx.CompanyOrders.FirstOrDefault(o=>o.OrderGroupID == compo.OrderGroupID)?.OrderNumber;   

                    var txt_adicional = dynDb.Select(baseDataCatalog.CatalogID,$@"SELECT TXT_ADICIONALES FROM #TABLE o WHERE o.OrderNumber=@OrderNumber", orderNumber);
                    var lstAdicionales = txt_adicional.ToObject<List<BaseData>>();
                    string adicionales = lstAdicionales.FirstOrDefault()?.TXT_ADICIONALES;
                    if(!string.IsNullOrEmpty(adicionales))
                    {
                        var langsList = adicionales.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                        var translated = langsList.Count() > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList) : langsList.First();
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = translated,
                            FiberType = "Additional",
                            TextType = TextType.CareInstruction,
                            Langs = langsList.ToList()
                        });

                    }

                }

            }
            return list;
        }
    }
}
