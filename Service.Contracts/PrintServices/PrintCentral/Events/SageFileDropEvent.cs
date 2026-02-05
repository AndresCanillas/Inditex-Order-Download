using Service.Contracts.WF;

namespace Service.Contracts.PrintCentral
{
	public class SageFileDropEvent: EQEventInfo
	{
		public SageFileDropEvent() { }

		public SageFileDropEvent(int orderid, string sageOrderNumber, string projectPrefix) 
		{
			OrderID = orderid;
			SAGEOrderNumber = sageOrderNumber;
			ProjectPrefix = projectPrefix;
		}

		public int OrderID;
		public string SAGEOrderNumber;
		public string ProjectPrefix;
	}


	public class SageFileDropAckEvent : EQEventInfo
	{
		public SageFileDropAckEvent()
		{
			//NOTE: Empty constructor is required by APM Workflow Constraints
		}

		public SageFileDropAckEvent(SageFileDropEvent e)
		{
			OrderID = e.OrderID;
			SAGEOrderNumber = e.SAGEOrderNumber;
			ProjectPrefix = e.ProjectPrefix;
		}

		public int OrderID;
		public string SAGEOrderNumber;
		public string ProjectPrefix;
	}
}
