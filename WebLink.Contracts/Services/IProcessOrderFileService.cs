using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts
{
    [Obsolete("Replace by IntakeWorkflow")]
    public interface IProcessOrderFileService
    {
        void RegisterFile(string fileName, string filePath, UploadOrderDTO dto);
        Task<DocumentImportResult> ProcessFile(int ftpFileReceivedId);
        bool FileIsPending(int ftpFileReceivedID);

        // TODO: Remove the following 3 methods??
        void CancelPendingEventFor(int ftpFileReceivedId);
        void CancelAllEvents();
        void ResetEventFor(int ftpFileReceivedId);
    }
}
