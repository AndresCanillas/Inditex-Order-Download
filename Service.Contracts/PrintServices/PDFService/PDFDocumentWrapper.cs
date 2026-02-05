using Service.Contracts.LabelService;
using System.Collections.Generic;

namespace Service.Contracts.PrintServices.PDFService
{
    public class PDFDocumentWrapper
    {
        public StatusPDFDocument StatusPDFDocument { get; set; }
        public object Exception { get; set; }
        public PrintPDFRequest SiglePages { get; set; }
        public bool IsSiglePage { get; set; }
        public byte[] ContentResult { get; set; }
        public int Priority { get; set; }
    }
    public class MergePdfWrapper : PDFDocumentWrapper
    {
        public int MergePdfId { get; set; }
        public List<string> FilesPaths { get; set; }
        public string TargetFile { get; set; }
        public bool HasBlankSheets { get; set; }
    }
}
