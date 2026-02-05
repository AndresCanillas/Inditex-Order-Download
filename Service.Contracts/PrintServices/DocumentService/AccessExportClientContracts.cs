using Service.Contracts.Database;
using System.Collections.Generic;

namespace Service.Contracts.Documents
{
    //public interface IAccessExportClient
    //{
    //	string Url { get; set; }
    //	Task<ExportToolResult> ExportDataAsync(ExportToolSettings settings);
    //}

    public class ExportToolSettings
    {
        public string OutputPath { get; set; }
        public string ZipFile { get; set; }
        public ExportToolFormat Format { get; set; }
        public List<TableData> Tables { get; set; }
        public bool CreateGlobalView { get; set; }
        public string RootViewTable { get; set; }
    }

    public class ExportToolResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string StackTrace { get; set; }
    }
}
