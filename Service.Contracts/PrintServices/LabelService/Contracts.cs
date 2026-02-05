using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.LabelService
{
    public interface ILabelServiceClient
    {
        string Url { get; set; }
        bool Authenticated { get; }
        string Token { get; }
        DateTime ExpirationDate { get; }
        void Login(string controller, string user, string password);
        Task LoginAsync(string controller, string user, string password);
        Task<PrinterDriversResponse> GetPrinterDriversAsync();
        Task<LabelInfoResponse> GetLabelInfoAsync(LabelInfoRequest data);
        Task<LabelServiceResponse> GetArticlePreviewAsync(ArticlePreviewRequest data);
        Task<LabelServiceResponse> GetArticlePreview2Async(ArticlePreviewRequest2 data);
        Task<LabelServiceResponse> PrintArticleAsync(PrintToFileRequest data);
        Task<LabelServiceResponse> PrintArticleByQuantityAsync(PrintToFileRequest config);
        Task<LabelServiceContentResponse> PrintArticleLocallyAsync(PrintToFileRequest data);
        Task<LabelServiceContentResponse> PrintArticleLocallyByQuantityAsync(PrintToFileRequest data);
        Task<ComparePreviewResponse> ComparePreviewsAsync(ComparePreviewsRequest data);
        Task<LabelServiceContentResponse> PrintHeaderAsync(PrintHeaderRequest config);
        Task<LabelServiceResponse> InvalidateCache(LabelCacheInvalidationRequest data);
        Task<LabelServiceResponse> PrintPDFLibAsync(PrintPDFRequest rq);
        Task<LabelServiceContentResponse> PrintPDFLibLocallyAsync(PrintPDFRequest rq);
    }


    public interface IBLabelServiceClient : ILabelServiceClient
    {
        int MaxRetries { get; set; }
        void SetEndPoints(List<string> endpoints);
        void SetEndPoints(List<LSEP> endpoints);
        int NumberOfAvailablesEndpoints { get; }
    }


    public class ComparePreviewsRequest : ABBaseServiceRequest
    {
        public Guid Preview1FileGUID;
        public Guid Preview2FileGUID;
        public Guid ResultFileGUID;
    }

    public class ComparePreviewResponse : BaseServiceResponse
    {
    }

    public class LabelServiceResponse : BaseServiceResponse
    {
        public Guid FileGUID;
        public byte[] Result;		// NOTE: Used only if the request specified that the result should be included in the response. Otherwise, it is assumed the calling system can access the file referenced in the response directly.
    }

    public class LabelServiceContentResponse : BaseServiceResponse
    {
        public string Content;
    }


    public class ArticlePreviewRequest : ABBaseServiceRequest
    {
        public int ProjectID;                   // ID of the project (used to access the project file store)
        public int LabelID;						// ID of the label (label file will be accessed directly from the label file store)
        public string FileName;					// Name of the label file, required to locate the right attachment whithin the Labels category.
        public string FileUpdateDate;           // The date of last update of the label file in the format "yyyy/MM/dd HH:mm:ss".
        public Guid LabelFileGUID;              // Guid used to locate the Label file
        public int DetailID;                    // ID of the detail row (this is used to keep track of items in the cache inside the label service)
        public LabelSide PreviewSide;           // Specifies which side of the label will be previewed
        public bool UseDefaultSize;             // Specifies if the real size of the label should be used, or if the specified Width & Height should be used instead.
        public int Width;                       // Width of the produced image (used only if UseDefaultSize is false)
        public int Height;                      // Height of the produced image (used only if UseDefaultSize is false)
        public bool IncludeWaterMark;           // If set to true the preview will include a watermark ("SMARTDOTS" printed across the preview)
        public List<LabelMapping> Mappings;		// Defines how variables are going to be initialized using the data found in VariableData.
        public List<TableData> ExportedData;	// Contains the data that will be exported to access (used when the label is bound to a data source)
        public bool AttachAsLabelPreview;		// A flag indicating if the preview should be attached to the label as the label preview
        public bool IncludeResultInResponse;
    }

    //ijsanchezm begin  
    public class PrintPDFRequest : ABBaseServiceRequest
    {
        public PrintPDFRequest()
        {

        }
        public int ProjectID;
        public int LabelID;
        public string FileName;
        public string SlaveFileName;
        public Guid LabelFileGUID;
        public string FileUpdateDate;
        public int DetailID;
        public string DriverName;
        public int XOffset;
        public int YOffset;
        public string Darkness;
        public string Speed;
        public bool ChangeOrientation;
        public bool Rotated;
        public List<LabelMapping> Mappings;
        public List<TableData> ExportedData;
        public bool IncludeResultInResponse;
        public bool IsSerialized = false;
        public int MaxPageNumber;
        public int Quantity;
        public int JobPDFFileID;
        public int ArticleID;
        public int OrderArticleID;
        public int CentralArticleID;
        public string PathMdb;
        public string PathJson;
        public string OutputFile;
        public string LabelFile;
    }

    //ijsanchezm end

    public class ArticlePreviewRequest2 : ABBaseServiceRequest
    {
        public int ProjectID;                           // ID of the project (used to access the project file store)
        public int LabelID;                             // ID of the label (label file will be accessed directly from the label file store)
        public string FileName;                         // Name of the label file, required to locate the right attachment whithin the Labels category.
        public string FileUpdateDate;                   // The date of last update of the label file in the format "yyyy/MM/dd HH:mm:ss".
        public Guid LabelFileGUID;                      // Guid used to locate the Label file
        public LabelSide PreviewSide;                   // Specifies which side of the label will be previewed
        public bool UseDefaultSize;                     // Specifies if the real size of the label should be used, or if the specified Width & Height should be used instead.
        public int Width;                               // Width of the produced image (used only if UseDefaultSize is false)
        public int Height;                              // Height of the produced image (used only if UseDefaultSize is false)
        public bool IncludeWaterMark;                   // If set to true the preview will include a watermark ("SMARTDOTS" printed across the preview)
        public List<LabelMapping> Mappings;             // Defines how variables are going to be initialized using the data found in VariableData.
        public Dictionary<string, string> VariableData;	// Contains the data that will be exported to access (used when the label is bound to a data source)
        public bool AttachAsLabelPreview;				// A flag indicating if the preview should be attached to the label as the label preview
        public bool IncludeResultInResponse;
    }


    public enum LabelSide
    {
        Front,
        Back,
        Both
    }

    public enum ArtifactDisposition
    {
        Left,
        Top,
        Right,
        Bottom
    }


    public class LabelMapping
    {
        public string Variable;
        public List<string> Fields = new List<string>();

        public LabelMapping() { }

        public LabelMapping(string variable, params string[] fields)
        {
            Variable = variable;
            Fields = new List<string>();
            foreach(var f in fields)
            {
                Fields.Add(f);
            }
        }

        public static List<LabelMapping> CreateMappingsFromData(Dictionary<string, string> data)
        {
            List<LabelMapping> mappings = new List<LabelMapping>();
            foreach(string key in data.Keys)
            {
                mappings.Add(new LabelMapping()
                {
                    Variable = key,
                    Fields = new List<string>() { key }
                });
            }
            return mappings;
        }
    }


    public class LabelInfoRequest : ABBaseServiceRequest
    {
        public Guid LabelFile;
        public string FilePath;
    }


    [Serializable]
    public class PrinterDriversResponse : BaseServiceResponse
    {
        public List<PrinterDriverInfo> Drivers;
    }

    [Serializable]
    public class PrinterDriverInfo
    {
        public string Name;
    }

    [Serializable]
    public class LabelInfoResponse : BaseServiceResponse
    {
        public string FileName;
        public int Width;
        public int Height;
        public int Rows;
        public int Cols;
        public List<LabelVariable> Variables;
        public bool IsDataBound;
    }

    [Serializable]
    public class LabelVariable
    {
        public string Name;
        public string Description = "";
        public string DefaultValue = "";
        public bool IsValueRequired = true;
        public int Length = 50;
        public int PromptOrder;
        public VariableType VariableType = VariableType.Text;
    }

    public enum VariableType
    {
        Unknown,
        Text,
        Date,
        Time,
        FloatingPoint,
        Currency,
        Counter
    }


    public class PrintToFileRequest : ABBaseServiceRequest
    {
        public PrintToFileRequest()
        {
        }

        public int ProjectID;
        public int LabelID;
        public string FileName;
        public string SlaveFileName;
        public Guid LabelFileGUID;
        public string FileUpdateDate;
        public int DetailID;
        public string DriverName;
        public string XOffset;
        public string YOffset;
        public string Darkness;
        public string Speed;
        public bool ChangeOrientation;
        public bool Rotated;
        public List<LabelMapping> Mappings;                // Defines how variables are going to be initialized using the data found in VariableData.
        public List<TableData> ExportedData;               // Contains the data that will be exported to access (used when the label is bound to a data source)
        public bool IncludeResultInResponse;               // Defaults to false, the result is not included in the response. In this case the field 
        public bool IsSerialized = false;					// Flag indicating if the label is serialized or not. Serialized labels should not be put in the print cache because they change every time and will never be reused.
        public int Quantity = 1;
    }


    public class LabelCacheInvalidationRequest : ABBaseServiceRequest
    {
        public int LabelID;            // Optional: Send 0 to invalidate entire cache
        public List<int> DetailIDs;    // Optional: Send null (or an empty list) to invalidate all details (for the specified labelid)
    }

    public class PrintHeaderRequest : ABBaseServiceRequest
    {
        public PrintHeaderRequest()
        {
        }

        public HeaderType HeaderType;
        public string DriverName;
        public int XOffset;
        public int YOffset;
        public string Darkness;
        public string Speed;
        public bool ChangeOrientation;
        public bool Rotated;
        public Dictionary<string, string> Values;   // Contains values for the different header fields (based of the HeaderType selected)
    }

    public enum HeaderType
    {
        GenericHeader = 1       // Is the only type of header supported right now... This header is used in the PrintCentral application based on the "Queue Sorting" feature.
                                // Future header types might include specific headers required by processes in the Print Local system
    }

    public class PaperPrintConfiguration
    {
        public string OrderNumber;
        public string Company;
        public string Brand;
        public string Project;
        public string LabelPath;
        public string OutputFile;
        public PaperSize PaperSize;
        public LabelSide PrintSide;        // Only "Front" and "Back" settings are valid, specifying "Both" will result in error.
        public DPIResolution Resolution;
        public PageOrientation PageOrientation;
        public LabelRotation LabelRotation;
        public CutGuideStyle CutGuides;
        public int Columns;                // Specifies the number of columns (if Columns or Rows is left as 0, the system will automatically fill the entire page with as many labels as possible to fit)
        public int Rows;                   // Specifies the number of rows
        public List<LabelPrintDetail> Data;// The data of each label to be printed and the number of labels
        public List<LabelMapping> Mappings;// Maps label variables to fields found in the Data array
        public DocumentMargins Margins;    // Margins of the document
        public int HorizontalGap;          // Horizontal space between labels. Value is given in millimeters.
        public int VerticalGap;            // Vertical space between labels. Value is given in millimeters.
        public bool SerializedPrint;       // Should be set to true if the order will NOT be encoded with the "table" encoding software.
                                           // In this case, labels of different barcodes can appear on the same page and there is no table header/tail.
                                           // In the other hand, this should be set to false if the order will be encoded in the table.
                                           // In this case, the system will:
                                           //    - Ensure there will be space left on each page to print the header barcode.
                                           //    - Print full pages, even if the quantity would have required only half a page for instance.
    }

    public enum PaperSize
    {
        A0, A1, A2, A3, A4, A5,
        Letter, Legal, Ledger, Tabloid,
        RA0, RA1, RA2, RA3, RA4, RA5,
        B0, B1, B2, B3, B4, B5,
        Quarto, Foolscap, Executive, GovernmentLetter,
        Post, Crown, LargePost, Demy, Medium,
        Royal, Elephant, DoubleDemy, QuadDemy,
        STMT, Folio, Statement, Size10x14,
        ArchA, ArchB, ArchC, ArchD, ArchE
    }


    public static class PaperSizeRepo
    {
        private static List<PaperSizeInfo> sizes = new List<PaperSizeInfo>()
        {
            new PaperSizeInfo(){ Name = "A0", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "A1", Width = 2246, Height = 3178, WidthInches = 23.4, HeightInches = 33.1, WidthMM = 594, HeightMM = 841 },
            new PaperSizeInfo(){ Name = "A2", Width = 1584, Height = 2246, WidthInches = 16.5, HeightInches = 23.4, WidthMM = 420, HeightMM = 594 },
            new PaperSizeInfo(){ Name = "A3", Width = 1123, Height = 1584, WidthInches = 11.7, HeightInches = 16.5, WidthMM = 297, HeightMM = 420 },
            new PaperSizeInfo(){ Name = "A4", Width = 797, Height = 1123, WidthInches = 8.3, HeightInches = 11.7, WidthMM = 210, HeightMM = 297 },
            new PaperSizeInfo(){ Name = "A5", Width = 557, Height = 797, WidthInches = 5.8, HeightInches = 8.3, WidthMM = 148, HeightMM = 210 },
            new PaperSizeInfo(){ Name = "Letter", Width = 816, Height = 1056, WidthInches = 8.5, HeightInches = 11, WidthMM = 216, HeightMM = 279 },
            new PaperSizeInfo(){ Name = "Legal", Width = 816, Height = 1344, WidthInches = 8.5, HeightInches = 14, WidthMM = 216, HeightMM = 356 },
            new PaperSizeInfo(){ Name = "Ledger", Width = 1056, Height = 1632, WidthInches = 11, HeightInches = 17, WidthMM = 279, HeightMM = 432 },
            new PaperSizeInfo(){ Name = "Tabloid", Width = 1056, Height = 1632, WidthInches = 11, HeightInches = 17, WidthMM = 279, HeightMM = 432 },

			// =======> Needs update
			new PaperSizeInfo(){ Name = "RA0", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "RA1", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "RA2", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "RA3", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "RA4", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "RA5", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B0", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B1", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B2", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B3", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B4", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "B5", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Quarto", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Foolscap", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Executive", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "GovernmentLetter", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Post", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Crown", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "LargePost", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Demy", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Medium", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Royal", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Elephant", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "DoubleDemy", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "QuadDemy", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "STMT", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Folio", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Statement", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Size10x14", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Arch A", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Arch B", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Arch C", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Arch D", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
            new PaperSizeInfo(){ Name = "Arch E", Width = 3178, Height = 4493, WidthInches = 33.1, HeightInches = 46.8, WidthMM = 841, HeightMM = 1189 },
			// <=======

		};

        public static PaperSizeInfo GetSize(PaperSize size)
        {
            return sizes[(int)size];
        }
    }


    public enum CutGuideStyle
    {
        None,
        OnPageMargins,
        OnLabelMargins
    }


    public class PaperSizeInfo
    {
        public string Name;
        public int Width;
        public int Height;
        public double WidthInches;
        public double HeightInches;
        public double WidthMM;
        public double HeightMM;
    }

    public enum DPIResolution
    {
        dpi96 = 96,
        dpi144 = 144,
        dpi150 = 150,
        dpi200 = 200,
        dpi300 = 300,
        dpi400 = 400,
        dpi600 = 600,
        dpi720 = 720,
        dpi1200 = 1200
    }

    public enum PageOrientation
    {
        Portrait,
        Landscape
    }

    public enum LabelRotation
    {
        r0 = 0,
        r90 = 90,
        r180 = 180,
        r270 = 270
    }

    public class DocumentMargins
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public DocumentMargins() { }

        public DocumentMargins(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }


    public class LabelPrintDetail
    {
        public int DetailID { get; set; }
        public int Quantity { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(1000);
            foreach(var kvp in Data)
            {
                sb.Append($"{kvp.Key}={kvp.Value}\r\n");
            }
            return sb.ToString();
        }
    }
}
