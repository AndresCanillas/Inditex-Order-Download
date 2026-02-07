using Service.Contracts;

namespace WebLink.Contracts
{
	public class PrinterConnectedEvent : EQEventInfo
	{
		public string DeviceID { get; set; }
		public string ProductName { get; set; }
		public string Firmware { get; set; }

		public PrinterConnectedEvent(string deviceid, string productName, string firmware)
		{
			DeviceID = deviceid;
			ProductName = productName;
			Firmware = firmware;
		}
	}
}
