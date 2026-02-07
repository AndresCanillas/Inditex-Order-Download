using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;



namespace WebLink.Contracts.Models
{
    public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
    {

        //public const IList<string> COMPO_CATALOGS_NAMES = new List<string>() { "OrderDetails", "VariableData", "Orders", "CompositionLabel", "UserSections", "UserFibers", "UserCareInstructions", "Fibers", "Sections", "CareInstructions" };

        public IList<CompositionDefinition> GetUserCompositionForGroup(int orderGroupId, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSeparator = ",")
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                return GetUserCompositionForGroup(ctx, orderGroupId, joinLang, languages, langSeparator);
            }
        }

        public IList<CompositionDefinition> GetUserCompositionForGroup(PrintDB ctx, int orderGroupId, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSeparator = ",")
        {

            if(languages == null)
            {
                languages = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            }

            var keyField = "Color";
            var compoLabelsIds = new List<int>();
            var sectionsIds = new List<int>();

            var userCompositions = new List<CompositionDefinition>();
            var userSections = new List<Section>();
            var userFibers = new List<Fiber>();
            var userCareInstructions = new List<CareInstruction>();

            /*
            var orders = ctx.CompanyOrders
                .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { Order = o, Printjob = j })
                .Join(ctx.Articles, j1 => j1.Printjob.ArticleID, art => art.ID, (join1, a) => new { join1.Order, join1.Printjob, Article = a })
                .Join(ctx.Labels, j2 => j2.Article.LabelID, lbl => lbl.ID, (join2, l) => new { join2.Order, Label = l })
                .Where(w => w.Order.OrderGroupID == orderGroupId)
                .Where(w => w.Order.OrderStatus != OrderStatus.Cancelled)
                .Where(w => w.Label.IncludeComposition)
                .Select(s => s.Order)
                .ToList();

            

            var firstOrder = orders.FirstOrDefault();

            if (firstOrder == null)
                throw new Exception($" Looking Composition - Cannot get orders for OrderGroupID [{orderGroupId}]");

            var projectID = firstOrder.ProjectID;*/
            var projectID = ctx.OrderGroups.Where(w => w.ID == orderGroupId).First().ProjectID;
            var project = ctx.Projects.FirstOrDefault(p => p.ID == projectID);

            var catalogs = GetCompositionCatalogsForProject(ctx, projectID);

            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            var orderCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            var compoCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            var userSectionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_SECTIONS_CATALOG));
            var userFiberCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_FIBERS_CATALOG));
            var userCareInstructionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG));
            var brandSectionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_SECTIONS_CATALOG));
            var brandFibersCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_FIBERS_CATALOG));
            var brandCareInstructionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_CAREINSTRUCTIONS_CATALOG));


            var printJobsDetails = ctx.PrinterJobDetails
                .Join(ctx.PrinterJobs, pjd => pjd.PrinterJobID, pj => pj.ID, (d, j) => new { PrinterJobDetail = d, PrinterJob = j })
                .Join(ctx.CompanyOrders, join1 => join1.PrinterJob.CompanyOrderID, ord => ord.ID, (j1, ord) => new { j1.PrinterJobDetail, j1.PrinterJob, CompanyOrder = ord })
                .Join(ctx.Articles, j2 => j2.PrinterJob.ArticleID, art => art.ID, (join2, a) => new { join2.PrinterJobDetail, join2.PrinterJob, join2.CompanyOrder, Article = a })
                .Join(ctx.Labels, j3 => j3.Article.LabelID, lbl => lbl.ID, (join3, l) => new { join3.PrinterJobDetail, join3.PrinterJob, join3.CompanyOrder, join3.Article, Label = l })
                .Where(w => w.CompanyOrder.OrderGroupID.Equals(orderGroupId)) // for OrderGroupID
                .Where(w => w.CompanyOrder.OrderStatus != OrderStatus.Cancelled)
                .Where(w => w.Label.IncludeComposition)
                .Select(s => new { ProductDataID = s.PrinterJobDetail.ProductDataID, OrderID = s.CompanyOrder.ID })
                .ToList();


            #region Get Composition DynamicDB
            using(DynamicDB dynDb = connManager.CreateDynamicDB())
            {
                var detailIds = printJobsDetails.Select(s => s.ProductDataID).Distinct().ToList();

                if(detailIds.Count < 1)
                    detailIds.Add(0);

                // all compositions defined for current OrderGroup
                var variableData = dynDb.Select(compoCatalog.CatalogID,
                $@"SELECT c.*, v.HasComposition, d.ArticleCode, v.{keyField} as KeyValue, d.ID as ProductDataID
                FROM #TABLE c
                INNER JOIN {variableDataCatalog.TableName} v ON v.HasComposition = c.ID
                INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                WHERE d.ID IN ({string.Join(",", detailIds)})
                ORDER BY d.ID
                ");

                foreach(JObject row in variableData)
                {
                    compoLabelsIds.Add(row.GetValue<int>("HasComposition"));
                    userCompositions.Add(new CompositionDefinition
                    {
                        ID = row.GetValue<int>("HasComposition"),
                        KeyName = keyField,
                        KeyValue = row.GetValue<string>("KeyValue"),
                        ProductDataID = row.GetValue<int>("ProductDataID"), // PrintJobDetails.ProductDataID <===> OrderDetail.ID
                        OrderID = printJobsDetails.First(w => w.ProductDataID.Equals(row.GetValue<int>("ProductDataID"))).OrderID,
                        OrderGroupID = orderGroupId,
                        ArticleCode = row.GetValue<string>("ArticleCode"),
                        ArticleID = 0,
                        TargetArticle = row.GetValue<int>("TargetArticle"),
                        WrTemplate = row.GetValue<int>("WrTemplate"), 
                        //ExceptionsLocation = row.GetValue<int>("ExceptionsLocation")

                    });
                }

                // sections
                if(compoLabelsIds.Count < 1)
                    compoLabelsIds.Add(0);

                var sectionSetField = dynDb.GetCatalog(compoCatalog.CatalogID).Fields.FirstOrDefault(f => f.Name.Equals("Sections"));
                string[] sectionFields = { "ID", "Code", "Category", "Position", "IsActive" };

                var languageFields = dynDb.GetCatalog(brandSectionsCatalog.CatalogID).Fields.Where(f => !sectionFields.Contains(f.Name));
                // TODO: add flag as parameter to enable all langs
                //var allLangs = true ? languageFields.Select(x => "s." + x.Name).ToList() : languageFields.Where(x => x.Name.ToLower().Equals("English".ToLower())).Select(x => "s." + x.Name).ToList();
                //var langsQuery = allLangs.Count > 1 ? $"concat({ string.Join("+',',", allLangs) })" : allLangs.FirstOrDefault().ToString();


                if(!languages.TryGetValue(CompoCatalogName.SECTIONS, out IEnumerable<string> lang))
                {
                    lang = languageFields.Select(x => x.Name).ToList();
                }


                var langsQuery = GetLanguageQuery(languageFields, joinLang, lang, langSeparator);

                var sections = dynDb.Select(userSectionsCatalog.CatalogID,
                $@"SELECT us.*, r.SourceID as CompositionID, s.Code, {langsQuery} 
                FROM #TABLE us
                INNER JOIN REL_{compoCatalog.CatalogID}_{userSectionsCatalog.CatalogID}_{sectionSetField.FieldID} r
                ON r.TargetID = us.ID
                INNER JOIN {brandSectionsCatalog.TableName} s
                ON us.Section = s.ID
                WHERE r.SourceID IN ({string.Join(",", compoLabelsIds)})");

                foreach(JObject sec in sections)
                {
                    sectionsIds.Add(sec.GetValue<int>("ID"));
                    userSections.Add(new Section
                    {
                        ID = sec.GetValue<int>("ID"),
                        SectionID = sec.GetValue<int>("Section"),
                        Code = sec.GetValue<string>("Code"),
                        Percentage = sec.GetValue<string>("Percentage"),
                        Sort = sec.GetValue<int>("Sort"),
                        CompositionID = sec.GetValue<int>("CompositionID"),
                        AllLangs = sec.GetValue<string>("AllLangs"),
                        IsBlank = sec.GetValue<bool>("IsBlank"),
                        IsMainTitle = sec.GetValue<bool>("IsMainTitle"),
                    });
                }

                if(sectionsIds.Count < 1)
                    sectionsIds.Add(0);

                // fibers
                //var fiberSetField = dynDb.GetCatalog(sectionsCatalog.CatalogID).Fields.FirstOrDefault(f => f.Name.Equals("Fibers"));
                var fiberSetField = userSectionsCatalog.Fields.FirstOrDefault(f => f.Name.Equals("Fibers"));

                string[] fiberFields = { "ID", "Code", "Category", "IsActive" };

                languageFields = dynDb.GetCatalog(brandFibersCatalog.CatalogID).Fields.Where(f => !fiberFields.Contains(f.Name));
                // TODO: add flag as parameter to enable all langs
                //allLangs = true ? languageFields.Select(x => "f." + x.Name).ToList() : languageFields.Where(x => x.Name.ToLower().Equals("English".ToLower())).Select(x => "f." + x.Name).ToList();
                //langsQuery = allLangs.Count > 1 ? $"concat({ string.Join("+',',", allLangs) })" : allLangs.FirstOrDefault().ToString();


                if(!languages.TryGetValue(CompoCatalogName.FIBERS, out lang))
                {
                    lang = languageFields.Select(x => x.Name).ToList();
                }

                langsQuery = GetLanguageQuery(languageFields, joinLang, lang, langSeparator);

                if(sectionsIds.Count < 1)
                    sectionsIds.Add(0);

                var fibers = dynDb.Select(userFiberCatalog.CatalogID,
                $@"SELECT uf.*, r.SourceID AS SectionID, f.Code, {langsQuery}
                FROM #TABLE uf
                INNER JOIN REL_{userSectionsCatalog.CatalogID}_{userFiberCatalog.CatalogID}_{fiberSetField.FieldID} r
                ON r.TargetID = uf.ID
                INNER JOIN {brandFibersCatalog.TableName} f
                ON uf.Fiber = f.ID
                WHERE r.SourceID IN ({string.Join(",", sectionsIds)})");

                foreach(JObject fb in fibers)
                    userFibers.Add(new Fiber
                    {
                        ID = fb.GetValue<int>("ID"),
                        FiberID = fb.GetValue<int>("Fiber"),
                        Code = fb.GetValue<string>("Code"),
                        Percentage = fb.GetValue<string>("Percentage"),
                        CountryOfOrigin = fb.GetValue<string>("CountryOfOrigin"),
                        FiberIcon = fb.GetValue<string>("FiberIcon"),
                        FiberType = fb.GetValue<string>("FiberType"),
                        SectionID = fb.GetValue<int>("SectionID"),
                        AllLangs = fb.GetValue<string>("AllLangs")
                    });


                // Care Instructions and exceptions
                var ciSetField = compoCatalog.Fields.FirstOrDefault(f => f.Name.Equals("CareInstructions"));

                string[] ciFields = { "ID", "Code", "Category", "Symbol", "SymbolType", "IsActive" };

                languageFields = dynDb.GetCatalog(brandCareInstructionsCatalog.CatalogID).Fields.Where(f => !ciFields.Contains(f.Name));
                //allLangs = true ? languageFields.Select(x => "ci." + x.Name).ToList() : languageFields.Where(x => x.Name.ToLower().Equals("English".ToLower())).Select(x => "ci." + x.Name).ToList();
                //langsQuery = allLangs.Count > 1 ? $"concat({ string.Join("+',',", allLangs) })" : allLangs.FirstOrDefault().ToString();



                if(!languages.TryGetValue(CompoCatalogName.CAREINSTRUCTIONS, out IEnumerable<string> langListCare))
                {
                    langListCare = languageFields.Select(x => x.Name).ToList();
                }

                if(!languages.TryGetValue(CompoCatalogName.EXCEPTIONS, out IEnumerable<string> langListExceptions))
                {
                    langListExceptions = languageFields.Select(x => x.Name).ToList();
                }

                if(!languages.TryGetValue(CompoCatalogName.ADDITIONALS, out IEnumerable<string> langListAdditionals))
                {
                    langListAdditionals = languageFields.Select(x => x.Name).ToList();

                }

                var langsQueryCare = GetLanguageQuery(languageFields, joinLang, langListCare, langSeparator);
                var langsQueryExceptions = GetLanguageQuery(languageFields, joinLang, langListExceptions, langSeparator);
                var langsQueryAdditional = GetLanguageQuery(languageFields, joinLang, langListAdditionals, langSeparator);

                var careInstructions = dynDb.Select(userCareInstructionsCatalog.CatalogID,
                $@"
SELECT * FROM (
                    SELECT uci.*, r.SourceID AS CompositionID, ci.Code, ci.Category, ci.SymbolType, ci.Symbol, {langsQueryCare} 
                    FROM #TABLE uci
                    INNER JOIN REL_{compoCatalog.CatalogID}_{userCareInstructionsCatalog.CatalogID}_{ciSetField.FieldID} r
                    ON r.TargetID = uci.ID
                    INNER JOIN {brandCareInstructionsCatalog.TableName} ci
                    ON uci.Instruction = ci.ID
                    WHERE r.SourceID IN ({string.Join(",", compoLabelsIds)}) AND ci.Category NOT IN ('Exception', 'Additional')

                    UNION ALL

                    (SELECT uci.*, r.SourceID AS CompositionID, ci.Code, ci.Category, ci.SymbolType, ci.Symbol, {langsQueryAdditional} 
                    FROM #TABLE uci
                    INNER JOIN REL_{compoCatalog.CatalogID}_{userCareInstructionsCatalog.CatalogID}_{ciSetField.FieldID} r
                    ON r.TargetID = uci.ID
                    INNER JOIN {brandCareInstructionsCatalog.TableName} ci
                    ON uci.Instruction = ci.ID
                    WHERE r.SourceID IN ({string.Join(",", compoLabelsIds)}) AND ci.Category = 'Additional')

                    UNION ALL

                    (SELECT uci.*, r.SourceID AS CompositionID, ci.Code, ci.Category, ci.SymbolType, ci.Symbol, {langsQueryExceptions} 
                    FROM #TABLE uci
                    INNER JOIN REL_{compoCatalog.CatalogID}_{userCareInstructionsCatalog.CatalogID}_{ciSetField.FieldID} r
                    ON r.TargetID = uci.ID
                    INNER JOIN {brandCareInstructionsCatalog.TableName} ci
                    ON uci.Instruction = ci.ID
                    WHERE r.SourceID IN ({string.Join(",", compoLabelsIds)}) AND ci.Category = 'Exception')
) allUci
ORDER BY CompositionID, Position
                    ");

                foreach(JObject ci in careInstructions)
                    userCareInstructions.Add(new CareInstruction
                    {
                        ID = ci.GetValue<int>("ID"),
                        Instruction = ci.GetValue<int>("Instruction"),
                        Code = ci.GetValue<string>("Code"),
                        Category = ci.GetValue<string>("Category"),
                        CompositionID = ci.GetValue<int>("CompositionID"),
                        AllLangs = ci.GetValue<string>("AllLangs"),
                        SymbolType = ci.GetValue<string>("SymbolType"),
                        Symbol = ci.GetValue<string>("Symbol"),
                        Position = ci.GetValue<int>("Position", 0)

                    });


                foreach(var compo in userCompositions)
                {
                    compo.Sections = userSections.Where(w => w.CompositionID.Equals(compo.ID)).ToList();
                    compo.CareInstructions = userCareInstructions.Where(w => w.CompositionID.Equals(compo.ID)).OrderBy(o => o.Position).ToList();
                    foreach(var us in compo.Sections)
                        us.Fibers = userFibers.Where(w => w.SectionID.Equals(us.ID)).ToList();
                }
            }
            #endregion Get Composition DynamicDB


            return userCompositions;
        }

        public CompositionLabelData GetUserCompositionForOrder(int orderID)
        {
            var keyField = "Color";
            var compoLabelsIds = new List<int>();
            var sectionsIds = new List<int>();

            var userCompositions = new List<CompositionDefinition>();
            var userSections = new List<Section>();
            var userFibers = new List<Fiber>();
            var userCareInstructions = new List<CareInstruction>();
            IOrder order;

            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                order = GetByID(ctx, orderID);

                var catalogsNames = Catalog.GetAllCompoCatalogNames;

                var catalogs = ctx.Catalogs
                    .Where(w => w.ProjectID.Equals(order.ProjectID))
                    .Where(w => catalogsNames.Contains(w.Name))
                    .ToList();

                var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
                var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                var orderCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
                var compoCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
                var sectionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_SECTIONS_CATALOG));
                var fiberCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_FIBERS_CATALOG));
                var careInstructionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG));

                var printJobsDetails = ctx.PrinterJobDetails
                    .Join(ctx.PrinterJobs, pjd => pjd.PrinterJobID, pj => pj.ID, (d, j) => new { PrinterJobDetail = d, PrinterJob = j })
                    .Where(w => w.PrinterJob.CompanyOrderID.Equals(orderID)) // for CompanyOrder
                    .Select(s => s.PrinterJobDetail).ToList();


                #region Get Composition
                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {
                    var detailIds = printJobsDetails.Select(s => s.ProductDataID).ToList();

                    if(detailIds.Count < 1)
                        detailIds.Add(0);
                    // al compositions defined for current CompanyOrder (ArticleLine)
                    // add join to CompositionLabel Table to get more compo info
                    var variableData = dynDb.Select(compoCatalog.CatalogID,
                    $@"SELECT c.*, v.HasComposition, d.ArticleCode, v.{keyField} as KeyValue, d.ID as ProductDataID
                    FROM #TABLE c
                    INNER JOIN {variableDataCatalog.TableName} v ON v.HasComposition = c.ID
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE v.HasComposition IS NOT NULL 
                    AND d.ID IN (@detailIds)
                    ", string.Join(",", detailIds));

                    foreach(JObject row in variableData)
                    {
                        compoLabelsIds.Add(row.GetValue<int>("HasComposition"));
                        userCompositions.Add(new CompositionDefinition
                        {
                            ID = row.GetValue<int>("HasComposition"),
                            KeyName = keyField,
                            KeyValue = row.GetValue<string>("KeyValue"),
                            ProductDataID = row.GetValue<int>("ProductDataID") // PrintJobDetails Table -> ProductDataID Field
                        });
                    }

                    // sections
                    var sectionSetField = dynDb.GetCatalog(compoCatalog.CatalogID).Fields.FirstOrDefault(f => f.Name.Equals("Sections"));
                    var sections = dynDb.Select(sectionsCatalog.CatalogID,
                    $@"SELECT s.*, r.SourceID as CompositionID
                    FROM #TABLE s
                    INNER JOIN REL_{compoCatalog.CatalogID}_{sectionsCatalog.CatalogID}_{sectionSetField.FieldID} r
                    ON r.TargetID = s.ID
                    WHERE r.SourceID IN (@compoLabelsIds)",
                    string.Join(",", compoLabelsIds));

                    foreach(JObject sec in sections)
                    {
                        sectionsIds.Add(sec.GetValue<int>("ID"));
                        userSections.Add(new Section
                        {
                            ID = sec.GetValue<int>("ID"),
                            Code = sec.GetValue<string>("Code"),
                            Percentage = sec.GetValue<string>("Percentage"),
                            Sort = sec.GetValue<int>("Sort"),
                            CompositionID = sec.GetValue<int>("CompositionID")
                        });
                    }

                    // fibers
                    //var fiberSetField = dynDb.GetCatalog(sectionsCatalog.CatalogID).Fields.FirstOrDefault(f => f.Name.Equals("Fibers"));
                    var fiberSetField = sectionsCatalog.Fields.FirstOrDefault(f => f.Name.Equals("Fibers"));
                    var fibers = dynDb.Select(fiberCatalog.CatalogID,
                    $@"SELECT f.*, r.SourceID AS SectionID
                    FROM #TABLE f
                    INNER JOIN REL_{sectionsCatalog.CatalogID}_{fiberCatalog.CatalogID}_{fiberSetField.FieldID} r
                    ON r.TargetID = f.ID
                    WHERE r.SourceID IN ({string.Join(",", sectionsIds)})");

                    // Care Instructions and exceptions
                    var ciSetField = compoCatalog.Fields.FirstOrDefault(f => f.Name.Equals("CareInstructions"));
                    var careInstructions = dynDb.Select(careInstructionsCatalog.CatalogID,
                    $@"SELECT ci.*, r.SourceID AS CompositionID
                    FROM #TABLE ci
                    INNER JOIN REL_{compoCatalog.CatalogID}_{careInstructionsCatalog.CatalogID}_{ciSetField.FieldID} r
                    ON r.TargetID = ci.ID
                    WHERE r.SourceID IN ({string.Join(",", compoLabelsIds)})");

                    foreach(JObject fb in fibers)
                        userFibers.Add(new Fiber
                        {
                            ID = fb.GetValue<int>("ID"),
                            Code = fb.GetValue<string>("Code"),
                            CountryOfOrigin = fb.GetValue<string>("CountryOfOrigin"),
                            FiberIcon = fb.GetValue<string>("FiberIcon"),
                            FiberType = fb.GetValue<string>("FiberType"),
                            SectionID = fb.GetValue<int>("SectionID"),
                        });

                    foreach(JObject ci in careInstructions)
                        userCareInstructions.Add(new CareInstruction
                        {
                            ID = ci.GetValue<int>("ID"),
                            Code = ci.GetValue<string>("Code"),
                            Category = ci.GetValue<string>("Category"),
                            CompositionID = ci.GetValue<int>("CompositionID")
                        });

                    foreach(var compo in userCompositions)
                    {
                        compo.Sections = userSections.Where(w => w.CompositionID.Equals(compo.ID)).ToList();
                        compo.CareInstructions = userCareInstructions.Where(w => w.CompositionID.Equals(compo.ID)).ToList();
                        compo.OrderID = order.ID;

                        foreach(var us in compo.Sections)
                            us.Fibers = userFibers.Where(w => w.SectionID.Equals(us.ID)).ToList();
                    }
                }
                #endregion Get Composition
            }

            return new CompositionLabelData()
            {
                OrderGroupId = order.OrderGroupID,
                OrderID = order.ID,
                Definition = userCompositions
            };
        }

        public void SaveComposition(int projectId, int rowId, string composition, string careInstructions, string symbols = null)
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var compositionLabelCatalog = ctx.Catalogs
                                            .FirstOrDefault(catalog =>
                                                    catalog.ProjectID.Equals(projectId) && catalog.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {
                    var detail = dynDb.SelectOne(compositionLabelCatalog.CatalogID, rowId);
                    detail["FullComposition"] = composition;
                    detail["FullCareInstructions"] = careInstructions;
                    detail["Symbols"] = symbols;

                    dynDb.Update(compositionLabelCatalog.CatalogID, JsonConvert.SerializeObject(detail));
                }

            }

        }

        public void SaveComposition(int projectId, int rowId, Dictionary<string, string> composition, string careInstructions, string symbols)
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var compositionLabelCatalog = ctx.Catalogs
                                            .FirstOrDefault(catalog =>
                                                    catalog.ProjectID.Equals(projectId) && catalog.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {
                    var detail = dynDb.SelectOne(compositionLabelCatalog.CatalogID, rowId);

                    CleanComositionRecord(detail, compositionLabelCatalog);

                    foreach(var s in composition)
                    {
                        detail[s.Key] = s.Value;

                    }

                    // can override care intructions and symbols
                    if(!string.IsNullOrEmpty(careInstructions))
                        detail["FullCareInstructions"] = careInstructions;
                    if(!string.IsNullOrEmpty(symbols))
                        detail["Symbols"] = symbols;

                    dynDb.Update(compositionLabelCatalog.CatalogID, JsonConvert.SerializeObject(detail));
                }

            }

        }

        public Dictionary<string, string> GetCompostionData(int projectId, int rowId)
        {
            var output = new Dictionary<string, string>();
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var compositionLabelCatalog = ctx.Catalogs
                                            .FirstOrDefault(catalog =>
                                                    catalog.ProjectID.Equals(projectId) && catalog.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

                using(DynamicDB dynDb = connManager.CreateDynamicDB())
                {
                    var detail = dynDb.SelectOne(compositionLabelCatalog.CatalogID, rowId);
                    foreach(var property in detail.Properties())
                    {
                        output.Add(property.Name, property.Value.ToString());
                    }

                }

            }

            return output;
        }

        private void CleanComositionRecord(JObject record, ICatalog catalog)
        {

            var fields = catalog.Fields;

            // ignore base fields , only clean generated text
            var tableFields = fields
                .Where(w => w.Name != "ID")
                .Where(w => w.Name != "Type")
                .Where(w => w.Name != "TargetArticle")
                .Where(w => w.Name != "EnableCompositionSection")
                .Where(w => w.Name != "EnableWashingRulesSection")
                .Where(w => w.Name != "EnableExceptions")
                .Where(w => w.Name != "WrTemplate")
                .Where(w => w.Type != ColumnType.Set && w.Type != ColumnType.Reference)
                .ToList();


            tableFields.ForEach(f =>
            {
                record[f.Name] = new JValue(DynamicDB.GetDefaultValue(f.Type));
            });

            //var recordFields = record.Properties()
            //    .Where(w => w.Name != "ID")
            //    .ToList();

            //recordFields.ForEach(s => record[s.Name] = null);
        }

        #region Register Composition Manager

        public void AddCompositionOrder(CompositionDefinition composition)
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                AddCompositionOrder(ctx, composition);
            }
        }

        public void AddCompositionOrder(PrintDB ctx, CompositionDefinition composition)
        {
            IOrder order;


            if(composition.OrderID < 1)
            {
                throw new Exception("this composition dont has order");

                //// Insert DynDb
                //var orderDataID = InsertCompositionData(ctx, composition);
                //// Insert PrintDb -> create order,  1 printjob and 1 printjobdetail
                //order = InsertCompositionWebInfo(ctx, composition, orderDataID);

            }
            else
            {
                order = GetByID(composition.OrderID);
                // Add Or Update DynDb
                AddOrUpdateCompositionData(ctx, composition, order);
                // Update PrintDb
                AddOrUpdateCompositionWebInfo(ctx, composition, order);

            }

        }
        #endregion Register Composition Manager


        #region Register Composition PrintJobs
        private void AddOrUpdateCompositionWebInfo(PrintDB ctx, CompositionDefinition composition, IOrder order)
        {
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();
            var groupRepo = factory.GetInstance<IOrderGroupRepository>();
            var groupInfo = groupRepo.GetBillingInfo(ctx, order.OrderGroupID);

            //IPrintJob x; WARNING, this Interface is only for ZEBRA
            IPrinterJob pj;
            IPrinterJobDetail pjd;

            if(composition.ProductDataID > 1)
            {

                pj = ctx.PrinterJobs.First(w => w.CompanyOrderID.Equals(order.ID));

                pjd = ctx.PrinterJobDetails.First(w => w.ProductDataID.Equals(composition.ProductDataID) && w.PrinterJobID.Equals(pj.ID));
               

                pjd.Quantity = composition.Quantity;
                ctx.SaveChanges();

            }
            else
            {
                // order always is created with almost one printjob - for PrintWeb 1:1 relationship
                pj = ctx.PrinterJobs.FirstOrDefault(w => w.CompanyOrderID == order.ID);

                if(pj == null)
                {
                    CreatePrintJobWithDetail(ctx, composition, order);
                }
                else
                {
                    CreatePrinterJobDetail(ctx, composition, pj);
                }

            }

            UpdateOrderQuantities(ctx, order);
        }

        private void AddOrUpdateCompositionWebInfo_BAD_remove_after_new_version_already_created(PrintDB ctx, CompositionDefinition composition, IOrder order)
        {
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();
            var groupRepo = factory.GetInstance<IOrderGroupRepository>();
            var groupInfo = groupRepo.GetBillingInfo(ctx, order.OrderGroupID);

            // this method for compo, always return only one PrintJob
            var printerJobs = ctx.PrinterJobs.Where(w => w.CompanyOrderID.Equals(order.ID)).ToList();
            var found = false;
            foreach(var pj in printerJobs)
            {
                var pjds = ctx.PrinterJobDetails.Where(w => w.PrinterJobID.Equals(pj.ID) && w.ProductDataID.Equals(composition.ProductDataID)).ToList();
                if(pjds.Count > 0)
                {
                    found = true;
                    ctx.RemoveRange(pjds);

                    ctx.SaveChanges();// flush

                    CreatePrinterJobDetail(ctx, composition, pj);

                }
            }

            if(!found)
            {// add new print job detail
                CreatePrinterJobDetail(ctx, composition, printerJobs.First());
            }

            // update order quantity
            printerJobs = ctx.PrinterJobs.Where(w => w.CompanyOrderID.Equals(order.ID)).ToList();
            printerJobs.ForEach(j =>
            {
                var jobDetails = ctx.PrinterJobDetails.Where(w => w.PrinterJobID.Equals(j.ID)).GroupBy(g => g.PrinterJobID)
                .ToList();

                foreach(var grp in jobDetails)
                {
                    j.Quantity = grp.Sum(s => s.Quantity);
                }
            });

            foreach(var grp in printerJobs.GroupBy(g => g.CompanyOrderID))
            {
                order.Quantity = grp.Sum(s => s.Quantity);
            }

            ctx.SaveChanges();

        }
        private IOrder InsertCompositionWebInfo(PrintDB ctx, CompositionDefinition composition, int orderDataID)
        {

            var groupRepo = factory.GetInstance<IOrderGroupRepository>();
            var groupInfo = groupRepo.GetBillingInfo(ctx, composition.OrderGroupID);

            IOrder order = CreateCustomPartialOrder(ctx, groupInfo, composition.Quantity, orderDataID, true);

            CreatePrintJobWithDetail(ctx, composition, order);

            return order;
        }

        private void CreatePrintJobWithDetail(PrintDB ctx, CompositionDefinition composition, IOrder order)
        {
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();
            //var groupRepo = factory.GetInstance<IOrderGroupRepository>();
            //var groupInfo = groupRepo.GetBillingInfo(ctx, composition.OrderGroupID);
            var article = ctx.Articles.First(f => f.ID.Equals(composition.ArticleID) & f.ProjectID.Equals(order.ProjectID));

            IPrinterJob jobData = new PrinterJob()
            {
                CompanyID = order.CompanyID,
                CompanyOrderID = order.ID,
                ProjectID = order.ProjectID,
                ProductionLocationID = null,
                AssignedPrinter = null,
                ArticleID = article.ID,
                Quantity = composition.Quantity,
                Printed = 0,
                Errors = 0,
                Extras = 0,
                DueDate = DateTime.Now.AddDays(7),
                Status = JobStatus.Pending,
                AutoStart = false,
                CreatedDate = DateTime.Now,
                CompletedDate = null
            };

            var inserted = pjRepo.AddExtraJob(ctx, jobData);

            CreatePrinterJobDetail(ctx, composition, inserted);

        }

        private void CreatePrinterJobDetail(PrintDB ctx, CompositionDefinition composition, IPrinterJob job)
        {
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();

            var jobDetailData = new PrinterJobDetail()
            {
                PrinterJobID = job.ID,
                ProductDataID = composition.ProductDataID,
                Quantity = composition.Quantity,
                Extras = 0,
                PackCode = null,
                UpdatedDate = DateTime.Now
            };

            var detailInserted = pjRepo.AddExtraDetailToJob(ctx, jobDetailData);
        }

        private void UpdateOrderQuantities(PrintDB ctx, IOrder order)
        {
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();
            pjRepo.RefreshOrderQuantityValue(ctx, new List<int>() { order.ID });
        }

        #endregion Register Composition PrintJobs 

        #region Register Composition Data 

        private void AddOrUpdateCompositionData(PrintDB ctx, CompositionDefinition composition, IOrder order)
        {
            var catalogs = GetCompositionCatalogsForProject(ctx, order.ProjectID);
            var orderCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));

            // row already exist in DB
            if(composition.ProductDataID >= 1)
            {
                //var pjd = ctx.PrinterJobDetails.First(w => w.ProductDataID.Equals(composition.ProductDataID));
                //var pj = ctx.PrinterJobs.First(w => w.ID.Equals(pjd.PrinterJobID));

                //pjd.Quantity = composition.Quantity;
                ////las cantidades en el printjob y la orden se estan actualizando de forma incorrecta
                ///// esta orden puede tener varios details y esto se esta actualizando en otro lugar
                //pj.Quantity = composition.Quantity; //-> esto esta mal
                //order.Quantity = composition.Quantity;//-> esto esta mal
                //// this way disable events notifications for entities
                //ctx.SaveChanges();

                var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                var compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

                // update a existing composition details
                using(var dynamicDB = connManager.CreateDynamicDB())
                {

                    var articleEntity = ctx.Articles.First(f => f.ID.Equals(composition.ArticleID) & f.ProjectID.Equals(order.ProjectID));

                    var detail = dynamicDB.SelectOne(detailCatalog.CatalogID, composition.ProductDataID);
                    //detail["Quantity"] = composition.Quantity; // XXX: quanity not depend from Composition object, remove this line
                    //detail["ArticleCode"] = Article.EMPTY_ARTICLE_CODE; 
                    //dynamicDB.Update(detailCatalog.CatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(detail));
                    composition.ArticleCode = articleEntity.ArticleCode;

                    var product = dynamicDB.SelectOne(variableDataCatalog.CatalogID, detail.GetValue<int>("Product"));

                    // update or create compo record
                    if(product.GetValue<int>("HasComposition") > 0)
                    {

                        if(composition.ID != 0 && composition.ID != product.GetValue<int>("HasComposition"))
                            throw new Exception($"Composition Error - Data miss match. CompositionID => [{composition.ID}] != Product.HasComposition => {product.GetValue<int>("HasComposition")}");

                        composition.ID = product.GetValue<int>("HasComposition"); // composition can be selecte by productDataID, for this case, composition.ID somethimes is 0

                        var label = dynamicDB.SelectOne(compositionLabelCatalog.CatalogID, product.GetValue<int>("HasComposition"));
                        label["Type"] = composition.Type;


                        label["TargetArticle"] = composition.TargetArticle;
                        label["EnableComposition"] = composition.EnableComposition;
                        label["EnableWashingRulesSection"] = composition.EnableWashingRulesSection;
                        label["EnableExceptions"] = composition.EnableExceptions;
                        label["WrTemplate"] = composition.WrTemplate;
                       // label["ExceptionsLocation"] = composition.ExceptionsLocation;

                        dynamicDB.Update(compositionLabelCatalog.CatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(label));

                        // Clean all 
                        RemoveSections(dynamicDB, catalogs, composition);
                        RemoveCareInstructions(dynamicDB, catalogs, composition);

                        // add user composition
                        //AddCareInstructions(dynamicDB, catalogs, composition);
                        //AddSections(dynamicDB, catalogs, composition);
                    }
                    else
                    {

                        CreateCompositionRecord(dynamicDB, catalogs, composition);
                        // add user composition
                        //AddCareInstructions(dynamicDB, catalogs, composition);
                        //AddSections(dynamicDB, catalogs, composition);
                        product["HasComposition"] = composition.ID;

                        dynamicDB.Update(variableDataCatalog.CatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(product));

                    }

                    // add user composition
                    AddCareInstructions(dynamicDB, catalogs, composition);
                    AddSections(dynamicDB, catalogs, composition);




                }
            }
            else
            {

                //for this way never happen, the order data must be already created
                throw new Exception("Invalid Order Data");

                //// from this way, is created all Order Variable Data in follow tables: Order, OrderDetail, VariableData CompositionLabel
                //var orderFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(orderCatalog.Definition);
                //var orderDetailSetFieldID = orderFields.FirstOrDefault(x => x.Name == "Details").FieldID;
                //// add compomsition data to the current orderdata
                //// TODO: this methods  maybe can share dynamicDB connection
                //int detailID = CreateCompositionDetailData(ctx, catalogs, composition, order.ProjectID);
                //composition.ProductDataID = detailID;

                //using (var dynamicDB = connManager.CreateDynamicDB())
                //{
                //    // register relation between order  and details in PrintData Database
                //    dynamicDB.InsertRel(orderCatalog.CatalogID, detailCatalog.CatalogID, orderDetailSetFieldID, order.OrderDataID, detailID);
                //}
            }
        }

        //public int InsertCompositionData(PrintDB ctx, CompositionDefinition composition)
        //{
        //    var pjRepo = factory.GetInstance<IPrinterJobRepository>();
        //    var groupRepo = factory.GetInstance<IOrderGroupRepository>();
        //    var companyRepo = factory.GetInstance<ICompanyRepository>();

        //    var groupInfo = groupRepo.GetBillingInfo(ctx, composition.OrderGroupID);
        //    var sendTo = companyRepo.GetByID(ctx, groupInfo.SendToCompanyID);
        //    var billto = companyRepo.GetByID(ctx, groupInfo.BillToCompanyID);
        //    var catalogs = GetCompositionCatalogsForProject(ctx, groupInfo.ProjectID);

        //    var orderCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
        //    var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));

        //    var orderFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(orderCatalog.Definition);
        //    var orderDetailSetFieldID = orderFields.FirstOrDefault(x => x.Name == "Details").FieldID;

        //    //var articleEntity = ctx.Articles.First(f => f.ID.Equals(composition.ArticleID));

        //    int orderDataID = 0;

        //    // TODO: this methods  maybe can share dynamicDB connection
        //    int detailID = CreateCompositionDetailData(ctx, catalogs, composition, groupInfo.ProjectID);

        //    using (var dynamicDB = connManager.CreateDynamicDB())
        //    {

        //        dynamic orderData = new JObject();
        //        orderData.OrderNumber = groupInfo.OrderNumber;
        //        orderData.OrderDate = DateTime.Now;
        //        orderData.BillTo = billto.CompanyCode;
        //        orderData.SendTo = sendTo.CompanyCode;

        //        orderDataID = dynamicDB.Insert(orderCatalog.CatalogID, (JObject)orderData);

        //        // register relation between order  and details in PrintData Database
        //        dynamicDB.InsertRel(orderCatalog.CatalogID, detailCatalog.CatalogID, orderDetailSetFieldID, orderDataID, detailID);
        //    }

        //    return orderDataID;
        //}

        /// <summary>
        /// Every composition definition required a row inner this tables
        /// Create  a row into: 
        /// - ORDERDETAILS_CATALOG
        /// - VARIABLEDATA_CATALOG
        /// - COMPOSITIONLABEL_CATALOG
        /// - USER_CAREINSTRUCTIONS_CATALOG
        /// - USER_SECTIONS_CATALOG
        /// - USER_FIBERS_CATALOG
        /// </summary>
        //private int CreateCompositionDetailData(PrintDB ctx, IList<ICatalog> catalogs, CompositionDefinition composition, int projectID)
        //{
        //    var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
        //    var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
        //    var compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

        //    var compositionFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(compositionLabelCatalog.Definition);

        //    var articleEntity = ctx.Articles.First(f => f.ID.Equals(composition.ArticleID) & f.ProjectID.Equals(projectID));

        //    using (var dynamicDB = connManager.CreateDynamicDB())
        //    {
        //        CreateCompositionRecord(dynamicDB, catalogs, composition);

        //        dynamic product = new JObject();
        //        product.Barcode = "-";
        //        product.TXT1 = articleEntity.Description;
        //        product.TXT2 = "";
        //        product.TXT3 = "";
        //        product.Size = "";
        //        product.Color = composition.KeyValue; // Hardcode Color
        //        product.Price = "";
        //        product.Currency = "";
        //        product.HasComposition = composition.ID;

        //        var productID = dynamicDB.Insert(variableDataCatalog.CatalogID, (JObject)product);

        //        dynamic detail = new JObject();
        //        detail.ArticleCode = Article.EMPTY_ARTICLE_CODE;//articleEntity.ArticleCode;
        //        detail.Quantity = composition.Quantity;
        //        detail.Product = productID;

        //        var detailID = dynamicDB.Insert(detailCatalog.CatalogID, (JObject)detail);
        //        composition.ProductDataID = detailID;

        //        AddCareInstructions(dynamicDB, catalogs, composition);

        //        AddSections(dynamicDB, catalogs, composition);

        //        return detailID;
        //    }
        //}

        // Only create Compostion Record for existing VariableData Record
        // TODO: this code is repeated multiple times
        private void CreateCompositionRecord(DynamicDB dynamicDB, IList<ICatalog> catalogs, CompositionDefinition composition)
        {
            var compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

            dynamic compoLabelData = new JObject();
            compoLabelData.Type = composition.Type;
            compoLabelData.TargetArticle = composition.TargetArticle;
            compoLabelData.EnableCompositionSection = composition.EnableComposition;
            compoLabelData.EnableWashingRulesSection = composition.EnableWashingRulesSection;
            compoLabelData.EnableExceptions = composition.EnableExceptions;
            compoLabelData.FullComposition = null;
            compoLabelData.FullCareInstrunctions = null;
            compoLabelData.WrTemplate = composition.WrTemplate;

            var compoID = dynamicDB.Insert(compositionLabelCatalog.CatalogID, (JObject)compoLabelData);
            composition.ID = compoID;
        }


        private void AddCareInstructions(DynamicDB dynamicDB, IList<ICatalog> catalogs, CompositionDefinition composition)
        {
            if(composition.CareInstructions == null) return;

            var compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

            var uciCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG));

            var compositionFields = compositionLabelCatalog.Fields;

            var careInstruction = compositionFields.FirstOrDefault(x => x.Name == Catalog.BRAND_CAREINSTRUCTIONS_CATALOG);

            var careInstructionsSetFieldID = compositionFields.FirstOrDefault(x => x.Name == Catalog.BRAND_CAREINSTRUCTIONS_CATALOG).FieldID;

            var validCareInstructions = composition.CareInstructions.Where(w => w.Instruction > 0).ToList();

            for(int i = 0; composition.CareInstructions != null && i < validCareInstructions.Count; i++)
            {
                var el = validCareInstructions.ElementAt(i);

                dynamic newUci = new JObject();
                newUci.Position = i + 1;
                newUci.Instruction = el.Instruction; // must be id from brand care instructions catalog

                var userInstructionID = dynamicDB.Insert(uciCatalog.CatalogID, newUci);
                dynamicDB.InsertRel(compositionLabelCatalog.CatalogID, uciCatalog.CatalogID, careInstructionsSetFieldID, composition.ID, userInstructionID);

                composition.CareInstructions.ElementAt(i).ID = userInstructionID;

            }
        }

        private void AddSections(DynamicDB dynamicDB, IList<ICatalog> catalogs, CompositionDefinition composition)
        {
            var compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            var usCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_SECTIONS_CATALOG));
            var ufCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_FIBERS_CATALOG));
            var compositionFields = compositionLabelCatalog.Fields;
            var sectionsFields = usCatalog.Fields;
            var sectionsSetFieldID = compositionFields.FirstOrDefault(x => x.Name == Catalog.BRAND_SECTIONS_CATALOG).FieldID;
            var fibersSetFieldID = sectionsFields.FirstOrDefault(x => x.Name == Catalog.BRAND_FIBERS_CATALOG).FieldID;

            // sections and fibers
            // sort sections in plugin before concat
            for(int i = 0; composition.Sections != null && i < composition.Sections.Count; i++)
            {
                var sectionData = composition.Sections.OrderByDescending(o => o.Percentage).ElementAt(i);
                // add section
                dynamic userSection = new JObject(); // JObject.FromObject(sectionData);
                userSection.Position = i;
                userSection.Percentage = sectionData.Percentage;
                userSection.Section = sectionData.SectionID; // must be id from brad sections catalog
                userSection.IsBlank = sectionData.IsBlank;
                userSection.IsMainTitle = sectionData.IsMainTitle;

                var sectionID = dynamicDB.Insert(usCatalog.CatalogID, (JObject)userSection);

                // add compo section rel
                dynamicDB.InsertRel(compositionLabelCatalog.CatalogID, usCatalog.CatalogID, sectionsSetFieldID, composition.ID, sectionID);

                sectionData.ID = sectionID;

                for(int j = 0; sectionData.Fibers != null && j < sectionData.Fibers.Count; j++)
                {
                    var fiberData = sectionData.Fibers.OrderByDescending(o => int.Parse(o.Percentage)).ElementAt(j);
                    if(fiberData.FiberID < 1) continue;// avoid to add empty fibers
                    // add fibers
                    dynamic userFiber = new JObject();
                    userFiber.Position = j;
                    userFiber.Percentage = fiberData.Percentage;
                    userFiber.CountrOfOrigin = fiberData.CountryOfOrigin;
                    userFiber.FiberType = fiberData.FiberType;
                    userFiber.FiberIcon = fiberData.FiberIcon;
                    userFiber.Fiber = fiberData.FiberID; // must be id from brad fibers catalog
                    //userFiber.Category = fiberData.C

                    var fiberID = dynamicDB.Insert(ufCatalog.CatalogID, (JObject)userFiber);
                    fiberData.ID = fiberID; // user fiber ID
                    // add section fiber rel
                    dynamicDB.InsertRel(usCatalog.CatalogID, ufCatalog.CatalogID, fibersSetFieldID, sectionID, fiberID);


                }
            }
        }

        // Sections Remove bulk action
        private void RemoveSections(DynamicDB dynamicDB, IList<ICatalog> catalogs, CompositionDefinition composition)
        {

            var clCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            var usCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_SECTIONS_CATALOG));
            var ufCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_FIBERS_CATALOG));

            var allSectionsIds = new List<int>();
            var allFibersIds = new List<int>();

            var sectionSetField = clCatalog.Fields.FirstOrDefault(f => f.Name.Equals("Sections"));
            var fiberSetField = usCatalog.Fields.FirstOrDefault(f => f.Name.Equals("Fibers"));

            var RelCompositionSectionName = $"REL_{clCatalog.CatalogID}_{usCatalog.CatalogID}_{sectionSetField.FieldID}";
            var RelSectionFiberName = $"REL_{usCatalog.CatalogID}_{ufCatalog.CatalogID}_{fiberSetField.FieldID}";

            var currentSections = dynamicDB.Conn.SelectToJson($@"
            SELECT r.TargetID
            FROM {RelCompositionSectionName} r
            WHERE r.SourceID = @id", composition.ID);

            foreach(JObject sectionFound in currentSections)
                allSectionsIds.Add(sectionFound.GetValue<int>("TargetID"));

            if(allSectionsIds.Count > 0)
            {
                var currentFibers = dynamicDB.Conn.SelectToJson($@"
                SELECT r.TargetID
                FROM {RelSectionFiberName} r
                WHERE r.SourceID IN ({string.Join(',', allSectionsIds)})");

                foreach(JObject sectionFound in currentFibers)
                    allFibersIds.Add(sectionFound.GetValue<int>("TargetID"));
            }
            try
            {
                // delete relations first
                dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {RelCompositionSectionName} WHERE SourceID = @id", composition.ID);

                if(allSectionsIds.Count > 0)
                {
                    // delete relation first
                    dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {RelSectionFiberName}  WHERE SourceID IN ({string.Join(',', allSectionsIds)})");
                    // delete sections and fibers rows
                    dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {usCatalog.TableName} WHERE ID IN ({string.Join(',', allSectionsIds)})");

                    if(allFibersIds.Count > 0)
                        dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {ufCatalog.TableName} WHERE ID IN ({string.Join(',', allFibersIds)})");
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private void RemoveCareInstructions(DynamicDB dynamicDB, IList<ICatalog> catalogs, CompositionDefinition composition)
        {
            var clCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            var ciCatalog = catalogs.First(f => f.Name.Equals(Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG));
            var allCiIds = new List<int>();

            var careInstructionsSetField = clCatalog.Fields.FirstOrDefault(f => f.Name.Equals("CareInstructions"));

            var RelCompositionCareInstructionsName = $"REL_{clCatalog.CatalogID}_{ciCatalog.CatalogID}_{careInstructionsSetField.FieldID}";

            var currentCareInstructions = dynamicDB.Conn.SelectToJson($@"
            SELECT r.TargetID
            FROM {RelCompositionCareInstructionsName} r
            WHERE r.SourceID = @id", composition.ID);

            foreach(JObject sectionFound in currentCareInstructions)
                allCiIds.Add(sectionFound.GetValue<int>("TargetID"));

            dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {RelCompositionCareInstructionsName} WHERE SourceID = @id", composition.ID);

            if(allCiIds.Count > 0)
                dynamicDB.Conn.ExecuteNonQuery($"DELETE FROM {ciCatalog.TableName} WHERE ID IN ({string.Join(',', allCiIds)})");

        }
        #endregion Register Composition Data 

        #region Utils

        public IList<ICatalog> GetCompositionCatalogsForProject(int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCompositionCatalogsForProject(ctx, projectID);
            }
        }

        public IList<ICatalog> GetCompositionCatalogsForProject(PrintDB ctx, int projectID)
        {
            var catalogsNames = Catalog.GetAllCompoCatalogNames;
            var catalogs = ctx.Catalogs
               .Where(w => w.ProjectID.Equals(projectID))
               .Where(w => catalogsNames.Contains(w.Name))
               .ToList<ICatalog>();

            return catalogs;
        }

        public string GetLanguageQuery(IEnumerable<FieldDefinition> fields, bool joinLang, IEnumerable<string> selectedLanguages, string langSeparator)
        {

            var sortedFields = new List<FieldDefinition>();

            selectedLanguages.ToList().ForEach(f =>
            {
                var toInclude = fields.FirstOrDefault(w => w.Name.ToLower() == f.ToLower());
                if(toInclude != null)
                {
                    sortedFields.Add(toInclude);
                }
            });

            if(sortedFields.Count == 0)
            {
                sortedFields = fields.ToList(); // add all fields if not found selectedLanguages
            }

            // concat method required multiple fields
            if(sortedFields.Count < 2)
            {
                return $" {sortedFields.First().Name} AS AllLangs";
            }

            return $"CONCAT({string.Join($"+'{langSeparator}',", sortedFields.Select(s => s.Name))}) AS AllLangs";

        }
        #endregion Utils
    }
}

