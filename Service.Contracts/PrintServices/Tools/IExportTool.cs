using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IExportTool
    {
        void Run(string fileName, List<TableData> exportedData, ExportToolFormat exportFormat);
        Task RunAsync(string fileName, List<TableData> exportedData, ExportToolFormat exportFormat);
    }

    public enum ExportToolFormat
    {
        AccessFile,
        FlatTextFile
    }

    public class ExportTool : IExportTool
    {
        private ITempFileService tempFiles;
        private string toolPath;

        public ExportTool(IAppConfig config, ITempFileService tempFiles)
        {
            this.tempFiles = tempFiles;
            toolPath = config.GetValue("Tools.ExportTool", Environment.CurrentDirectory);
        }

        public Task RunAsync(string fileName, List<TableData> exportedData, ExportToolFormat exportFormat)
        {
            string inputFile = tempFiles.GetTempFileName("exportInput.json", true);
            string outputFile = tempFiles.GetTempFileName("exportOutput.json", true);
            var cmdOutoput = string.Empty;
            try
            {

                ExportToolSettings settings = new ExportToolSettings()
                {
                    OutputPath = fileName,
                    Tables = exportedData,
                    ZipFile = null,
                    CreateGlobalView = false,
                    RootViewTable = null
                };

                File.WriteAllText(inputFile, JsonConvert.SerializeObject(settings));


                cmdOutoput = CmdLineHelper.RunCommand(Path.Combine(toolPath, "ExportTool.exe"), $@"/config=""{inputFile}"" /result=""{outputFile}""", Path.GetDirectoryName(fileName), waitForCompletion: true);

                var fileContent = File.ReadAllText(outputFile);
                var result = JsonConvert.DeserializeObject<ExportToolResult>(fileContent);
                if(result == null)
                    throw new Exception($"go to the file {fileContent}");
                if(!result.Success)
                    throw new Exception($"ExportTool did not return a valid response:\r\n {result.Error} \r\n {result.StackTrace}");
            }
            finally
            {
                if(File.Exists(inputFile)) { try { File.Delete(inputFile); } catch { } }
                if(File.Exists(outputFile)) { try { File.Delete(outputFile); } catch { } }
            }

            return Task.CompletedTask;
        }

        public void Run(string fileName, List<TableData> exportedData, ExportToolFormat exportFormat)
        {
            RunAsync(fileName, exportedData, exportFormat).Wait();
        }
    }

    public static class ExportedDataExtensions
    {
        class TableDataEx : TableData
        {
            public JArray Rows;
            public List<FieldDefinition> Definition;
            public bool Ignore;

            public TableDataEx(TableData t)
            {
                CatalogID = t.CatalogID;
                Name = t.Name;
                CatalogType = t.CatalogType;
                Fields = t.Fields;
                Records = t.Records;
                Rows = JArray.Parse(t.Records);
                Definition = JsonConvert.DeserializeObject<List<FieldDefinition>>(t.Fields);
                Ignore = false;
            }
        }


        public static void FlattenExportedData(this List<TableData> tables, string rootCatalogName, string outputFile)
        {
            using(var fs = File.OpenWrite(outputFile))
            {
                FlattenExportedData(tables, rootCatalogName, fs);
            }
        }


        public static void FlattenExportedData(this List<TableData> tables, string rootCatalogName, Stream outputStream)
        {
            List<TableDataEx> exTables = new List<TableDataEx>();
            foreach(var t in tables)
                exTables.Add(new TableDataEx(t));

            var rootCatalog = exTables.FirstOrDefault(t => String.Compare(t.Name, rootCatalogName, true) == 0);
            if(rootCatalog == null)
                throw new Exception($"Could not find table {rootCatalogName} in the export data.");

            outputStream.SetLength(0L);
            if(rootCatalog.Rows.Count > 0)
            {
                JObject row = rootCatalog.Rows[0] as JObject;
                var line = ExpandHeader(rootCatalog, exTables);
                var utf8 = Encoding.UTF8.GetBytes(line + "\r\n");
                outputStream.Write(utf8, 0, utf8.Length);
                foreach(var t in rootCatalog.Rows)
                {
                    row = t as JObject;
                    line = ExpandRowReference(rootCatalog, exTables, row);
                    utf8 = Encoding.UTF8.GetBytes(line + "\r\n");
                    outputStream.Write(utf8, 0, utf8.Length);
                }
            }
        }


        private static string ExpandHeader(TableDataEx root, List<TableDataEx> tables)
        {
            StringBuilder sb = new StringBuilder(10000);
            foreach(var field in root.Definition)
            {
                if(field.Name == "ID") continue;
                sb.Append(field.Name).Append(',');
                if(field.Type == ColumnType.Reference)
                {
                    var referencedCatalog = tables.FirstOrDefault(t => t.CatalogID == field.CatalogID);
                    if(referencedCatalog.Rows.Count == 0)
                        referencedCatalog.Ignore = true;
                    else
                        sb.Append(ExpandHeader(referencedCatalog, tables) + ",");
                }
            }
            if(sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        private static string ExpandRowReference(TableDataEx root, List<TableDataEx> tables, JObject rowData)
        {
            StringBuilder sb = new StringBuilder(10000);
            foreach(var field in root.Definition)
            {
                if(field.Name == "ID") continue;
                string value;
                var token = rowData[field.Name];
                if(token != null)
                    value = token.ToString();
                else
                    value = "";
                sb.Append("\"").Append(value.Replace("\"", "\"\"")).Append("\",");
                if(field.Type == ColumnType.Reference)
                {
                    var referencedCatalog = tables.FirstOrDefault(t => t.CatalogID == field.CatalogID);
                    if(referencedCatalog == null)
                        throw new Exception($"Could not find table ({field.CatalogID}) in the export data.");
                    if(!referencedCatalog.Ignore)
                    {
                        if(String.IsNullOrWhiteSpace(value))
                        {
                            sb.Append(ExpandNullReference(referencedCatalog, tables) + ",");
                        }
                        else
                        {
                            int id = Convert.ToInt32(value);
                            var referencedRowData = referencedCatalog.Rows.FirstOrDefault(r => (r as JObject).GetValue<int>("ID") == id) as JObject;
                            sb.Append(ExpandRowReference(referencedCatalog, tables, referencedRowData) + ",");
                        }
                    }
                }
            }
            if(sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        private static string ExpandNullReference(TableDataEx root, List<TableDataEx> tables)
        {
            StringBuilder sb = new StringBuilder(10000);
            foreach(var field in root.Definition)
            {
                if(field.Name == "ID") continue;
                sb.Append("").Append(',');
                if(field.Type == ColumnType.Reference)
                {
                    var referencedCatalog = tables.FirstOrDefault(t => t.CatalogID == field.CatalogID);
                    sb.Append(ExpandNullReference(referencedCatalog, tables) + ",");
                }
            }
            if(sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}
