using Service.Contracts.PrintCentral;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IVariableDataRepository
    {
        IVariableData GetByID(int projectid, int id, bool removeIds = false);
        IVariableData GetByID(PrintDB ctx, int projectid, int id, bool removeIds = false);

        IVariableData GetByBarcode(int projectid, string code, bool removeIds = false);
        IVariableData GetByBarcode(PrintDB ctx, int projectid, string code, bool removeIds = false);

        IVariableData GetByDetailID(int projectid, int detailid, bool removeIds = false);
        IVariableData GetByDetailID(PrintDB ctx, int projectid, int detailid, bool removeIds = false);

        IVariableData GetProductDataFromDetail(int projectid, int detailid);
        IVariableData GetProductDataFromDetail(PrintDB ctx, int projectid, int detailid);

        IEnumerable<IVariableData> GetAllByDetailID(int projectid, bool removeIds, bool showDetailId, params int[] ids);
        IEnumerable<IVariableData> GetAllByDetailID(PrintDB ctx, int projectid, bool removeIds, bool showDetailId, params int[] ids);
    }
}
