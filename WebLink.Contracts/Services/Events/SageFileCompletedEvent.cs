using Service.Contracts;
using Service.Contracts.PrintCentral;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class SageFileCompletedEvent : EQEventInfo
	{
		public int OrderID { get; set; }
		public SageFileCompletedEvent()
		{
		}
		public SageFileCompletedEvent(int orderID)
		{
			OrderID = orderID;	
		}
	}
}
