using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{

	public class DynamicExportClient: BaseServiceClient, IDynamicExportClient
	{
		public ExportResult ProcessOrder(ExportSettings settings)
		{
			return Invoke<ExportSettings, ExportResult>("api/dynamicexport/processorder", settings);
		}

		public Task<ExportResult> ProcessOrderAsync(ExportSettings settings)
		{
			return InvokeAsync<ExportSettings, ExportResult>("api/dynamicexport/processorder", settings);
		}
	}
}
