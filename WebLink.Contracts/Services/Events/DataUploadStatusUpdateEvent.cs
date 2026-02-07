using Service.Contracts;
using Service.Contracts.Documents;

namespace WebLink.Contracts
{
	public class DataUploadStatusUpdateEvent : EQEventInfo
	{
		public DataUploadStatusUpdateEvent(string user, DocumentImportProgress progress)
		{
			User = user;
			Progress = progress;
		}

		public string User { get; set; }
		public DocumentImportProgress Progress { get; set; }
	}
}
