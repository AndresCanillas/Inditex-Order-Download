using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts;
using Service.Contracts.Documents;
using WebLink.Contracts;

namespace WebLink.Contracts
{
	public interface IDataExportService
	{
		void Export(int orderId);
	}
}
