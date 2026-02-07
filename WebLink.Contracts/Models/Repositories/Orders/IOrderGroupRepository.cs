using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System.Collections.Generic;
using System.IO;

namespace WebLink.Contracts.Models
{
    public interface IOrderGroupRepository : IGenericRepository<IOrderGroup>
    {
		IOrderGroup GetGroupFor(IOrderGroup data);
		IOrderGroup GetGroupFor(PrintDB ctx, IOrderGroup data);
		IOrderGroup GetOrCreateGroup(PrintDB ctx, IOrderGroup data,int? ProviderRecordId);
        void SetOrderGroupAttachment(int orderGroupID, string attachmentCategory, string fileName, Stream stream);

        OrderInfoDTO GetProjectInfo(int orderGroupID);
		OrderInfoDTO GetProjectInfo(PrintDB ctx, int orderGroupID);

		OrderInfoDTO GetBillingInfo(int orderGroupID);
		OrderInfoDTO GetBillingInfo(PrintDB ctx, int orderGroupID);

		Project GetProjectBy(OrderArticlesFilter filter);
		Project GetProjectBy(PrintDB ctx, OrderArticlesFilter filter);

		IEnumerable<IOrder> ChangeProvider(int orderGroupID, int providerRecordID);
		IEnumerable<IOrder> ChangeProvider(PrintDB ctx, int orderGroupID, int providerRecordID);

		IEnumerable<OrderGroupDetailDTO> GetRegisteredInSage();
		IEnumerable<OrderGroupDetailDTO> GetRegisteredInSage(PrintDB ctx);

        IEnumerable<IOrder> GetAllErpOrderReferencesInGroup(OrderInGroupFilter filter);
        IEnumerable<IOrder> GetAllErpOrderReferencesInGroup(PrintDB ctx, OrderInGroupFilter filter);
        void ChangeOrderNumber(int orderGroupID, string orderNumber);
        void ChangeOrderNumber(PrintDB ctx, int orderGroupID, string orderNumber);
        OperationResult AttachOrderGroupDocument(OrderAttachDocumentRequest attachRequest);
        OrderPdfResult GetOrderPdf(int ordergroupid, string orderNumber, string attachmentCategory);
        IEnumerable<OrderGroup> GetGroupByOrderNumberList(string orderNumber, int projectID, int days);
    }
}