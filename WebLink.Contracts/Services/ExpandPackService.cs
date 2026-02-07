using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using WebLink.Contracts.Models;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Services
{
    public class ExpandPackService : IExpandPackService
    {
        private IFactory factory;
        private IFileStoreManager storeManager;
        private IArticleRepository articleRepo;
        private IPackRepository packRepo;
        private IPluginManager<IPackArticlesPlugin> pluginManager;
        private IOrderRepository orderRepo;
        private ICatalogDataRepository catalogDataRepo;
        private IVariableDataRepository variableDataRepo;
        private ILabelRepository labelRepo;
        private readonly IDataImportService dataImportService;
        private readonly IMappingRepository mappingRepo;

        public ExpandPackService(
            IFactory factory
            , IFileStoreManager storeManager
            , IArticleRepository articleRepo
            , IPackRepository packRepo
            , IPluginManager<IPackArticlesPlugin> pluginManager
            , IOrderRepository orderRepo
            , ICatalogDataRepository catalogDataRepo
            , IVariableDataRepository variableDataRepo
            , ILabelRepository labelRepo
            , IDataImportService dataImportService
            , IMappingRepository mappingRepo)
        {
            this.factory = factory;
            this.storeManager = storeManager;
            this.articleRepo = articleRepo;
            this.packRepo = packRepo;
            this.pluginManager = pluginManager;
            this.orderRepo = orderRepo;
            this.catalogDataRepo = catalogDataRepo;
            this.variableDataRepo = variableDataRepo;
            this.labelRepo = labelRepo;
            this.dataImportService = dataImportService;
            this.mappingRepo = mappingRepo;
        }

        public OrderInfo GenerateOrderInfo(int orderID, int projectID, string packCode)
        {

            var order = orderRepo.GetByID(orderID);

            var orderData = MapDictionaryToImportedData(orderID, projectID).GetAwaiter().GetResult();

            OrderInfo orderInfo = new OrderInfo()
            {
                OrderGroupID = order.OrderGroupID,
                OrderNumber = order.OrderNumber,
                SendTo = order.SendTo,
                BillTo = order.BillTo,
                SendToCompanyID = order.SendToCompanyID,
                ArticleCode = packCode,
                PackCode = null,
                Quantity = order.Quantity,
                Data = orderData,
            };
            return orderInfo;
        }

        // TODO: esto puede causar problemas cuando la compañia tiene varios mappings, esta utilizando siempre el primero que se encuentra
        private async Task<ImportedData> MapDictionaryToImportedData(int orderID, int projectID)
        {
            var userName = "ExpandPackService-" + DateTime.Now.ToString("HH:mm:ss.fff");

            var orderFile = orderRepo.GetOrderFile(orderID);

            var tempStore = storeManager.OpenStore("TempStore");
            var dstfile = await tempStore.CreateFileAsync(orderFile.FileName);
            await dstfile.SetContentAsync(orderFile.GetContentAsBytes());


            var config = mappingRepo.GetDocumentImportConfiguration(userName, projectID, dstfile);

            var job = await dataImportService.RegisterUserJob(userName, projectID, DocumentSource.Validation, true);

            await dataImportService.StartUserJob(userName, config);

            DocumentImportProgress process = new DocumentImportProgress();

            while(process.ReadProgress < 100)
            {
                process = dataImportService.GetJobProgress(userName);
                await Task.Delay(50);
            }

            var jobResult = await dataImportService.GetJobResult(userName);

            if (!jobResult.Success)
                throw new Exception($"Cannot Expand Pack, error to get Order Data for OrderID [{orderID}]");

            var importedData = await dataImportService.GetImportedDataAsync(userName);

            await dataImportService.PurgeJob(userName);

            return importedData;

        }

        // este metodo ya existe en el OrderExistVerifier
        private List<Dictionary<string, string>> GetBaseData(int orderId)
        {
            var orderData = new List<Dictionary<string, string>>();

            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var order = ctx.CompanyOrders.FirstOrDefault(c => c.ID == orderId);
                var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == order.ProjectID && c.Name.Equals("OrderDetails"));
                var detailOrder = ctx.PrinterJobs
                        .Join(ctx.PrinterJobDetails, j => j.ID, d => d.PrinterJobID, (job, detail) => new { PrinterJobs = job, PrinterJobDetails = detail })
                        .Where(w => w.PrinterJobs.CompanyOrderID == orderId)
                        .Select(s => s.PrinterJobDetails)
                        .ToList();
                if (catalog != null)
                {
                    List<TableData> newTables = catalogDataRepo
                        .ExportData(order.ProjectID, "Orders", true, new NameValuePair("ID", order.OrderDataID))
                        .Where(w => !w.Name.StartsWith("REL_")).ToList();
                    var newOrderData = newTables.FirstOrDefault(x => x.Name.Equals("OrderDetails"));

                    orderData = GetOrderData(newOrderData, order.ProjectID, detailOrder, true);
                }
            }
            return orderData;
        }

        private List<Dictionary<string, string>> GetOrderData(TableData data, int projectId, IEnumerable<IPrinterJobDetail> productDetails, bool showDataId = false)
        {
            var dicData = new List<Dictionary<string, string>>();

            if (data != null)
            {

                var variableDataIds = JArray.Parse(data.Records)
               .Where(r => productDetails.Any(w => w.ProductDataID.Equals(int.Parse(((JObject)r).Property("ID").Value.ToString()))))
               .Select(s => int.Parse(((JObject)s).Property("ID").Value.ToString()))
               .ToList();

                // TODO: se podria pasar directo el identificador que vienen en productDetails.ProductDataID
                dicData = variableDataRepo.GetAllByDetailID(projectId, true, showDataId, variableDataIds.ToArray()).Select(s => s.Data).ToList();


                //log.LogMessage("Flattent Object for {0} details - {1}", variableDataIds.Count, string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));
            }

            return dicData;
        }

        private int GetArticleId(int orderid)
        {
            int articleId = 0;
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                articleId = ctx.PrinterJobs.FirstOrDefault(p => p.CompanyOrderID == orderid).ArticleID;
            }
            return articleId;
        }

        private string GetArticleCode(int articleId)
        {
            string articleCode = string.Empty;
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                articleCode = ctx.Articles.FirstOrDefault(a => a.ID == articleId).ArticleCode;
            }
            return articleCode;
        }


        // Solo el codigo del pack/articulo y el id del proyecto 
        public List<OrderInfo> Execute(OrderInfo order, int projectID, CancellationToken cancellationToken)
        {
            var newRows = new List<ImportedRow>();

            order.Data.ForEach(o =>
            {
                if(o.GetValue("#OrderNumber").ToString() == order.OrderNumber)
                {
                    newRows.Add(o);
                }
            });

            order.Data.Rows = newRows;

            // NOTE: Create snapshot to be able to modify orders collection while iterating
            var orders = new List<OrderInfo>
            {
                order
            };

            using (var ctx = factory.GetInstance<PrintDB>())
            {

                if (String.IsNullOrWhiteSpace(order.ArticleCode))
                {
                    var article = articleRepo.GetDefaultArticle(ctx);
                    order.ArticleCode = article.ArticleCode;
                    //AttachArticleCodeToVariableData(item, order, article);
                    InitializeArticle(orders, order, article);
                }
                else
                {
                    // first option is looking article code in packs
                    // second: project article code exist
                    // last option: looging in shared articles the code

                    var pack = GetPack(ctx, order.ArticleCode, projectID);

                    if (pack != null)
                    {
                        orders.Remove(order);
                        ExpandPack(ctx, order.ArticleCode, projectID, orders, order);
                    }
                    else
                    {
                        var article = articleRepo.GetByCodeInProject(ctx, order.ArticleCode, projectID);
                        if (article == null)
                            article = articleRepo.GetSharedByCode(ctx, order.ArticleCode);

                        if (article == null)
                            throw new ArticleCodeNotFoundException($"Could not find any article or pack matching the code {order.ArticleCode} for project {projectID}", order.ArticleCode);
                        InitializeArticle(orders, order, article);
                    }
                }
            }
            // await UpdateOrdersDataFile(item, orders);
            return orders;
        }

        private ILabelData GetLabelData(int? labelId)
        {
            if (!labelId.HasValue) return default;
            return labelRepo.GetByID(labelId.Value);
        }

        private void AttachArticleCodeToVariableData(OrderFileItem item, OrderInfo order, IArticle article)
        {
            if (!order.Data.HasColumn("Details.ArticleCode"))
                order.Data.Cols.Add(new ImportedCol(null, "Details.ArticleCode"));
            order.Data.ForEach((r) => r.SetValue("Details.ArticleCode", article.ArticleCode));
            var mapping = item.ImportConfiguration.Output.Mappings.FirstOrDefault(m => m.TargetColumn == "Details.ArticleCode");
            if (mapping == null)
            {
                mapping = new DocumentColMapping()
                {
                    InputColumn = "ArticleCode",
                    TargetColumn = "Details.ArticleCode"
                };
                item.ImportConfiguration.Output.Mappings.Add(mapping);
            }
        }


        private void InitializeArticle(List<OrderInfo> orders, OrderInfo order, IArticle article)
        {
            order.ArticleID = article.ID;
            order.ArticleCode = article.ArticleCode;

            // When LabelID is null, it means this is an item, not a label with variable data.
            //if (article.LabelID == null)
            //{


            //             order.IsItem = true;
            //	order.Data = null;		// This order does not have any variable data because it is an item, not a label
            //}
        }


        public void ExpandPack(PrintDB ctx, string articleCode, int projectID, List<OrderInfo> orders, OrderInfo order)
        {
            var mappingsDictionary = new Dictionary<string, PackMappingInfo>();

            var pack = GetPack(ctx, articleCode, projectID);
            var packArticles = packRepo.GetPackArticles(ctx, pack.ID);

            if (packArticles == null || packArticles.Count == 0)
                throw new ArticleCodeNotFoundException($"Pack with code {articleCode} in project {projectID} does not have any items defined.", articleCode);
            foreach (var packArticle in packArticles)
            {
                switch (packArticle.Type)
                {
                    case PackArticleType.ByArticle:
                        ExpandPackByArticle(ctx, orders, order, pack, packArticle);
                        break;
                    case PackArticleType.ByOrderData:
                        var mapping = GetMappings(ctx, pack, packArticle);
                        if (!mappingsDictionary.ContainsKey(mapping.NormalizedKey))
                            mappingsDictionary[mapping.NormalizedKey] = mapping;
                        else
                            mappingsDictionary[mapping.NormalizedKey].Mappings.AddRange(mapping.Mappings);
                        break;
                    case PackArticleType.ByPlugin:
                        ExpandPackByPlugin(ctx, orders, order, pack, packArticle);
                        break;
                    default:
                        throw new NotImplementedException($"Current Intake Workflow implementation does not support {packArticle.Type} PackArticleType.");
                }
            }

            foreach (var key in mappingsDictionary.Keys)
                ExpandPackByOrderData(orders, order, pack, mappingsDictionary[key]);


        }

        //private void ExpandPack(PrintDB ctx, OrderFileItem item, List<OrderInfo> orders, OrderInfo order)
        //{
        //    var mappingsDictionary = new Dictionary<string, PackMappingInfo>();

        //    var pack = GetPack(ctx, order.ArticleCode, item.ProjectID);
        //    var packArticles = packRepo.GetPackArticles(ctx, pack.ID);

        //    if (packArticles == null || packArticles.Count == 0)
        //        throw new ArticleCodeNotFoundException($"Pack with code {order.ArticleCode} in project {pack.ProjectID} does not have any items defined.", order.ArticleCode);

        //    // NOTE: Remove original order, it will be replaced by N orders after expanding the pack.
        //    orders.Remove(order);

        //    foreach (var packArticle in packArticles)
        //    {
        //        switch (packArticle.Type)
        //        {
        //            case PackArticleType.ByArticle:
        //                ExpandPackByArticle(ctx, orders, order, pack, packArticle);
        //                break;
        //            case PackArticleType.ByOrderData:
        //                var mapping = GetMappings(ctx, pack, packArticle);
        //                if (!mappingsDictionary.ContainsKey(mapping.NormalizedKey))
        //                    mappingsDictionary[mapping.NormalizedKey] = mapping;
        //                else
        //                    mappingsDictionary[mapping.NormalizedKey].Mappings.AddRange(mapping.Mappings);
        //                break;
        //            case PackArticleType.ByPlugin:
        //                ExpandPackByPlugin(ctx, orders, order, pack, packArticle);
        //                break;
        //            default:
        //                throw new NotImplementedException($"Current Intake Workflow implementation does not support {packArticle.Type} PackArticleType.");
        //        }
        //    }

        //    foreach (var key in mappingsDictionary.Keys)
        //        ExpandPackByOrderData(orders, order, pack, mappingsDictionary[key]);
        //}



        private void ExpandPackByArticle(PrintDB ctx, List<OrderInfo> orders, OrderInfo order, IPack pack, IPackArticle packArticle)
        {
            if (packArticle.ArticleID == null)
                throw new ArticleCodeNotFoundException($"Pack with code {order.ArticleCode} has an invalid article: ArticleID is null.", order.ArticleCode);

            var article = articleRepo.GetByID(ctx, packArticle.ArticleID.Value, true);

            var newOrder = Reflex.Clone(order);
            newOrder.ArticleCode = article.ArticleCode;
            newOrder.ArticleID = article.ID;
            newOrder.ArticleName = article.Name;
            newOrder.ArticleLabelID = article.LabelID;
            newOrder.PackCode = pack.PackCode;
            newOrder.Quantity = packArticle.Quantity > 1 ? (newOrder.Quantity * packArticle.Quantity) : newOrder.Quantity;
            


            if (article.LabelID == null)
            {
                //create variabledata for item
                newOrder.IsItem = true;

                //set variableData to Item
                var cloneOderData = CloneImportOrderDataForItems(order, article.ArticleCode, newOrder.Quantity);
                newOrder.Data = cloneOderData.Data;
            }
            else
            {
                // Replace the PackCode in the variable data with the correct article code (NOTE: Notice this change affects the clone, not the original order)
                // Also apply quantity multiplier to each row

                var detailsArticleCodeColumn = newOrder.Data.GetTargetColumnByName("Details.ArticleCode");
                var detailsQuantityColumn = newOrder.Data.GetTargetColumnByName("Details.Quantity");

                newOrder.Data.ForEach((r) =>
                {
                    r.SetValue(detailsArticleCodeColumn, article.ArticleCode);
                    if (packArticle.Quantity > 1)
                    {
                        var q = Convert.ToInt32(r.GetValue(detailsQuantityColumn));
                        r.SetValue(detailsQuantityColumn, q * packArticle.Quantity);
                    }
                });
                newOrder.Quantity = newOrder.Data.Sum("Details.Quantity");
                var label = GetLabelData(article.LabelID.Value);
                newOrder.ArticleLabelType = GetArticleLabelType(label.Type);
                newOrder.ArticleEncodeRIFD = label.EncodeRFID;
            }

            orders.Add(newOrder);
        }


        private void ExpandPackByOrderData(List<OrderInfo> orders, OrderInfo order, IPack pack, PackMappingInfo mapping)
        {
            var orderClone = Reflex.Clone(order);
            var src = order.Data;
            var dst = orderClone.Data;
            dst.ClearRows();

            var detailsQuantityColumn = order.Data.GetTargetColumnByName("Details.Quantity");
            var detailsArticleCodeColumn = order.Data.GetTargetColumnByName("Details.ArticleCode");

            src.ForEach((r) =>
            {
                var matches = mapping.GetMatchFromRowData(src);
                foreach (var match in matches)
                {
                    var newRow = new ImportedRow();
                    newRow.Data = Reflex.Clone(r.Data);
                    dst.Rows.Add(newRow);

                    newRow.SetValue(detailsArticleCodeColumn, match.Article.ArticleCode);
                    if (match.Quantity > 1)
                    {
                        var q = Convert.ToInt32(newRow.GetValue(detailsQuantityColumn));
                        newRow.SetValue(detailsQuantityColumn, q * match.Quantity);
                    }
                }
            });

            // Since we might end up evaluating many different mappings at once, and each mapping might produce an article code that
            // is different from the previous row, we can easily end up with an order that contains multiple article codes; so now we
            // need to split into idividual orders...

            SplitPackOrders(orders, orderClone, dst, pack, mapping);
        }

        private void ExpandPackByPlugin(PrintDB ctx, List<OrderInfo> orders, OrderInfo order, IPack pack, IPackArticle packArticle)
        {
            var articleCodes = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(packArticle.PluginName))
            {
                using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    using(var plugin = pluginManager.GetInstanceByName(packArticle.PluginName))
                    {
                        plugin.GetPackArticles(order.Data, articleCodes);
                    }
                }
            }

            foreach (var code in articleCodes)
            {
                var article = articleRepo.GetByCodeInProject(ctx, code.Key, pack.ProjectID);
                if (article != null)
                {

                    var newOrder = Reflex.Clone(order);
                    newOrder.ArticleCode = article.ArticleCode;
                    newOrder.ArticleID = article.ID;
                    newOrder.ArticleName = article.Name;
                    newOrder.ArticleLabelID = article.LabelID;
                    newOrder.PackCode = pack.PackCode;
                    newOrder.Quantity = packArticle.Quantity > 1 ? (code.Value * packArticle.Quantity) : code.Value;

                   

                    if (article.LabelID == null)
                    {
                        newOrder.IsItem = true;
                        var cloneOderData = CloneImportOrderDataForItems(order, article.ArticleCode, newOrder.Quantity);
                        newOrder.Data = cloneOderData.Data;

                    }
                    else
                    {
                        newOrder.IsItem = false;
                        var label = GetLabelData(article.LabelID.Value);
                        newOrder.ArticleLabelType = GetArticleLabelType(label.Type);
                        newOrder.ArticleEncodeRIFD = label.EncodeRFID;
                    }

                    orders.Add(newOrder);
                }
            }
        }

        private string GetArticleLabelType(LabelType type)
        {
            switch (type)
            {
                case LabelType.Sticker:
                    return "Sticker";
                case LabelType.CareLabel:
                    return "Care Label";
                case LabelType.HangTag:
                    return "Hang Tag";
                case LabelType.PiggyBack:
                    return "Piggy Back";
                default:
                    return "";



            }
        }

        private void SplitPackOrders(List<OrderInfo> orders, OrderInfo order, ImportedData data, IPack pack, PackMappingInfo mapping)
        {
            var detailsQuantityColumn = data.GetTargetColumnByName("Details.Quantity");
            var groups = data.GroupBy("Details.ArticleCode");
            foreach (var group in groups)
            {
                var matchingMapping = mapping.Mappings.Where(m => m.Article.ArticleCode == group.Key).First();
                var newOrder = Reflex.Clone(order);
                newOrder.PackCode = pack.PackCode;
                newOrder.ArticleID = matchingMapping.Article.ID;
                newOrder.ArticleCode = matchingMapping.Article.ArticleCode;
                newOrder.Quantity = group.Sum(r => Convert.ToInt32(r.GetValue(detailsQuantityColumn)));
                newOrder.ArticleName = matchingMapping.Article.Name;

                //newOrder.Data = new ImportedData()
                //{
                //    Cols = order.Data.Cols,
                //    Rows = group.Rows
                //};
                //newOrder.IsItem = matchingMapping.Article.LabelID == null ? true : false;

                if (matchingMapping.Article.LabelID == null)
                {
                    var row = Reflex.Clone(group.Rows[0]);

                    row.SetValue(detailsQuantityColumn, newOrder.Quantity);

                    newOrder.Data = new ImportedData()
                    {
                        Cols = order.Data.Cols,
                        Rows = new List<ImportedRow>() { row }
                    };
                    newOrder.IsItem = true;
                }
                else
                {
                    newOrder.Data = new ImportedData()
                    {
                        Cols = order.Data.Cols,
                        Rows = group.Rows
                    };
                    newOrder.IsItem = false;
                    var label = GetLabelData(matchingMapping.Article.LabelID.Value);
                    newOrder.ArticleLabelType = GetArticleLabelType(label.Type);
                    newOrder.ArticleEncodeRIFD = label.EncodeRFID;
                }

                orders.Add(newOrder);
            }
        }


        private IPack GetPack(PrintDB ctx, string packode, int projectid)
        {
            var pack = packRepo.GetByCodeInProject(ctx, packode, projectid);
            //if (pack == null)
            //	throw new ArticleCodeNotFoundException($"Could not find any article or pack matching the code {packode} for project {projectid}", packode);

            return pack;
        }


        private PackMappingInfo GetMappings(PrintDB ctx, IPack pack, IPackArticle packArticle)
        {
            if (String.IsNullOrWhiteSpace(packArticle.FieldName))
                throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: FieldName cannot be null or empty.", pack.PackCode);

            if (String.IsNullOrWhiteSpace(packArticle.Mapping))
                throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: Mapping cannot be null or empty when type is {packArticle.Type}.", pack.PackCode);

            List<MappingDTO> mappings;
            try
            {
                mappings = JsonConvert.DeserializeObject<List<MappingDTO>>(packArticle.Mapping);
            }
            catch (Exception ex)
            {
                throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: Mapping cannot be deserialized.", ex, pack.PackCode);
            }

            PackMappingInfo result = new PackMappingInfo(packArticle.FieldName, pack.PackCode);
            foreach (var mapping in mappings)
            {
                if (String.IsNullOrWhiteSpace(mapping.Key))
                    throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: Mapping Key cannot be null or empty.", pack.PackCode);

                if (String.IsNullOrWhiteSpace(mapping.Value))
                    throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: Mapping Value cannot be null or empty.", pack.PackCode);

                var article = articleRepo.GetByCodeInProject(ctx, mapping.Value, pack.ProjectID);
                if (article == null)
                    throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: Article code {mapping.Value} could not be found.", mapping.Value);

                result.Mappings.Add(new PackArticleMappingInfo(mapping.Key, mapping.Value, packArticle.Quantity, article));
            }

            if (result.Mappings.Count == 0)
                throw new ArticleCodeNotFoundException($"Pack with code {pack.PackCode} contains an invalid article definition: There are no mappings to evaluate.", pack.PackCode);

            return result;
        }


        private async Task UpdateOrdersDataFile(OrderFileItem item, List<OrderInfo> orders)
        {
            var file = await storeManager.GetFileAsync(item.OrdersDataFile);
            byte[] fileContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orders));
            file.SetContent(fileContent);
        }

        //private ImportedData CloneOrderData(OrderInfo order,string articleCode,int Quantity)
        //{
        //    ImportedData cloneOrderData = new ImportedData();

        //    cloneOrderData.Cols.Add(new ImportedCol("OrderNumber", "#OrderNumber"));
        //    cloneOrderData.Cols.Add(new ImportedCol("OrderDate", "OrderDate"));
        //    cloneOrderData.Cols.Add(new ImportedCol("BillTo", "BillTo"));
        //    cloneOrderData.Cols.Add(new ImportedCol("SendTo", "SendTo"));
        //    cloneOrderData.Cols.Add(new ImportedCol("ArticleCode", "Details.ArticleCode"));
        //    cloneOrderData.Cols.Add(new ImportedCol("Quantity", "Details.Quantity"));
        //    cloneOrderData.Cols.Add(new ImportedCol("Barcode", "Details.Product.Barcode"));
        //    cloneOrderData.Cols.Add(new ImportedCol("TXT1", "Details.Product.TXT1"));
        //    cloneOrderData.Cols.Add(new ImportedCol("TXT2", "Details.Product.TXT2"));
        //    cloneOrderData.Cols.Add(new ImportedCol("TXT3", "Details.Product.TXT3"));
        //    cloneOrderData.Cols.Add(new ImportedCol("Size", "Details.Product.Size"));
        //    cloneOrderData.Cols.Add(new ImportedCol("Color", "Details.Product.Color"));


        //    var newrow = new ImportedRow();

        //    cloneOrderData.Cols.ForEach((r)=>
        //    {
        //        var c = r.InputColumn;

        //        if( c == "ArticleCode")
        //        {
        //            newrow.Data.Add(c, articleCode);
        //            return;
        //        }

        //        if (c == "Quantity")
        //        {
        //            newrow.Data.Add(c, Quantity);
        //            return;
        //        }

        //        if (c == "TXT1")
        //        {
        //            newrow.Data.Add(c, articleCode);
        //            return;
        //        }

        //        var val = order.Data.Rows[0].Data.Where(x => x.Key.Contains(c)).Select(x => x.Value).FirstOrDefault();
        //        newrow.Data.Add(c, val);               
        //    });

        //    cloneOrderData.Rows.Add(newrow);

        //    return cloneOrderData;
        //}

        private OrderInfo CloneImportOrderDataForItems(OrderInfo order, string articleCode, int Quantity)
        {
            var newOrder = Reflex.Clone(order);

            if (newOrder.Data.Rows.Count > 1)
            {
                newOrder.Data.Rows.RemoveRange(1, newOrder.Data.Rows.Count - 1);
            }

            var detailsArticleCodeColumn = order.Data.GetTargetColumnByName("Details.ArticleCode");
            var detailsQuantityColumn = order.Data.GetTargetColumnByName("Details.Quantity");

            newOrder.Data.ForEach((r) =>
            {
                r.SetValue(detailsArticleCodeColumn, articleCode);
                r.SetValue(detailsQuantityColumn, Quantity);

            });

            return newOrder;
        }
    }

    class PackMappingInfo
    {
        public string NormalizedKey;
        public string FieldName;
        public string PackCode;
        public List<PackArticleMappingInfo> Mappings;

        public PackMappingInfo(string fieldName, string packCode)
        {
            NormalizedKey = fieldName.ToLower();
            FieldName = fieldName;
            PackCode = packCode;
            Mappings = new List<PackArticleMappingInfo>();
        }

        public List<PackArticleMappingInfo> GetMatchFromRowData(ImportedData data)
        {
            var value = data.GetValue("Details.Product." + FieldName).ToString();
            var matches = Mappings.Where(m => String.Compare(m.Key.Trim(), value.Trim(), true) == 0).ToList();

            if (matches.Count == 0)
                throw new ArticleCodeNotFoundException($"Could not find an article match for value {value} in pack {PackCode}", PackCode);

            return matches;
        }
    }


    class PackArticleMappingInfo
    {
        public string Key;
        public string Value;
        public int Quantity;
        public IArticle Article;

        public PackArticleMappingInfo(string key, string value, int quantity, IArticle article)
        {
            Key = key;
            Value = value;
            Quantity = quantity;
            Article = article;
        }
    }
}
