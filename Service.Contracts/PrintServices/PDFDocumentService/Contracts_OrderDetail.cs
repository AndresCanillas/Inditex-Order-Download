using System;
using System.Collections.Generic;

namespace Service.Contracts
{
    public class PDFOrderDetail
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
        public List<OrderDetailColumn> Columns { get; set; } = new List<OrderDetailColumn>();
        public List<OrderDetailArticle> Rows { get; set; } = new List<OrderDetailArticle>();
        public List<OrderDetailArticlePreview> Previews { get; set; } = new List<OrderDetailArticlePreview>();

        // The service will direct its output to this file
        public string OutputFile { get; set; }
    }

    public class OrderDetail_Table_Columns
    {
        public string Ref { get; set; }
        public string Description { get; set; }
        public string QuantityByUnitOdNeasure { get; set; }
        public string PricebyUnitOfMeasure { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Measure { get; set; }
    }

    public class OrderDetailColumn
    {
        public string NameSpanish;
        public string NameEnglish;

        public OrderDetailColumn(string spanish, string english)
        {
            NameSpanish = spanish;
            NameEnglish = english;
        }
    }

    public class OrderDetailArticle
    {
        public bool IsPack;
        public OrderDetailRow RowData { get; set; }
        public List<OrderDetailRow> PackArticles { get; set; }
    }

    public class OrderDetailRow
    {
        public string PackCode;
        public string ArticleCode;
        public int Quantity;
        public int Cols;
        public int Rows;
        public int PageQuantity;
        public List<string> ColumnData { get; set; } = new List<string>();
        public string Description;
        public int TotalPages;
        public string TotalQuantity;
        public string BillingCode;

        public OrderDetailRow(string packCode, string code, string description, int pages, string BillingCode, string Quantity, params string[] columndata)
        {
            this.ArticleCode = code;
            this.Description = description;
            this.TotalPages = pages;
            this.TotalQuantity = Quantity;
            this.BillingCode = BillingCode;
            this.PackCode = packCode;
            //PageQuantity = pagequantity;
            foreach (var val in columndata)
                ColumnData.Add(val);
        }

        //public OrderDetailRow(string packCode, string code, int quantity, int cols, int rows, int pagequantity, params string[] columndata)
        //{
        //    PackCode = packCode;
        //    ArticleCode = code;
        //    Quantity = quantity;
        //    Cols = cols;
        //    Rows = rows;
        //    PageQuantity = pagequantity;
        //    foreach (var val in columndata)
        //        ColumnData.Add(val);
        //}
    }

    public class OrderDetailArticlePreview
    {
        public string PackCode;
        public string ArticleCode;
        public string Description;
        public string Instructions;
        public string TotalQuantity;
        public int TotalPages;
        public Guid PreviewFileGuid;
        public string GroupingFieldName;
        public string GroupingFieldValue;
        public string Size;
        public string Color;
        public string BillingCode;

        public OrderDetailArticlePreview(string code, string description, int pages, string BillingCode, string Quantity)
        {
            this.ArticleCode = code;
            this.Description = description;
            this.TotalPages = pages;
            this.TotalQuantity = Quantity;
            this.BillingCode = BillingCode;
        }
    }
}
