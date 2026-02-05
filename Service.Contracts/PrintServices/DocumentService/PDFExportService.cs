using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{
	public interface IPDFExportClient
	{
		string Url { get; set; }
		Task ExportAsync(PDFExportConfiguration configuration);
		Task<PDFExportResult> GetJobResultAsync(string id);
	}


	class PDFExportClient : BaseServiceClient, IPDFExportClient
	{
		public Task ExportAsync(PDFExportConfiguration config)
		{
			return base.InvokeAsync<PDFExportConfiguration>("api/pdfexport/registerjob", config);
		}

		public async Task<PDFExportResult> GetJobResultAsync(string id)
		{
			return await base.InvokeAsync<string, PDFExportResult>("api/pdfexport/getjobresult", id);
		}
	}


	public class PDFExportConfiguration
	{
		public string JobID { get; set; }				// Unique id assigned to this job
		public DocType DocumentType { get; set; }		// The type of document to generate
		public string OutputFilePath { get; set; }		// The file to be generated
		public string WorkDirectory { get; set; }		// Directory where the document generation routine will search for any required resources (images, json data, etc.)

		public PDFExportConfiguration()
		{
			JobID = Guid.NewGuid().ToString();
		}
	}

	public enum DocType
	{
		OrderPreview
	}

	public class PDFExportResult
	{
		private volatile bool completed;
		private volatile bool success;
		private volatile string message;

		// Unique id assigned to this job.
		public string JobID { get; set; }
		
		// Flag indicating if the job has been completed.
		public bool Completed				
		{
			get { return completed; }
			set { completed = value; }
		}

		// Flag indicating if the document was sucessfully generated, meaningful only if Completed is true.
		public bool Success
		{
			get { return success; }
			set { success = value; }
		}

		// Message from the export process, meaningful only if Completed is true. In case of error this contains the error message.
		public string Message 
		{
			get { return message; }
			set { message = value; }
		}
	}


	public class OrderPreviewData
	{
		public int OrderID;
		public string OrderNumber;
		public int ProjectID;
		public string Company;
		public List<OrderPreviewArticle> Articles;
	}

	public class OrderPreviewArticle
	{
		public int ArticleID;
		public int LabelID;
		public int ProductDataID;
		public string ArticleCode;
		public string ProductCode;
		public int Quantity;
		public string ImagePath;
	}
}
