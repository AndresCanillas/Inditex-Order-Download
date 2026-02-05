using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{
	public class AccessExportClient: BaseServiceClient
	{
		public Task<ExportToolResult> ExportDataAsync(ExportToolSettings settings)
		{
			return InvokeAsync<ExportToolSettings, ExportToolResult>("api/AccessExport/ExportData", settings);
		}
	}
}
