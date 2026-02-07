using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IOrderRepository : IGenericRepository<IOrder>
    {
        void SetOrderIdImageFile(int orderid, Stream filedata);
        void SetOrderFile(int orderid, Guid fileGUID);
        void SetOrderFile(int orderid, Stream filedata);
        IFileData GetOrderFile(int orderid);
        void SetOrderAttachment(int orderid, string attachmentCategory, string filePath);
        IAttachmentData GetOrderAttachment(int orderid, string attachmentCategory, string attachmentName);

        List<IOrder> GetOrdersByStatus(OrderStatus status);
        List<IOrder> GetOrdersByStatus(PrintDB ctx, OrderStatus status);

        List<IOrder> GetOrdersWithoutMDConfirmation();
        List<IOrder> GetOrdersWithoutMDConfirmation(PrintDB ctx);

        void SetOrderMDConfirmation(int orderid);
        void SetOrderMDConfirmation(PrintDB ctx, int orderid);

        [Obsolete("Use GetOrderReportPage")]
        List<CompanyOrderDTO> GetOrderReport(OrderReportFilter filter);
        [Obsolete("Use GetOrderReportPage")]
        List<CompanyOrderDTO> GetOrderReport(PrintDB ctx, OrderReportFilter filter);

        IEnumerable<Order> GetOrdersByFilter(OrderFilter filter);
        IEnumerable<Order> GetOrdersByFilter(PrintDB ctx, OrderFilter filter);

        IEnumerable<Order> GetOrdersByLabelID(int projectId, int labelId, int count, string orderNumber);
        IEnumerable<Order> GetOrdersByLabelID(PrintDB ctx, int projectId, int labelId, int count, string orderNumber);

        OrderProductionDetail GetOrderProductionDetail(int orderid);
        OrderProductionDetail GetOrderProductionDetail(PrintDB ctx, int orderid);

        //OrderProductionDetail GetOrderProductionDetail(int orderDataID, string orderNumber, int projectID, int companyID);
        //OrderProductionDetail GetOrderProductionDetail(PrintDB ctx, int orderDataID, string orderNumber, int projectID, int companyID);

        OrderBillingDetail GetOrderBillingDetail(int orderid);
        OrderBillingDetail GetOrderBillingDetail(PrintDB ctx, int orderid);


        IEnumerable<Order> GetOrderAffectedByCatalogUpdate(int projectID);
        IEnumerable<Order> GetOrderAffectedByCatalogUpdate(PrintDB ctx, int projectID);

        int TotalOrdersAffectedByCatalogupdate(int projectID);
        int TotalOrdersAffectedByCatalogupdate(PrintDB ctx, int projectID);

        string PackMultiplePrintPackages(int factoryid, IEnumerable<int> orderids);

        Task<string> PackMultiplePreviewDocument(IEnumerable<int> orderids);

        string PackMultipleOrdersValidationPreview(IEnumerable<int> orderids);

        List<EncodedEntity> GetEncodedByOrder(int id);

        IOrderGroup GetOrCreateOrderGroup(string orderNumber, int projectid, int billToCompanyID, int sendToCompanyID, string erpReference, string clientCategory, int? ProviderRecordId);
        IOrderGroup GetOrCreateOrderGroup(PrintDB ctx, string orderNumber, int projectid, int billToCompanyID, int sendToCompanyID, string erpReference, string clientCategory, int? ProviderRecordId);

        bool ChangeDueDate(OrderDueDateDTO entity, IOrderRegisterInERP orderRegisterInERP);




        #region Clone, Repear, Copy Orders

        void Clone(int id, bool isBillable, string articleCode, int? providerID, string username, bool withSameData = false, DocumentSource repetition = DocumentSource.NotSet);
        IOrder Copy(int id, bool isBillable, string articleCode, int? providerID, string username, DocumentSource repetition = DocumentSource.NotSet);

        #endregion


        #region PartialActions

        IOrder ChangeStatus(int orderID, OrderStatus newStatus);
        IOrder ChangeStatus(PrintDB ctx, int orderID, OrderStatus newStatus);
        IOrder ChangeStatusWF(int orderID, OrderStatus newStatus, bool repeatTask);

        IWizardStep GetNextStep(int orderID);
        IWizardStep GetNextStep(PrintDB ctx, int orderID);

        IWizardStep GetBackStep(int orderID);
        IWizardStep GetBackStep(PrintDB ctx, int orderID);

        IWizardStep GetStep(int position, int orderID);
        IWizardStep GetStep(PrintDB ctx, int position, int orderID);

        void ResetStatusEvent(int orderID);
        void ResetStatusEvent(PrintDB ctx, int orderID);

        IWizardStep GetNextStepBySelection(List<OrderGroupSelectionDTO> selection);
        IWizardStep GetNextStepBySelection(PrintDB ctx, List<OrderGroupSelectionDTO> selection);

        IWizardStep GetStepBySelection(int position, List<OrderGroupSelectionDTO> selection);
        IWizardStep GetStepBySelection(PrintDB ctx, int position, List<OrderGroupSelectionDTO> selection);

        IEnumerable<IWizardStep> GetAllWizardSteps(List<OrderGroupSelectionDTO> selection);
        IEnumerable<IWizardStep> GetAllWizardSteps(PrintDB ctx, List<OrderGroupSelectionDTO> selection);

        void ChangeProductionType(int orderID, ProductionType productionType);
        void ChangeProductionType(PrintDB ctx, int orderID, ProductionType productionType);

        IEnumerable<IOrder> SetConflictStatusByShareData(bool IsInConflict, int orderID);
        IEnumerable<IOrder> SetConflictStatusByShareData(PrintDB ctx, bool IsInConflict, int orderID);
        #endregion


        #region PartialQueries

        IEnumerable<OrderToUpdateDTO> GetOrdersToUpdate(int orderGroupID, int orderID, string orderNumber, int projectID, ConflictMethod method, bool categorizeArticle, int? factoryID, int? providerRecordId, bool onlyActive = true);
        IEnumerable<OrderToUpdateDTO> GetOrdersToUpdate(PrintDB ctx, int orderGroupID, int orderID, string orderNumber, int projectID, ConflictMethod method, bool categorizeArticle, int? factoryID, int? providerRecordId, bool onlyActive = true);

        OrderToUpdateDTO GetConflictedOrderFor(int currentID);
        OrderToUpdateDTO GetConflictedOrderFor(PrintDB ctx, int currentID);

        IEnumerable<OrderDetailDTO> GetOrderArticles(OrderArticlesFilter filter, ProductDetails addProductDetails = ProductDetails.None, List<string> productFields = null);
        //IEnumerable<OrderDetailDTO> GetOrderArticles(OrderArticlesFilter filter, bool addProductDetails = false, List<string> productFields = null);
        IEnumerable<OrderDetailDTO> GetOrderArticles(PrintDB ctx, OrderArticlesFilter filter, ProductDetails addProductDetails = ProductDetails.None, List<string> productFields = null);
        IEnumerable<OrderDetailDTO> GetOrderArticles(PrintDB ctx, OrderArticlesFilter filter, bool addProductDetails = false, List<string> productFields = null);

        List<OrderGroupSelectionDTO> GetArticleDetailSelection(IEnumerable<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter, bool addProductDetail = false, List<string> productFields = null);
        List<OrderGroupSelectionDTO> GetArticleDetailSelection(PrintDB ctx, IEnumerable<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter, bool addProductDetail = false, List<string> productFields = null);

        List<OrderGroupSelectionDTO> GetItemsExtrasDetailSelection(List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter);
        List<OrderGroupSelectionDTO> GetItemsExtrasDetailSelection(PrintDB ctx, List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter);

        OrderInfoDTO GetProjectInfo(int orderID);
        OrderInfoDTO GetProjectInfo(PrintDB ctx, int orderID);

        OrderInfoDTO GetBillingInfo(int orderID);
        OrderInfoDTO GetBillingInfo(PrintDB ctx, int orderID);

        IAddress GetOrderShippingAddress(int orderID);
        IAddress GetOrderShippingAddress(PrintDB ctx, int orderID);

        List<OrderGroupSelectionDTO> GetOrderShippingAddressByGroup(List<OrderGroupSelectionDTO> selection);
        List<OrderGroupSelectionDTO> GetOrderShippingAddressByGroup(PrintDB ctx, List<OrderGroupSelectionDTO> selection);

        void AddExtraItemsByGroup(List<OrderGroupExtraItemsDTO> articleList, bool isActive = false);
        void AddExtraItemsByGroup(PrintDB ctx, List<OrderGroupExtraItemsDTO> articleList, bool isActive = false);

        Task<IEnumerable<CompanyOrderDTO>> GetOrderReportPage(OrderReportFilter filter, CancellationToken ct = default(CancellationToken));
        Task<IEnumerable<CompanyOrderDTO>> GetOrderReportPage(PrintDB ctx, OrderReportFilter filter, CancellationToken ct = default(CancellationToken));

        IEnumerable<OrderDetailDTO> GetRegisteredInSage(); // TODO: this method required filters, to avoid get big response
        IEnumerable<OrderDetailDTO> GetRegisteredInSage(PrintDB ctx);

        //IOrder CreateCustomPartialOrder(OrderInfoDTO groupInfo, int quantity, int orderDataID, bool isActive);
        IOrder CreateCustomPartialOrder(PrintDB ctx, OrderInfoDTO groupInfo, int quantity, int orderDataID, bool isActive, Guid? fileGUID = null);

        IEnumerable<OrderPrinterJobDetailDTO> GetDetailsByLabel(int orderID, int labelID);
        IEnumerable<OrderPrinterJobDetailDTO> GetDetailsByLabel(PrintDB ctx, int orderID, int labelID);

        IEnumerable<IOrder> GetOrdersByGroupID(int orderGroupID);
        IEnumerable<IOrder> GetOrdersByGroupID(PrintDB ctx, int orderGroupID);

        IEnumerable<IOrder> GetOrderWithSharedData(int orderID);
        IEnumerable<IOrder> GetOrderWithSharedData(PrintDB ctx, int orderID);

        Task<MemoryStream> GetOrderFileReport(OrderReportFilter filter); // TODO: GetOrderFileReport or  GetOrderFileCustomReport

        IEnumerable<IOrder> GetEncodedByProjectInStatusBetween(int projectID, IEnumerable<OrderStatus> orderStatus, DateTime from, DateTime to);
        IEnumerable<IOrder> GetEncodedByProjectInStatusBetween(PrintDB ctx, int projectID, IEnumerable<OrderStatus> orderStatus, DateTime from, DateTime to);

        OrderDetailDTO GetOrderArticle(int orderID);
        OrderDetailDTO GetOrderArticle(PrintDB ctx, int orderID);

        #endregion PartialQueries


        #region Comparer

        IComparerConfiguration GetComparerType(int orderId);
        IComparerConfiguration GetComparerType(PrintDB ctx, int orderId);

        OrderData GetBaseData(int id, int prevOrderId, bool showDataId);
        OrderData GetBaseData(PrintDB ctx, int id, int prevOrderId, bool showDataId);

        void CompareByRow(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates);
        void CompareByColumn(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string key, List<string> insertRows);

        OrderComparerViewModel Compare(int id, int prevOrderId, bool showDataId, int labelId);
        OrderComparerViewModel Compare(PrintDB ctx, int id, int prevOrderId, bool showDataId, int labelId);

        Task<Stream> GetComparerPreviews(Guid preview1, Guid preview2, int dataId);


        #endregion

        #region PrintLocal Sync Orders
        IEnumerable<CompanyOrderDTO> GetPendingOrdersForFactory(IEnumerable<int> currentOrders, int locationID, double deltaTimeHours);
        IEnumerable<CompanyOrderDTO> GetPendingOrdersForFactory(PrintDB ctx, IEnumerable<int> currentOrders, int locationID, double deltaTimeHours);
        void SyncOrderWithFactory(IEnumerable<CompanyOrderDTO> orders);
        #endregion PrintLocal Sync Orders

        #region PartialComposition
        CompositionLabelData GetUserCompositionForOrder(int orderID);
        IList<CompositionDefinition> GetUserCompositionForGroup(int orderGroupId, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSeparator = ",");
        IList<CompositionDefinition> GetUserCompositionForGroup(PrintDB ctx, int orderGroupId, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSeparator = ",");
        void SaveComposition(int projectId, int rowId, string composition, string careInstructions, string symbols = null);
        void SaveComposition(int projectId, int rowId, Dictionary<string, string> sectionsData, string careInstructions, string symbols);
        Dictionary<string, string> GetCompostionData(int projectId, int rowId);

        void AddCompositionOrder(CompositionDefinition composition);
        void AddCompositionOrder(PrintDB ctx, CompositionDefinition composition);
        #endregion PartialComposition

        #region CustomReport
        IEnumerable<OrderDetailWithCompoDTO> GetOrderCustomReportPage(OrderReportFilter filter);
        IEnumerable<OrderDetailWithCompoDTO> GetOrderCustomReportPage(PrintDB ctx, OrderReportFilter filter);
        MemoryStream GetOrderFileCustomReport(OrderReportFilter filter); // TODO: GetOrderFileReport or  GetOrderFileCustomReport
        void SetWorkflowTask(int orderID, OrderStatus orderStatus, bool repeatTask);


        #endregion

        MemoryStream GetDeliveryReport(OrderReportFilter filter);
        CompanyOrderCountryDTO GetCountryByOrderLocation(int orderGroupID);
        CompanyOrderCountryDTO GetCountryByOrderLocation(PrintDB ctx, int orderGroupID);


    }
}
