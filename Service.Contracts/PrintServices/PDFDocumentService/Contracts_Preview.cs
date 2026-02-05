using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.PDFDocumentService
{
	public interface IPDFDocumentService
	{
		string Url { get; set; }
		Task<PDFDocumentResult> CreateOrderPreviewAsync(PDFOrderPreview documentData);
		Task<PDFDocumentResult> CreateProductionSheetAsync(PDFProdSheet documentData);
        Task<PDFDocumentResult> CreateOrderDetailAsync(PDFOrderDetail documentData);
	}


	public class PDFDocumentResult
	{
		public bool Success { get; set; }
		public string ErrorMessage { get; set; }
	}


	public class PDFOrderPreview
	{
		public string OrderNumber { get; set; }
		public string MDOrderNumber { get; set; }
		public string CompanyName { get; set; }
        public string Provider { get; set; }
        public string ClientReference { get; set; }
        public string BrandName { get; set; }
		public string ProjectName { get; set; }
		public DateTime OrderDate { get; set; }
		public DateTime ValidationDate { get; set; }
		public List<PDFOrderPreviewArticle> Articles { get; set; } = new List<PDFOrderPreviewArticle>();  // Information of each article included in the order
		public string OutputFile { get; set; }	// The service will direct its output to this file
        //ijsanchez PC_dev_I02
        public int? OrderGroupID;
        public string SupportFileCategory { get; set; }
    }


	public class PDFOrderPreviewArticle
	{
		public string Name { get; set; }
		public string ArticleCode { get; set; }
		public int TotalQuantity { get; set; }  // The number of units to be printed from this article
		public string GroupingField { get; set; }	// Name of the grouping field (as setup in the main label for this article)
		public int Rows { get; set; }       // Each page is divided in rows, this specifies how many rows per page are created in the document. Valid range [1, 3], default is 3. This setting applies per article.
		public int Cols { get; set; }       // Each row is divided in columns, this specifies how many columns per row are created. Valid range [1, 3], default is 3. This setting applies per article.
		public List<PDFArticleUnit> Units { get; set; } = new List<PDFArticleUnit>();
	}


	public class PDFArticleUnit
	{
		public string Text { get; set; }            // Text to be displayed under the unit (This is the value of the Grouping field from the main label)
		public int Quantity { get; set; }           // Quantity to produce of this specific unit
		public bool UseFixedPreview { get; set; }   // Indicates if the preview to be used is a fixed preview (extracted from ArticlePreviewStore or ProjectStore) or if the preview is dynamic (extracted from the LabelServiceCacheStore)
		public Guid PreviewFileGUID { get; set; }   // Guid associated to the preview that should be used for this unit
		public List<PDFOrderPreviewArtifact> Artifacts { get; set; } = new List<PDFOrderPreviewArtifact>();
	}


	// NOTE: For now it is assumed that the number of units to print of each artifact is the same as the main label... Might need to change in the future.
	public class PDFOrderPreviewArtifact
	{
		public string Name { get; set; }
		public Guid PreviewFileGUID { get; set; }     // The Guid of the preview for this artifact
	}

	public class PageMargins
	{
		public double Left;
		public double Top;
		public double Right;
		public double Bottom;

		public PageMargins(double left, double top, double right, double bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}
	}
}
