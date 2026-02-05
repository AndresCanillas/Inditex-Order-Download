using Service.Contracts;

namespace Services.Core
{
	class Logs
	{
		[PK, Identity]
		public long Id;
		public string EnvironmentName;
		public string MachineName;
		public string ComponentName;
		public string InstanceName;
		public long DateUtc;
		public long EntryType;
		public long PID;
		public long TID;
		public string Section;
		public string Message;
		public string ExceptionType;
		public string StackTrace;
		public string UserName;
		public string RemoteHost;

		public Logs() { }

		public Logs(string environmentName, string machineName, string componentName, string instanceName, long dateUtc, int entryType, int pID, int tID, string section, string message, string exceptionType, string stackTrace, string userName, string remoteHost)
		{
			EnvironmentName = environmentName;
			MachineName = machineName;
			ComponentName = componentName;
			InstanceName = instanceName;
			DateUtc = dateUtc;
			EntryType = entryType;
			PID = pID;
			TID = tID;
			Section = section;
			Message = message;
			ExceptionType = exceptionType;
			StackTrace = stackTrace;
			UserName = userName;
			RemoteHost = remoteHost;
		}
	}
}
