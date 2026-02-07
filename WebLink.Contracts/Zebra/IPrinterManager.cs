using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;

namespace WebLink.Contracts
{
	public interface IZPrinterManager
	{
		List<IZPrinter> GetList();
		List<PrinterState> GetPrinterStates();
		IZPrinter GetPrinter(int id);
		IZPrinter GetPrinter(string deviceid);
		IZPrinter RegisterPrinter(IWSConnection connection);
		void RegisterChannel(IWSConnection connection);
		void RemovePrinter(string deviceid);
	}
}
