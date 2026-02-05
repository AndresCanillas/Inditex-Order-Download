using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class PDFProdSheet
	{
		public int OrderID { get; set; }
		public string OrderNumber { get; set; }
        public string SageReference { get; set; }
        public string MDOrderNumber { get; set; }
        public string FileName { get; set; }
        public string CompanyName { get; set; }
		public string BrandName { get; set; }
		public string ProjectName { get; set; }
		public string Provider { get; set; }
        public string ClientReference { get; set; }
        public DateTime OrderDate { get; set; }
		public DateTime ValidationDate { get; set; }
        public string Location { get; set; }
        public string PackCode { get; set; }

        public bool IncludePageQuantity { get; set; }
		public List<ProdSheetColumn> Columns { get; set; } = new List<ProdSheetColumn>();
		public List<ProdSheetArticle> Rows { get; set; } = new List<ProdSheetArticle>();
		public List<ProdSheetArticlePreview> Previews { get; set; } = new List<ProdSheetArticlePreview>();

		// The service will direct its output to this file
		public string OutputFile { get; set; }

        public string ShippingInstructions { get; set; }    

    }


    public class ProdSheetColumn
    {
        public string NameSpanish { get; set; }
        public string NameEnglish { get; set; }

        public ProdSheetColumn(string nameSpanish, string nameEnglish)
        {
            NameSpanish = nameSpanish;
            NameEnglish = nameEnglish;
        }
    }

    public class ProdSheetArticle
	{
		public bool IsPack { get; set; }
		public ProdSheetRow RowData { get; set; }
		public List<ProdSheetRow> PackArticles { get; set; }
	}

	public class ProdSheetRow
	{
        public string PackCode { get; set; }
        public string ArticleCode { get; set; }
		public int Quantity { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
        public int PageQuantity { get; set; }
		public List<string> ColumnData { get; set; } = new List<string>();

		public ProdSheetRow(string packCode, string articleCode, int quantity, int cols, int rows, int pageQuantity, List<string> columnData)
		{
            PackCode = packCode;
			ArticleCode = articleCode;
			Quantity = quantity;
            Cols = cols;
            Rows = rows;
			PageQuantity = pageQuantity;
			foreach (var val in columnData)
				ColumnData.Add(val);
		}

    }

    public class ProdSheetArticlePreview
    {
        public string PackCode { get; set; }
        public string ArticleCode { get; set; }
        public string Description { get; set; }
        public string Instructions { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalPages { get; set; }
        public Guid PreviewFileGuid { get; set; }
        public string GroupingFieldName { get; set; }
        public string GroupingFieldValue { get; set; }

        public ProdSheetArticlePreview(string packCode, string articleCode, string description, string instructions, int totalQuantity, int totalPages, Guid previewFileGuid, string groupingFieldName, string groupingFieldValue)
        {
            PackCode = packCode;
            ArticleCode = articleCode;
            Description = description;
            Instructions = instructions;
            TotalQuantity = totalQuantity;
            TotalPages = totalPages;
            PreviewFileGuid = previewFileGuid;
            GroupingFieldName = groupingFieldName;
            GroupingFieldValue = groupingFieldValue;
        }
    }
}
