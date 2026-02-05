//using PDFLib;
using Service.Contracts.LabelService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Contracts.PrintServices.PDFService
{
    public enum StatusPDFDocument
    {
        NO_STARTED = 0,
        STARTED = 1,
        COMPLETED = 2,
        ERROR = 3,

        MERGE_PENDING = 40,
        MERGE_EXECUTING = 41,
        MERGE_READY = 42,
        MERGE_ERROR = 49,

        COMPLETED_PARTIAL = 97,
        NOT_FOUND = 98,
        FULL = 99
    }

    public interface IPDFService
    {
        ////Task<PDFServiceResponse> AddTask(string key, PDFDocumentModel document);
        //Task<PDFServiceResponse> CreatePDFDocument(string key, PDFDocumentModel wrapper);
        //Task<PDFServiceResponse> CreatePDFDocumentSplited(ConcurrentDictionary<string, PDFDocumentModel> keyValues);
        Task<PDFServiceResponse> CreatePDFDocumentSplited(string JobFileId, string JobFileSplitId, PDFDocumentWrapper keyValues);
        Task<PDFServiceResponse> CreatePDFDocument(string key, PrintToFileRequest wrapper);
        Task<PDFServiceResponse> CreatePDFDocument(string Key, PDFDocumentWrapper wrapper);
        PDFDocumentWrapper GetStatus(string Key);
        MergePdfWrapper GetMergeStatus(int Key);
        void Connect();
        bool IsConnected();
        //void JoinFilesCompleted(string name, int jobFileID, List<string> pathFiles);
        Task<bool> MergeFilesForJobAsync(int jobFileID, string targetFile, List<int> filesToMerge, PDFDocumentWrapper wrapper);
        Task<bool> MergePdfForJobAsync(MergePdfWrapper wrapper);
        Task<bool> AddMergeReadyAsync(int key);
        Task<bool> AddMergePdfReadyAsync(int key);
        void ReRun(string Key);
        void Delete(List<string> Key);
        bool CanContinue(string Key);
    }

    public class PDFServiceResponse
    {
        /// <summary>
        /// Flag indicating if the process was completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// If Success is false, this will contain a descriptive error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public StatusPDFDocument Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TaskId { get; set; }
    }
}
