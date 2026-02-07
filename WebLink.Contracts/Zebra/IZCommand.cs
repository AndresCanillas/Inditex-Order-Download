using System;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public interface IZCommand
	{
		bool IsOneWay { get; set; }
		TimeSpan CommandTimeout { get; set; }
		byte[] ToByteArray();
		Task WaitForTransmission();
		Task<string> WaitForResponse();
		Task<string> WaitForResponse(TimeSpan timeout);
		void SetTransmission();
		void SetResponse(string response);
		void SetError(Exception ex);
	}

	public interface IPrintLabelCommand : IZCommand
	{
		Task PrepareLabel(int projectid, int labelid, int orderid, string orderNumber, int detailid, IVariableData productData, IPrinterSettings settings, string driverName, bool isSample);
		Task PrepareHeader(IVariableData data, IPrinterSettings settings, string driverName);
		bool EncodeRFID { get; }
		string LabelName { get; }
		string ProductCode { get; }
		long LastSerial { get; }
		string LastEPC { get; }
		string AccessPassword { get; }
		string KillPassword { get; }
		string Preamble { get; set; }
		bool EnableCut { get; set; }
		bool IsLastInBatch { get; set; }
	}
}
