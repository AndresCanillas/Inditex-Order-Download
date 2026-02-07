using Service.Contracts;
using Service.Contracts.LabelService;
using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{


    public class FtpAccountInfo
    {
        public int CompanyID { get; set; }
        public string FtpServer { get; set; }
        public int FtpPort { get; set; }
        public int FtpsPort { get; set; }
        public string FtpUser { get; set; }
        public string FtpPassword { get; set; }
    }


    public class FtpAccountTakenException : Exception
    {
        public FtpAccountTakenException() : base("FTP User is already taken, please choose a different user name.")
        {
        }
    }


    public class FtpPasswordTooWeakException : Exception
    {
        public FtpPasswordTooWeakException() : base("FTP Password is too weak, make sure it is at least 8 characters long and includes lower case and upper case characters as well as numbers.")
        {
        }
    }


    public class NotificationFilterDTO
    {
        public int Type { get; set; }
        public long DateTicks { get; set; }
    }


    public class ArticleViewModel
    {
        public int ID { get; set; }
        public int? ProjectID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CategoryID { get; set; }
        [Nullable]
        public string CategoryName { get; set; }
        public string LabelType { get; set; }
        [Nullable]
        public string ArticleCode { get; set; }
        [Nullable]
        public string BillingCode { get; set; }
        public int? MaterialID { get; set; }
        public string MaterialName { get; set; }
        public int? LabelID { get; set; }
        public string LabelName { get; set; }
        public bool EncodeRFID { get; set; }
        public bool EnableAddItems { get; set; } 

    }


    public class PackArticleViewModel
    {
        public int ID { get; set; }
        public int PackID { get; set; }
        public int? ArticleID { get; set; }
        public string Name { get; set; }
        public string ArticleCode { get; set; }
        public int Quantity { get; set; }
        public PackArticleType Type { get; set; }
        public string Catalog { get; set; }
        public string FieldName { get; set; }
        public PackArticleCondition Condition { get; set; }
        public string Mapping { get; set; }
        public string PluginName { get; set; }
        public bool AllowEmptyValues { get; set; }
    }

    public class ArticleTrackingInfo
    {
        public int ID { get; set; }
        public int ArticleID { get; set; }
        public string CompanyName { get; set; }
        public string BrandName { get; set; }
        public string ProjectName { get; set; }
        public string ArticleCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public DateTime InitialDate { get; set; }
    }


    public class NiceLabelInfo
    {
        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<LabelVariable> Variables { get; set; }
        public bool IsDataBound { get; set; }
    }


    public class OrderProductionDetail
    {
        public int OrderID;
        public string OrderNumber;
        public int CompanyID;
        public int ProjectID;
        public int? SLADays;
        public List<OrderProductionDetailRow> Details;
    }


    public class OrderProductionDetailRow
    {
        public int DetailID;
        public int ArticleID;
        public string ArticleCode;
        public int Quantity;
        public int Product;
        public int PackID;
        public string PackCode;
        public int? LabelID;
    }


    public class OrderBillingDetail
    {
        public string CompanyCode;      // Company code of the company placing the order
        public string OrderNumber;      // Order number
        public string CompanyName;      // Name of the company placing the order
        public string GSTCode;          // GST code of the company placing the order
        public int GSTID;               // GST ID of the company placing the order
                                        // All other fields are null, except RFID_ID_Params which will be set to 1 but never used as we will never ask MD to create an access db
        public string BillTo;
        public string SendTo;
        public List<MDArticle> Articles;                // Data to be placed in Articles
        public List<MDProvider> Providers;              // Data to be placed in Proveidors
        public List<MDBillingDetail> BillingDetails;    // Data to be placed in Comandes
    }


    public class MDArticle
    {
        public string ArticleCode;      // Articles.Modelo_Label (PK) When it is from our system this will always start with 'Print_' followed by the ID of the article in our DB.
        public bool EncodeRFID;         // Flag indicating if this article is to be encoded with RFID
        public string BillingCode;      // Copy to Articles.MD_CODI_ARTICLE_ARFID if EncodeRFID is true, or copy to Articles.MD_CODI_ARTICLE_SFRID if EncodeRFID is false.
    }


    public class MDProvider
    {
        public string CompanyCode;      // Proveidors.EMPRESA = Company Code of the main company
        public string ProviderCode;     // Proveidor.Proveidor = Company Code of the provider company (workshop)
        public string Name;             // Proveidor.Nom = Name of the provider
        public string Email;            // Proveidor.Email = Main contact email for the provider (or empty string if not available)
        public string GSTCode;          // Proveidor.GST_EMPRESA = GST code of the provider (for instance SMD or IDT)
        public int GSTID;               // Proveidor.GST_CLIENT = GST Client Code for the provider
                                        // All other fields can be left empty or with 0 (if field is an integer).
    }


    public class MDBillingDetail
    {
        public int Sequence;            // Comandes.Linia = Sequence number. This starts at 1 for each order, and increases by 1 on each consecutive row.
        public string Article;          // Comandes.MODELO_LABEL = This is not the ArticleCode, but the PK used in MDArticles, in our case this always starts with 'Print_' followed by the id of the article in our DB.
        public bool EncodeRFID;         // Flag indicating if the article is with RFID or not.
        public int Quantity;            // Number of articles to bill (Important: MD expects us to place this number in either of two possible fields:
                                        //		CANTIDAD_CON_RFID or CANTIDAD_SIN_RFID.
                                        //	If the label should be encoded, then CANTIDAD_CON_RFID must be set and the other field should be 0, and viceversa.
        public DateTime Date;           // Comandes.DATA_IMPORTAT = Date from when this order was placed in the database.
                                        // All other fields can be left empty (if string) or 0 (if integers)
                                        // SOLO_FACTURAR Must always be set to '1'
    }


    public class OrderFilter
    {
        public int? CompanyID;
        public int? BrandID;
        public int? ProjectID;
        public ProductionType? ProductionType;
        public OrderStatus? OrderStatus;
    }


    public class EncodingDTO
    {
        public int CodePage { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }


    public class CultureDTO
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }


    public class ProfileDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Language { get; set; }
        public bool ChangePassword { get; set; }
        public string OriginalPassword { get; set; }
        public string NewPassword { get; set; }
        public string PasswordConfirmation { get; set; }
    }

    public class ArticleDetailDTO
    {
        public int ID { get; set; }
        public string Article { get; set; }
        public int ArticleId { get; set; }
        public int CompanyId { get; set; }
    }


    public class ProviderDTO
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProviderCompanyID { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }
        public string ClientReference { get; set; }
        public int? DefaultProductionLocation { get; set; }
        [Nullable]
        public string LocationName { get; set; }
        [Nullable]
        public string Instructions { get; set; }
        public int? SLADays { get; set; }
        public string BillingInfoName { get; set; }
        public int? BillingInfoId { get; set; }
        public bool IsVeryfied { get; set; }
        public List<ArticleDetailDTO> ArticleDetailDTO { get; set; }

    }


    public class PrinterJobFilter
    {
        public int Status;
        public int CompanyID;
        public int LocationID;
        public DateTime Date;
        public string OrderNumber;
    }


    public class JobHeaderDTO
    {
        public int OrderID { get; set; }
        public int ProjectID { get; set; }
        public int BrandID { get; set; }
        public int CompanyID { get; set; }
        [Nullable]
        public string CompanyName { get; set; }
        [Nullable]
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DueDate { get; set; }
        [Nullable]
        public ProductionType ProductionType { get; set; }
        public int JobID { get; set; }
        public int? ProductionLocationID { get; set; }
        [Nullable]
        public string LocationName { get; set; }
        public int? AssignedPrinter { get; set; }
        [Nullable]
        public string PrinterName { get; set; }
        public int ArticleID { get; set; }
        public string ArticleName { get; set; }
        public string ArticleCode { get; set; }
        public int MaterialID { get; set; }
        public string MaterialName { get; set; }
        public int LabelID { get; set; }
        public string LabelName { get; set; }
        public int Quantity { get; set; }
        public int Printed { get; set; }
        public int Errors { get; set; }
        public int Extras { get; set; }
        public JobStatus Status { get; set; }
        public bool AutoStart { get; set; }
        public string SendToCompany { get; set; }
        public int ProductDataID { get; set; }
        public bool SendToHercules { get; set; }
        public int Encoded { get; set; }
        public bool EncodeRFID { get; set; }
    }


    public class PrintJobDetailDTO
    {
        private object syncObj = new object();
        private int printed;
        private int encoded;
        private int errors;
        private int extras;
        public int ID { get; set; }
        public int PrinterJobID { get; set; }
        public int ProductDataID { get; set; }
        public string ProductData { get; set; }
        public int Quantity { get; set; }
        public int Printed
        {
            get { lock(syncObj) return printed; }
            set { lock(syncObj) printed = value; }
        }
        public void IncPrinted() { lock(syncObj) printed += 1; }
        public int Encoded { get { lock(syncObj) return encoded; } }
        public void IncEncoded() { lock(syncObj) encoded += 1; }
        public int Errors
        {
            get { lock(syncObj) return errors; }
            set { lock(syncObj) errors = value; }
        }
        public void IncErrors() { lock(syncObj) errors += 1; }
        public int Extras
        {
            get { lock(syncObj) return extras; }
            set { lock(syncObj) extras = value; }
        }
        public void IncExtras() { lock(syncObj) extras += 1; }
        public string PackCode { get; set; }
    }


    public class PrinterJobExtrasDTO
    {
        public int JobID { get; set; }
        public string ProductCode { get; set; }
        public int Quantity { get; set; }
    }





    public class DBFieldInfo
    {
        public bool Predefined { get; set; }
        public string Name { get; set; }
    }


    public class OrderParameters
    {
        public int ProductionLocationID;
        public int SLADays;
        public DateTime DueDate;
    }


    public class CatalogDataException : Exception
    {
        public string Column;

        public CatalogDataException(string column, string message) : base(message)
        {
            Column = column;
        }
    }


    public class ArtifactViewModel
    {
        public int ID { get; set; }
        public int ArticleID { get; set; }
        public int LabelID { get; set; }
        public string LabelName { get; set; }
        public string ArticleName { get; set; }
        public int LayerLabel { get; set; }
        public int Position { get; set; }
    }
}
