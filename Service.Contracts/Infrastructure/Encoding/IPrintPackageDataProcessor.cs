using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Service.Contracts
{
	public interface IPrintPackage
	{
		string GetFile(string fileName);
		void AddFile(string fileName, string content);
	}

	public interface IPrintPackageDataProcessor
	{
		void AddPrintPackageData(IPrintPackage archive, int orderid, string orderNumber, List<TableData> variableData);
		void ExtractPrintPackageData(IPrintPackage archive);
	}
}
