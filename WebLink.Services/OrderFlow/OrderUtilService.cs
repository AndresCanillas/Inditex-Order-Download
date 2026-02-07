using LinqKit;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Platform.PoolFiles;

namespace WebLink.Services
{

    public class CompositionOrderDTO
    {
        public int OrderGroupID { get; set; }
        public OrderDetailDTO Order { get; set; }
        public List<CompositionDefinition> UserComposition { get; set; }
    }

    public static class CompositionCatalogs
    {
        public const string PARTS = "CompositionParts";
        public const string FIBERS = "CompositionFibers";
    }

    public class OrderUtilService : IOrderUtilService
    {
        private ILogService log;
        private IOrderGroupRepository groupRepo;
        private IOrderLogService orderLogService;
        private IOrderRepository orderRepo;
        private ICatalogRepository catalogRepo;
        private ICatalogDataRepository catalogDataRepo;
        private IProjectRepository projectRepo;
        private readonly IFactory factory;
        public static string LANG_SEPARATOR = "__S__";


        public OrderUtilService(
            ILogService log,
            IOrderGroupRepository groupRepo,
            IOrderLogService orderLogService,
            IOrderRepository orderRepo,
            ICatalogRepository catalogRepo,
            ICatalogDataRepository catalogDataRepo,
            IProjectRepository projectRepo,
            IFactory factory
            )
        {
            this.log = log;
            this.groupRepo = groupRepo;
            this.orderLogService = orderLogService;
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
            this.catalogDataRepo = catalogDataRepo;
            this.projectRepo = projectRepo;
            this.factory = factory;
        }

        // no agrupar los detalles por color
        public IEnumerable<OrderGroupSelectionDTO> CurrentOrderedLablesGroupBySelectionV2(IEnumerable<OrderGroupSelectionDTO> selection)
        {
            var keyName = "Color";// and size

            // obtener todos los articulos(incluido los cancelados) de cada grupo seleccionado
            // for all companyorders inner group, clean Orders
            //var cloneSelection = new List<OrderGroupSelectionDTO>();

            //selection.ToList().ForEach(sel =>
            //{
            //    var cln = new OrderGroupSelectionDTO(sel);
            //    cln.Orders = (new List<int>()).ToArray();
            //    cloneSelection.Add(cln);
            //});


            //obtiene todos los detalles de las ordenes aun si estan cancelados pero sin numero de orden
            var ordersArticleDetails = orderRepo.GetArticleDetailSelection(selection, new OrderArticlesFilter()
            {
                ArticleType = ArticleTypeFilter.All,
                ActiveFilter = OrderActiveFilter.NoRejected,
                Source = OrderSourceFilter.NotSet,
                OrderStatus = OrderStatusFilter.All
            });



            foreach(var selectionGroup in ordersArticleDetails)
            {
                //obtiene el detalle de la orden en particular y de un solo articulo dentro del grupo actual
                var detailsOrder = GetDetailsForThisOrder(selectionGroup);

                var newDetails = new List<OrderDetailWithCompoDTO>();

                //if(detailsOrder.Count() < 1)
                //{
                //    continue; // next group;
                //}

                //obtiene las compo definidas por el usuario por medio del ordergroupid
                var compoDefinedByUser = orderRepo.GetUserCompositionForGroup(selectionGroup.OrderGroupID);



                // map details to OrderDetailWithCompoDTO
                foreach(var dt in detailsOrder)
                {
                    // every detail has his owned composition defined  (obtiene la compo si existe a traves del color y el productid)
                    var compoFound = compoDefinedByUser.Where(w => w.ProductDataID == dt.ProductDataID).FirstOrDefault();

                    if(compoFound == null)
                    {
                        // create a default object to avoid check nulls on javascript interface
                        compoFound = new CompositionDefinition()
                        {
                            OrderGroupID = selectionGroup.OrderGroupID,
                            Sections = new List<Section>(),
                            CareInstructions = new List<CareInstruction>(),
                            KeyName = keyName,
                            KeyValue = dt.Color,
                            OrderID = dt.OrderID,
                            ProductDataID = dt.ProductDataID,// the current detail belong to the composition order
                            OrderDataID = 0,// ?????, if not used -> removed property
                            Product = 0
                        };
                    }

                    //add ArticleInfo
                    compoFound.ArticleCode = dt.ArticleCode;
                    compoFound.ArticleID = dt.ArticleID;



                    var mappingDetail = new OrderDetailWithCompoDTO(dt);
                    mappingDetail.Composition = compoFound;
                    mappingDetail.KeyName = keyName;
                    mappingDetail.KeyValue = dt.Color;

                    newDetails.Add(mappingDetail); //son los mismos detalles pero con composicion
                }

                //var detailsGroup = details.OrderBy(o1 => o1.ArticleID).ThenBy(o2 => o2.Color).GroupBy(g1 => new { g1.OrderGroupID, g1.OrderID, g1.Color });

                selectionGroup.Details = newDetails.OrderBy(o => o.ProductDataID).ToList<OrderDetailDTO>();

                /*
                selectionGroup.Details = new List<OrderDetailDTO>();
                var groupNewDetails = newDetails.GroupBy(g => new { g.Color, g.OrderGroupID });
                var mappingDetailGrouped = new List<OrderDetailDTO>();

                foreach (var nd in groupNewDetails)
                {
                    var newdetailgroup = new OrderDetailWithCompoDTO(nd.First());
                    newdetailgroup.Quantity = nd.Sum(s => s.Quantity);
                    newdetailgroup.QuantityRequested = nd.Sum(s => s.QuantityRequested);
                    newdetailgroup.Size = string.Join(", ", nd.ToList().Select(s => s.Size).ToArray());
                    newdetailgroup.ProductDataID = 0;
                    mappingDetailGrouped.Add(newdetailgroup);
                }

                selectionGroup.Details = mappingDetailGrouped;*/

                // set all ordersIds
                // ????: maybe the best place to execute this action, is inner  IOrderRepository.GetArticleDetailSelection method
                selectionGroup.Orders = selectionGroup.Details
                    .Where(w => w.OrderStatus != OrderStatus.Cancelled)
                    .Select(s => s.OrderID)
                    .ToArray();
            }

            return ordersArticleDetails;
        }

        /// <summary>
        /// to create compo, only is required detail from one article, this article must contain sizes and color details
        /// - composition article
        /// - hantag article
        /// - initial order recived with default EMPTY_ARTICLE asigned
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        private IEnumerable<OrderDetailDTO> GetDetailsForThisOrder(OrderGroupSelectionDTO selection)
        {
            var ret = new List<OrderDetailDTO>();
            if(selection.Details.Count() < 1)
            {
                return ret;
            }

            // looking for articles with Composition Label, only one is enough
            var groupeOrders = selection.Details
                .Where(w => w.IncludeComposition == true)
                .OrderBy(o => o.OrderID)
                .GroupBy(g => new { g.OrderID, g.ArticleCode, g.OrderStatus, g.LabelType })
                .Select(group => new { group.Key.OrderID, group.Key.ArticleCode, group.Key.OrderStatus, group.Key.LabelType, Details = group.ToList() });

            // search not cancelled orders first
            groupeOrders.Where(w => w.OrderStatus != OrderStatus.Cancelled).ForEach(e => ret.AddRange(e.Details));

            // // first article inner the order must contain full details
            //if (ret == null)
            // {
            //     //throw new CompositionConfigurationException("Not found size/color details for this order");
            //     return new List<OrderDetailDTO>();

            // }

            return ret;
        }

        /// <summary>
        /// return gruped details  by color
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        [Obsolete("very confuse method,  use CurrentOrderedLablesGroupBySelectionV2")]
        public IEnumerable<OrderGroupSelectionDTO> CurrentOrderedLablesGroupBySelection(IEnumerable<OrderGroupSelectionDTO> selection)
        {

            var keyName = "Color";

            // for all group, clean Orders
            selection.ToList().ForEach(sel => sel.Orders = (new List<int>()).ToArray()); // remove orders id, to execute query by ordergroupid


            // only hangtags inner current order
            // only one detail 
            var allowedDetails = orderRepo.GetArticleDetailSelection(selection, new OrderArticlesFilter()
            {
                ArticleType = ArticleTypeFilter.HangTag,
                ActiveFilter = OrderActiveFilter.NoRejected,
                Source = OrderSourceFilter.NotSet
            });

            // for all selection groups, clean Orders property
            selection.ToList().ForEach(sel => sel.Orders = (new List<int>()).ToArray()); // remove orders id, to execute query by ordergroupid

            List<OrderGroupSelectionDTO> ordersCreatedLikeSelection = orderRepo.GetArticleDetailSelection(selection, new OrderArticlesFilter()
            {
                ArticleType = ArticleTypeFilter.CareLabel,
                ActiveFilter = OrderActiveFilter.NoRejected,
                Source = OrderSourceFilter.FromValidation

            });

            // join all created details
            var createdDetails = new List<OrderDetailDTO>();
            ordersCreatedLikeSelection.ForEach(e => createdDetails.AddRange(e.Details));



            #region stored compo
            //var OrdersCompositionLink = new Dictionary<>
            var userComposition = new List<CompositionDefinition>();
            var compositionOrdersFound = new List<CompositionOrderDTO>();

            // get details stored in print_data database, only can existe one composition article by group
            foreach(var sel in ordersCreatedLikeSelection)
            {


                // every group only can have o one composition labels, whit multiple details for every color/size combination
                var uc = orderRepo.GetUserCompositionForGroup(sel.OrderGroupID);
                userComposition.AddRange(uc);


                if(sel.Details.FirstOrDefault() != null)
                {
                    compositionOrdersFound.Add(new CompositionOrderDTO()
                    {
                        OrderGroupID = sel.OrderGroupID,
                        Order = sel.Details.First(),
                        UserComposition = uc.ToList()
                    });
                }

            }
            #endregion stored compo

            //allowedDetails.AddRange(ordersCreated);

            // grouping order details - by articles and color -> 20210630 only for color
            // TODO: group fields will be dynamic
            allowedDetails.ForEach(sel =>
            {
                // TODO: how to custom field order
                var allGroups = sel.Details.OrderBy(o1 => o1.ArticleID).ThenBy(o2 => o2.Color).GroupBy(g1 => new { g1.OrderGroupID, g1.OrderID, g1.Color });

                var newDetails = new List<OrderDetailDTO>();

                foreach(var grp in allGroups)
                {
                    // exist order for this group, using the same keys, how to 

                    var orderFound = compositionOrdersFound.Where(w => w.Order.OrderID.Equals(grp.Key.OrderID)).FirstOrDefault();
                    CompositionDefinition compoOrderN = null;

                    if(orderFound != null)
                        compoOrderN = orderFound.UserComposition
                        .Where(w => w.KeyValue.Equals(grp.Key.Color))
                        .Where(w => w.OrderGroupID.Equals(sel.OrderGroupID))
                        .Where(w => w.OrderID.Equals(grp.Key.OrderID))
                        .FirstOrDefault();

                    var createdGrupedDetails = createdDetails.GroupBy(g2 => new { g2.OrderGroupID, g2.OrderID, g2.Color });

                    var compoOrder = userComposition.Where(w => w.KeyValue.Equals(grp.Key.Color) && w.OrderGroupID.Equals(sel.OrderGroupID)).FirstOrDefault();

                    var orderCreatedWithCompo = createdDetails.FirstOrDefault(w => w.Color.Equals(grp.Key.Color));

                    var currentCreatedCompoGroup = createdGrupedDetails
                    .Where(w => w.Key.OrderGroupID.Equals(grp.Key.OrderGroupID))
                    .Where(w => w.Key.Color.Equals(grp.Key.Color))
                    .FirstOrDefault();


                    if(compoOrder == null)
                    {
                        // other details are created, try to use the same CompanyOrder To Attach other color details for all colors
                        var otherColorComposition = userComposition.FirstOrDefault(w => w.OrderGroupID.Equals(sel.OrderGroupID));
                        IOrder orderRow = null;

                        if(otherColorComposition != null)
                        {
                            orderRepo.GetByID(orderCreatedWithCompo.OrderID);
                        }

                        compoOrder = new CompositionDefinition()
                        {
                            OrderGroupID = grp.Key.OrderGroupID,
                            Sections = new List<Section>(),
                            CareInstructions = new List<CareInstruction>(),
                            KeyName = keyName,
                            KeyValue = grp.Key.Color,
                            OrderID = orderCreatedWithCompo != null ? orderCreatedWithCompo.OrderID : 0,
                            OrderDataID = orderRow != null ? orderRow.OrderDataID : 0
                        };
                    }


                    //var product = grp.ToList().FirstOrDefault(f => f.ArticleID.Equals(grp.Key.ArticleID));

                    newDetails.Add(new OrderDetailWithCompoDTO()
                    {
                        ArticleID = orderCreatedWithCompo != null ? orderCreatedWithCompo.ArticleID : 0,
                        ArticleCode = orderCreatedWithCompo != null ? orderCreatedWithCompo.ArticleCode : string.Empty,// product.ArticleCode,
                        Article = orderCreatedWithCompo != null ? orderCreatedWithCompo.Article : string.Empty,// product.Article,
                        Color = grp.Key.Color, // customizable field
                        KeyName = keyName,
                        KeyValue = grp.Key.Color,
                        Size = string.Join(", ", grp.ToList().Select(s => s.Size).ToArray()), // customizable field
                        QuantityRequested = grp.Sum(s => s.QuantityRequested), // suggested | default value is 0
                        Quantity = currentCreatedCompoGroup != null ? currentCreatedCompoGroup.Sum(s => s.Quantity) : grp.Sum(s => s.Quantity), // user confirmed quantity for other labels
                        OrderID = compoOrder != null ? compoOrder.OrderID : 0, // added
                        ProductDataID = compoOrder != null ? compoOrder.ProductDataID : 0,
                        Composition = compoOrder
                    });
                }

                sel.Details = newDetails;

            });

            return allowedDetails;
        }

        public CompositionDefinition GetCompositionDetailsForOrder(int orderID)
        {
            var order = orderRepo.GetByID(orderID);
            var allCreatedCompoForThisGroup = orderRepo.GetUserCompositionForGroup(order.OrderGroupID);

            return allCreatedCompoForThisGroup.FirstOrDefault(w => w.OrderID.Equals(orderID));
        }

        public object GetCompositionCatalogBySelection(IEnumerable<OrderGroupSelectionDTO> selection)
        {
            // get project identifier for every selection
            Dictionary<int, IList<ICatalog>> catalogs = new Dictionary<int, IList<ICatalog>>();
            Dictionary<int, string> data = new Dictionary<int, string>();

            foreach(var sel in selection)
            {
                var groupInfo = groupRepo.GetProjectInfo(sel.OrderGroupID);
                sel.ProjectID = groupInfo.ProjectID;

                var parts = catalogRepo.GetByName(groupInfo.ProjectID, CompositionCatalogs.PARTS);
                var fibers = catalogRepo.GetByName(groupInfo.ProjectID, CompositionCatalogs.FIBERS);
                catalogs[sel.OrderGroupID] = new List<ICatalog>();
                catalogs[sel.OrderGroupID].Add(parts);
                catalogs[sel.OrderGroupID].Add(fibers);
                data[parts.ID] = catalogDataRepo.GetList(parts.CatalogID);
                data[parts.ID] = catalogDataRepo.GetList(parts.CatalogID);
            }

            return new { CatalogsBySelection = catalogs, DataByCatalog = data };

        }

        public IList<CompositionDefinition> GetComposition(int orderGroupID, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSeparator = ",")
        {
            return orderRepo.GetUserCompositionForGroup(orderGroupID, joinLang, languages, langSeparator);
        }

        public void SaveComposition(int projectId, int rowId, string composition, string careInstructions, string symbols = null)
        {
            orderRepo.SaveComposition(projectId, rowId, composition, careInstructions, symbols);
        }

        public void SaveComposition(int projectId, int rowId, Dictionary<string, string> composition, string careInstructions, string symbols)
        {
            orderRepo.SaveComposition(projectId, rowId, composition, careInstructions, symbols);
        }

        public IProject GetProjectById(int projectId)
        {
            return projectRepo.GetByID(projectId);
        }

        public CompositionDefinition SaveCompositionDefinition(CompositionDefinition composition)
        {
            orderRepo.AddCompositionOrder(composition);

            return composition;
        }

        public Dictionary<string, string> GetCompositionData(int projectId, int rowId)
        {
            return orderRepo.GetCompostionData(projectId, rowId);
        }

        #region OrderPool
        public async Task SendToPool(int projectID, string fileName, Stream fileContent)
        {
            Type handlerType;
            IPoolFileHandler handler;

            var project = projectRepo.GetByID(projectID);
            if(project == null)
                throw new PoolFileHandlerException("Project not found or user does not have access to it.");

            if(!project.EnablePoolFile)
                throw new PoolFileHandlerException("Project does not have the pool file option enabled.");

            handlerType = TypeExtensions.FindType(project.PoolFileHandler);
            if(!handlerType.Implements(typeof(IPoolFileHandler)))
                throw new PoolFileHandlerException("Could not instantiate pool file handler. Type does not implement IPoolFileHandler");

            handler = (IPoolFileHandler)factory.GetInstance(handlerType);

            await handler.UploadAsync(project, fileContent, SendEmailOrderPoolReceived);

        }

        public async Task SaveOrderPoolList(int projectID, List<OrderPool> orderPools)
        {
            Type handlerType;
            IPoolFileHandler handler;

            var project = projectRepo.GetByID(projectID);
            if(project == null)
                throw new PoolFileHandlerException("Project not found or user does not have access to it.");

            if(!project.EnablePoolFile)
                throw new PoolFileHandlerException("Project does not have the pool file option enabled.");

            handlerType = TypeExtensions.FindType(project.PoolFileHandler);
            if(!handlerType.Implements(typeof(IPoolFileHandler)))
                throw new PoolFileHandlerException("Could not instantiate pool file handler. Type does not implement IPoolFileHandler");

            handler = (IPoolFileHandler)factory.GetInstance(handlerType);
            await handler.InsertListAsync(project, orderPools); 

        }


        // REgister token for EmailSenderService
        public void SendEmailOrderPoolReceived(int projectID, IList<IOrderPool> ordersReceived)
        {
            var providerRepo = factory.GetInstance<IProviderRepository>();
            var companyRepo = factory.GetInstance<ICompanyRepository>();
            var brandRepo = factory.GetInstance<IBrandRepository>();
            var mailService = factory.GetInstance<IOrderEmailService>();

            var project = projectRepo.GetByID(projectID);
            var brand = brandRepo.GetByID(project.BrandID);

            ordersReceived.GroupBy(gb => gb.ProviderCode2).ForEach(providerData =>
            {

                var prv = providerRepo.GetProviderByClientReference(brand.CompanyID, providerData.Key);

                var recipients = companyRepo.GetContactEmails(prv.ProviderCompanyID);

                var orderNumbers = providerData.AsEnumerable().GroupBy(on => on.OrderNumber).Select(s => new
                {
                    OrderNumber = s.Key,
                    OrderID = s.AsEnumerable().First().ID
                });

                foreach(var usr in recipients)
                {
                    var token = mailService.GetTokenFromUser(usr, EmailType.OrderPoolUpdated);
                    if(token == null) continue;

                    orderNumbers.ForEach(s => mailService.AddOrderIfNotExists(token, s.OrderID));
                }

            });



        }


    }


    #endregion OrderPool



    

    //[Serializable]
    //public class CompositionConfigurationException : Exception
    //{
    //    public CompositionConfigurationException()
    //    {
    //    }

    //    public CompositionConfigurationException(string message) : base(message)
    //    {
    //    }

    //    public CompositionConfigurationException(string message, Exception innerException) : base(message, innerException)
    //    {
    //    }

    //    protected CompositionConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    //    {
    //    }
    //}
}
