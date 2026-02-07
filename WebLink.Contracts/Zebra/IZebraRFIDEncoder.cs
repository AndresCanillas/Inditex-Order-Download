using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using Service.Contracts.PrintCentral;
using Service.Contracts;

namespace WebLink.Contracts
{
	public interface IZebraRFIDEncoder
	{
		int OrderID { get; set; }
		string OrderNumber { get; set; }
		int DetailID { get; set; }
		long LastSerial { get; }
		string LastEPC { get; }
		string AccessPassword { get; }
		string KillPassword { get; }
		void SetRFIDConfig(IRFIDConfig config);
		string Encode(IVariableData data);
	}
}
