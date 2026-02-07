using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface ILabelRepository : IGenericRepository<ILabelData>
    {
        List<ILabelData> GetByProjectID(int projectid);
        List<ILabelData> GetByProjectID(PrintDB ctx, int projectid);

        List<string> GetGroupingFields(int id);
        List<string> GetGroupingFields(PrintDB ctx, int id);

        string GetComparerField(int id);

        void UpdateGroupingFields(int id, string data);
        void UpdateGroupingFields(PrintDB ctx, int id, string data);

        void UpdateComparerField(int id, string data);
        void UpdateComparerField(PrintDB ctx, int id, string data);

        Guid GetLabelFileReference(int labelid);
        NiceLabelInfo UploadFile(int labelid, string fileName, Stream content);
        Stream DownloadFile(int labelid, out string fileName);
        Stream GetLabelPreview(int labelid);
        Guid GetLabelPreviewReference(int labelid);

        Task SetLabelPreviewWithVariablesAsync(int labelid, string previewData);

        Task SetLabelPreviewAsync(int labelid, int orderid, int variableDataDetailID);
        Task SetLabelPreviewAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID);

        Task<Stream> GetArticlePreviewAsync(int labelid, int orderid, int variableDataDetailID);
        Task<Stream> GetArticlePreviewAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID);

        Task<Guid> GetArticlePreviewReferenceAsync(int labelid, int orderid, int variableDataDetailID);
        Task<Guid> GetArticlePreviewReferenceAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID);

        Task<string> PrintArticleAsync(int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample);
        Task<string> PrintArticleByQuantityAsync(int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample);

        Task<string> PrintArticleAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample);

        Task<NiceLabelInfo> GetLabelInfo(int id);
        string GetResourceDirectory(int projectid);
    }
}
