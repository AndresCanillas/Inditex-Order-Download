using System;
using System.Threading.Tasks;
using Service.Contracts.Documents;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class DataImportJobInfo
	{
		private object syncObj = new object();
		private DocumentImportProgress progress = new DocumentImportProgress();
		private DocumentImportResult result;
		private ImportedData importeddata;
		private bool started;
		private bool isrunning;
		private bool keepalive = true;
		private bool completed;
		private object userdata;
		private Func<DataImportJobInfo, Task> callback;

		public DataImportJobInfo(string username, int projectid, DocumentSource source)
		{
			JobID = Guid.NewGuid().ToString();
			User = username;
			ProjectID = projectid;
			Source = source;
			PurgeFile = (Source != DocumentSource.FTP);
		}

		public string JobID { get; set; }
		public string User { get; set; }
		public int ProjectID { get; set; }
		public DocumentSource Source { get; set; }
		public string FileName { get; set; }
		public Guid FileGUID { get; set; }
		public bool PurgeFile { get; set; }
		public DocumentImportConfiguration Config { get; set; }
		public DateTime Date { get; set; } = DateTime.Now;
		public DocumentImportProgress Progress
		{
			get { lock (syncObj) return progress; }
			set { lock (syncObj) progress = value; }
		}
		public DocumentImportResult Result
		{
			get { lock (syncObj) return result; }
			set { lock (syncObj) result = value; }
		}
		public ImportedData ImportedData
		{
			get { lock (syncObj) return importeddata; }
			set { lock (syncObj) importeddata = value; }
		}
		public bool Started
		{
			get { lock (syncObj) return started; }
			set { lock (syncObj) started = value; }
		}
		public bool IsRunning
		{
			get { lock (syncObj) return isrunning; }
			set { lock (syncObj) isrunning = value; }
		}
		public bool KeepAlive
		{
			get { lock (syncObj) return keepalive; }
			set { lock (syncObj) keepalive = value; }
		}
		public bool Completed
		{
			get { lock (syncObj) return completed; }
			set { lock (syncObj) completed = value; }
		}
		public object UserData
		{
			get { lock (syncObj) return userdata; }
			set { lock (syncObj) userdata = value; }
		}
		public Func<DataImportJobInfo, Task> CompleteCallback
		{
			get { lock (syncObj) return callback; }
			set { lock (syncObj) callback = value; }
		}
	}
}
