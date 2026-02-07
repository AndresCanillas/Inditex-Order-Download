using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services.Wizards
{
    public interface IItemAssigmentService
    {

        void SetVariableData(IEnumerable<ProductField> commonFields, CustomArticle selectedarticle, IOrder order);
        void SetVariableData(PrintDB ctx, IEnumerable<ProductField> commonFields, CustomArticle selectedarticle, IOrder order);
        List<OrderGroupSelectionDTO> GetOrderData(PrintDB ctx, List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter = null);
        List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter = null);
        CustomDetailSelectionDTO UpdateOrders(CustomDetailSelectionDTO rq);
        void UpdateSizes(List<FieldsToUpdateDTO> rq);
        void UpdateTagtypes(List<FieldsToUpdateDTO> rq);
        void UpdateCustomTrackinCode(List<int> orderIds);
        //void SetBaseData(IEnumerable<ProductField> baseFields, IOrder order);
        //void SetBaseData(PrintDB ctx, IEnumerable<ProductField> baseFields, IOrder order);
        //void SetBaseData(PrintDB ctx, IEnumerable<ProductField> updateColumns, IEnumerable<ProductField> filterColumns, int projectId);
        //void SetBaseData(IEnumerable<ProductField> updateColumns, IEnumerable<ProductField> filterColumns, int projectId);
        //void UpdateBaseDataOrders(CustomDetailSelectionDTO rq);
    }
}
