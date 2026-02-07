using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public interface ICompany : IEntity, ICanRename, IBasicTracing
    {
        string Name { get; set; }
        string CompanyCode { get; set; }
        int? MainLocationID { get; set; }
        string MainContact { get; set; }
        string MainContactEmail { get; set; }
        string Culture { get; set; }
        string IDTZone { get; set; }
        string GSTCode { get; set; }
        int? GSTID { get; set; }
        string ClientReference { get; set; }
        string Instructions { get; set; }
        int? SLADays { get; set; }
        int? DefaultProductionLocation { get; set; }
        int? DefaultDeliveryLocation { get; set; }
        bool ShowAsCompany { get; set; }
        string FtpUser { get; set; }
        string FtpPassword { get; set; }
        int? RFIDConfigID { get; set; }
        string OrderSort { get; set; }
        string HeaderFields { get; set; }
        string StopFields { get; set; }
        string CustomerSupport1 { get; set; }
        string CustomerSupport2 { get; set; }
        string ProductionManager1 { get; set; }
        string ProductionManager2 { get; set; }
        string ClientContact1 { get; set; }
        string ClientContact2 { get; set; }
        bool SyncWithSage { get; set; }
        string SageRef { get; set; }
        bool IsBroker { get; set; }
        bool? HasOrderWorkflow { get; set; }
    }

    public interface ILocation : IEntity, ICanRename, IBasicTracing
    {
        int CompanyID { get; set; }
        string Name { get; set; }
        string DeliverTo { get; set; }
        string AddressLine1 { get; set; }
        string AddressLine2 { get; set; }
        string CityOrTown { get; set; }
        string StateOrProvince { get; set; }
        string Country { get; set; }
        string ZipCode { get; set; }
        string FactoryCode { get; set; }
        int WorkingDays { get; set; }
        string Holidays { get; set; }
        string CutoffTime { get; set; }
        int? ProductionManager1 { get; set; }
        int? ProductionManager2 { get; set; }
        bool EnableERP { get; set; }            // Enable Invoice System for this factory, only support one ERP Invoice For all Factories, require update in future for multiple ERP Systems
        int CountryID { get; set; }
        string ERPCurrency { get; set; }		// Use this currency to register invoice into ERP System
        //Country Country { get; set; }
        int MaxNotEncodingQuantity { get; set; }
        string FscCode { get; set; }
    }

    public interface IPrinter : IEntity, ICanRename, IBasicTracing
    {
        string DeviceID { get; set; }
        string ProductName { get; set; }
        string Name { get; set; }
        string FirmwareVersion { get; set; }
        DateTime? LastSeenOnline { get; set; }
        int LocationID { get; set; }
        string DriverName { get; set; }
        string PrinterType { get; set; }
        bool SupportsCutter { get; set; }
        bool SupportsRFID { get; set; }
        bool IsRemote { get; set; }
        string IP { get; set; }
        string Port { get; set; }
    }

    public interface IPrinterSettings : IEntity, IBasicTracing
    {
        int PrinterID { get; set; }
        int ArticleID { get; set; }
        double XOffset { get; set; }
        double YOffset { get; set; }
        string Speed { get; set; }
        string Darkness { get; set; }
        bool Rotated { get; set; }
        bool ChangeOrientation { get; set; }
        bool PauseOnError { get; set; }
        bool EnableCut { get; set; }
        CutBehavior CutBehavior { get; set; }
        bool ResumeAfterCut { get; set; }
    }

    public enum CutBehavior
    {
        EachLabel,
        EachBarcode,
        EachStop
    }

    public interface IPrinterJob : IEntity
    {
        int CompanyID { get; set; }
        int CompanyOrderID { get; set; }
        int ProjectID { get; set; }
        int? ProductionLocationID { get; set; }
        int? AssignedPrinter { get; set; }
        int ArticleID { get; set; }
        int Quantity { get; set; }

        int Printed { get; set; }
        void IncPrinted();

        int Encoded { get; set; }
        void IncEncoded();

        int Errors { get; set; }
        void IncErrors();

        int Extras { get; set; }
        DateTime? DueDate { get; set; }
        JobStatus Status { get; set; }
        bool AutoStart { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime UpdatedDate { get; set; }
        DateTime? CompletedDate { get; set; }
        bool PrintPackageGenerated { get; set; }
    }

    public enum JobStatus
    {
        Pending = 1,
        Paused = 2,
        Executing = 3,
        Error = 4,
        Completed = 5,
        Cancelled = 6,
        Printed = 7
    }

    public interface IPrinterJobDetail : IEntity
    {
        int PrinterJobID { get; set; }
        int ProductDataID { get; set; }
        int Quantity { get; set; }
        int QuantityRequested { get; set; }
        [MaxLength(25)]
        string PackCode { get; set; }
        int Printed { get; set; }
        int Encoded { get; set; }
        int Errors { get; set; }
        int Extras { get; set; }
        DateTime UpdatedDate { get; set; }
    }

    public enum Zones
    {
        EUROPE,
        ASIA
    }

    public interface ICompanyProvider : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        int ProviderCompanyID { get; set; }
        string ClientReference { get; set; }
        string Instructions { get; set; }
        int? SLADays { get; set; }
        int? DefaultProductionLocation { get; set; }
        ICollection<Order> Orders { get; set; }
    }

    public interface IOrder : IEntity, IBasicTracing, IMapOrderWithSage
    {

        int CompanyID { get; set; }
        int ProjectID { get; set; }
        int OrderDataID { get; set; }           // dynamic db identifier
        string OrderNumber { get; set; }        // client order number
        string MDOrderNumber { get; set; }      // ERP Middleware order number
        DateTime OrderDate { get; set; }
        string UserName { get; set; }
        DocumentSource Source { get; set; }
        int Quantity { get; set; }
        ProductionType ProductionType { get; set; }
        int? AssignedPrinterID { get; set; }
        OrderStatus OrderStatus { get; set; }
        string OrderStatusText { get; }

        int? LocationID { get; set; }
        bool PreviewGenerated { get; set; }
        bool? PrintPackageGenerated { get; set; }
        string BillTo { get; set; }
        string SendTo { get; set; }
        int BillToCompanyID { get; set; }
        int SendToCompanyID { get; set; }
        int? SendToAddressID { get; set; }
        int? SendToLocationID { get; set; }     // TODO: SendToLocationID is probably the same as LocationID...  Use LocationID instead... Remove this later
        string ValidationUser { get; set; }
        DateTime? ValidationDate { get; set; }   // Date in which this order was validated
        DateTime? DueDate { get; set; }         // Date in which this order has to be delivered, this is automatically calculated based on the SLA

        int OrderGroupID { get; set; }
        int? ParentOrderID { get; set; }
        int? ProviderRecordID { get; set; }
        CompanyProvider Provider { get; set; }

        #region Flags
        bool ConfirmedByMD { get; set; }
        bool IsBilled { get; set; }             // PrintWeb dont Generate invoices, rename this property -> register to invoice
        bool IsBillable { get; set; }
        bool IsInConflict { get; set; }
        bool IsStopped { get; set; }
        bool DuplicatedEPC { get; set; }
        bool HasOrderWorkflow { get; set; }
        long? ItemID { get; set; }

        bool AllowRepeatedOrders { get; set; }

        DeliveryStatus DeliveryStatusID { get; set; }    
        #endregion Flags
    }

    public enum DocumentSource
    {
        NotSet = 0,
        Web = 1,
        FTP = 2,
        API = 3,
        Validation = 4,
        Repetition = 5
    }

    // Object copied into FTPFileWatcherService
    public enum ProductionType
    {
        All = 0,
        IDTLocation = 1,
        CustomerLocation = 2
    }


    // If you update enum members, update javascript enum is required OrderStatus var
    public enum OrderStatus
    {
        None = 0,
        Received = 1,       // order was received and is waiting to be processed (this is the state after uploading an order that is going to be produced by IDT)
        Processed = 2,      // Only For IDT Production: Order has been processed, the order has been picked up by the system deamon and sent to MD (for billing) and the corresponding print jobs have been created (not assigned to any production facility yet). 
        Printing = 3,       // Order is printing... For local production, this is the initial state. For IDT production, it means that a printing location and printer have been assigned to at least one of the print jobs associated with the order.
        Completed = 6,      // Order is complete... For Local Production, this state is assigned automatically when all print jobs associated to the order are completed. For IDT production, this state is assigned manually by operators when they get confirmation of delivery.
        Cancelled = 7,      // Order was cancelled and no longer needs to be produced or sent to the client
        InFlow = 20,
        Validated = 30,     // Order has been validated
        CompoAuditNeeded = 31, 
        Billed = 40,
        ProdReady = 50
    }

    public enum ReportType
    {
        Detailed = 1,
        Grouped = 2
    }

    public interface IOrderDetail : IEntity
    {
        string ArticleCode { get; set; }
        string PackCode { get; set; }
        int Quantity { get; set; }
        int Product { get; set; }
    }


    public interface IContact : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        string PhoneNumber { get; set; }
        string MobileNumber { get; set; }
        string Comments { get; set; }
    }

    public interface IAddress : IEntity, IBasicTracing
    {
        string Name { get; set; }
        string AddressLine1 { get; set; }
        string AddressLine2 { get; set; }
        string CityOrTown { get; set; }
        string StateOrProvince { get; set; }
        string Country { get; set; }
        int CountryID { get; set; }
        string ZipCode { get; set; }
        string Notes { get; set; }
        bool Default { get; set; }
        bool SyncWithSage { get; set; }
        string SageRef { get; set; }
        string AddressLine3 { get; set; }
        string SageProvinceCode { get; set; }
        string Telephone1 { get; set; }
        string Telephone2 { get; set; }
        string Email1 { get; set; }
        string Email2 { get; set; }
        string BusinessName1 { get; set; }
        string BusinessName2 { get; set; }
    }

    public interface ICompanyAddress : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        int AddressID { get; set; }
    }

    public interface IProviderBillingsInfo : IEntity
    {
        int ProviderID { get; set; }
        int BillingInfoID { get; set; }
    }

    public interface IBillingInfo : IEntity, IBasicTracing
    {
        string Name { get; set; }
    }

    public interface IPack : IEntity, ICanRename, IBasicTracing
    {
        int ProjectID { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string PackCode { get; set; }
    }

    public interface IPackArticle : IEntity, IBasicTracing
    {
        int PackID { get; set; }
        int? ArticleID { get; set; }
        int Quantity { get; set; }
        PackArticleType Type { get; set; }
        string FieldName { get; set; }
        string Mapping { get; set; }
        string PluginName { get; set; }

        bool AllowEmptyValues { get; set; }
    }

    public interface IArticle : IEntity, ICanRename, IBasicTracing
    {
        int? ProjectID { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string ArticleCode { get; set; }
        string BillingCode { get; set; }
        int? LabelID { get; set; }
        string Instructions { get; set; }
        int? CategoryID { get; set; }
        bool SyncWithSage { get; set; }
        string SageRef { get; set; }
        bool EnableLocalPrint { get; set; }
        bool EnableConflicts { get; set; }
        Guid PrintCountSequence { get; set; }
        PrintCountSequenceType PrintCountSequenceType { get; set; }
        string PrintCountSelectorField { get; set; }
        SelectorType PrintCountSelectorType { get; set; }
        bool EnableAddItems { get; set; }
        string ExportBlockedLocationIds { get; set; }
    }
    public interface IArticleTracking : IEntity
    {
        int ArticleID { get; set; }
        DateTime InitialDate { get; set; }
        string LastUpdateUserName { get; set; }
    }

    public enum LabelType
    {
        Sticker = 1,
        CareLabel = 2,
        HangTag = 3,
        PiggyBack = 4
    }


    public interface IMaterial : IEntity, ICanRename, IBasicTracing
    {
        string Name { get; set; }
        string Properties { get; set; }
        bool ShowAsMaterial { get; set; }
    }


    public interface ILabelData : IEntity, ICanRename, IBasicTracing
    {
        int? ProjectID { get; set; }
        string FileName { get; set; }
        string Name { get; set; }
        string Comments { get; set; }
        bool EncodeRFID { get; set; }
        bool DoubleSide { get; set; }
        bool IsSerialized { get; set; }
        string PreviewData { get; set; }
        LabelType Type { get; set; }
        int? MaterialID { get; set; }
        bool? LabelsAcross { get; set; }
        int? Rows { get; set; }
        int? Cols { get; set; }
        string Mappings { get; set; }
        string GroupingFields { get; set; }
        bool? RequiresDataEntry { get; set; }
        bool DenyPartialExport { get; set; }
        bool ShoeComposition { get; set; }/*
		bool IsForComposition { get; set; }*/
        bool IsDataBound { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        bool IncludeComposition { get; set; }
        string UpdatedFileBy { get; set; }
        DateTime UpdatedFileDate { get; set; }
        bool IncludeCareInstructions { get; set; }
        string IsValidBy { get; set; }
        DateTime IsValidDate { get; set; }
        bool IsValid { get; set; }
    }


    public interface IArtifact : IEntity, IBasicTracing
    {
        int? ArticleID { get; set; } // belongTo
        int? LabelID { get; set; }
        // TODO: could be int16 maybe unsigned
        // will be need a manual for explain layerlevel term
        int LayerLevel { get; set; }
        // top, bottom, left, right, top right, top left, bottom right, bottom leff
        // t,b,l,r, tr, tl, br, bl
        string Location { get; set; }
        string Name { get; set; }
        // sequence position, ordered, sort
        int Position { get; set; }
        bool SyncWithSage { get; set; }
        [MaxLength(16)]
        string SageRef { get; set; }

        bool EnablePreview { get; set; }
        bool IsTail { get; set; }
        bool IsHead { get; set; }
        [MaxLength(2000)]
        string Description { get; set; }
        bool IsMain { get; set; }
    }

    public interface IRFIDConfig : IEntity, IBasicTracing
    {
        string SerializedConfig { get; set; }
    }

    public interface IOrderWorkflowConfig : IEntity, IBasicTracing
    {
        string SerializedConfig { get; set; }
    }

    public interface IBrand : IEntity, ICanRename, IBasicTracing
    {
        int CompanyID { get; set; }
        string Name { get; set; }
        byte[] Icon { get; set; }
        bool EnableFTPFolder { get; set; }
        string FTPFolder { get; set; }
        int? RFIDConfigID { get; set; }
    }




    public interface IGroupFileColumn : IEntity, IBasicTracing
    {

        int ProjectId { get; set; }

        string TableName { get; set; }

        string Key { get; set; }

    }


    public interface IProjectImage : IEntity, IBasicTracing
    {


        string Name { get; set; }
        string Description { get; set; }
        int? ProjectID { get; set; }
        string Extension { get; set; }
        ImageMetadata UserMetaData { get; set; }

    }


    public interface IEncodedLabel
    {
        int ID { get; set; }
        int DeviceID { get; set; }
        int CompanyID { get; set; }
        int ProjectID { get; set; }
        int OrderID { get; set; }
        string ArticleCode { get; set; }
        string Barcode { get; set; }
        int ProductionType { get; set; }
        int ProductionLocationID { get; set; }
        long Serial { get; set; }
        string TID { get; set; }
        string EPC { get; set; }
        string AccessPassword { get; set; }
        string KillPassword { get; set; }
        float RSSI { get; set; }
        bool Success { get; set; }
        string ErrorCode { get; set; }
        DateTime Date { get; set; }
        int? InlayConfigID { get; set; }
        string InlayConfigDescription { get; set; }
        SyncState SyncState { get; set; }
    }

    public interface INotification : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        NotificationType Type { get; set; }
        string IntendedRole { get; set; }
        string IntendedUser { get; set; }
        string NKey { get; set; }
        string Source { get; set; }
        string Title { get; set; }
        string Message { get; set; }
        string Data { get; set; }
        bool AutoDismiss { get; set; }
        int Count { get; set; }
        string Action { get; set; }
        int? LocationID { get; set; }
        int? ProjectID { get; set; }
    }

    public enum NotificationType
    {
        All = 0,
        Error = 1,                          // Notifications of this type are used to inform of errors in any internal processes that would go unnoticed otherwise
        OrderTracking = 2,                  // Encloses notifications related to the order processing workflow.
        OrderImportError,                   // Order Import Process
        FTPFileWhatcher                     // Ftp File Whatcher Service

    }

    // This class defines different values to be used for the "Source" field found in notifications. This field is just for user reference, and can contain any string.
    // However, for the most part we should try to always set the Source field to one of the sources defined here.
    public class NotificationSources
    {
        public const string SystemMessages = "System Messages";
        public const string ProcessManager = "Process Manager";
        public const string MDConfirmation = "MD Confirmation";
        public const string OrderProcessingStage1 = "Order Processing Stage 1";
        public const string OrderProcessingStage2 = "Order Processing Stage 2";
        public const string OrderProcessingStage3 = "Order Processing Stage 3";
        public const string CreateOrderPreview = "Create Order Preview";
        public const string CreatePrintPackage = "Create Print Package";
        public const string EmailService = "Email Service";
        public const string SageService = "SageService";

        public static IEnumerable<string> Enumerate()
        {
            return new string[]
            {
                SystemMessages, ProcessManager, MDConfirmation, OrderProcessingStage1, OrderProcessingStage2, OrderProcessingStage3, CreateOrderPreview, EmailService
            };
        }
    }

    public class ExceptionInfo
    {
        public ExceptionInfo() { }
        public ExceptionInfo(Exception ex, object data)
        {
            Error = ex.Message;
            ExceptionType = ex.GetType().Name;
            StackTrace = ex.StackTrace;
            AttachedData = data;
        }
        public ExceptionInfo(object data)
        {
            Error = null;
            ExceptionType = null;
            StackTrace = null;
            AttachedData = data;
        }

        public string Error { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
        public object AttachedData { get; set; }
    }


    public interface IAppUser
    {
        string Id { get; set; }
        string UserName { get; set; }
        int? CompanyID { get; set; }
        int? SelectedCompanyID { get; set; }
        int? SelectedBrandID { get; set; }
        int? SelectedProjectID { get; set; }
        int? LocationID { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        string PhoneNumber { get; set; }
        string Language { get; set; }
        bool ShowAsUser { get; set; }
        bool LockoutEnabled { get; set; }
        DateTimeOffset? LockoutEnd { get; set; }
    }

    public interface IAppRole
    {
        string Name { get; set; }
    }

    public interface IResetToken
    {
        string ID { get; set; }
        string UserName { get; set; }
        DateTime ValidUntil { get; set; }
    }


    public interface IDataImportMapping : IEntity
    {
        int ProjectID { get; set; }
        string Name { get; set; }
        int RootCatalog { get; set; }
        string SourceType { get; set; }
        string FileNameMask { get; set; }
        string SourceCulture { get; set; }
        string Encoding { get; set; }
        string LineDelimiter { get; set; }
        char? ColumnDelimiter { get; set; }
        string QuotationChar { get; set; }
        bool IncludeHeader { get; set; }
        string Plugin { get; set; }
    }

    public interface IDataImportColMapping : IEntity
    {
        int? DataImportMappingID { get; set; }
        int ColOrder { get; set; }
        string InputColumn { get; set; }
        bool? Ignore { get; set; }
        int? Type { get; set; }
        bool? IsFixedValue { get; set; }
        string FixedValue { get; set; }
        int? MaxLength { get; set; }
        int? MinLength { get; set; }
        long? MinValue { get; set; }
        long? MaxValue { get; set; }
        DateTime? MinDate { get; set; }
        DateTime? MaxDate { get; set; }
        string DateFormat { get; set; }
        int? DecimalPlaces { get; set; }
        int? Function { get; set; }
        string FunctionArguments { get; set; }
        bool? CanBeEmpty { get; set; }
        string TargetColumn { get; set; }
    }


    public interface IProcessMappings
    {
        IDataImportMapping Data { get; }
        List<DocumentColMapping> Columns { get; }
    }

    public interface ICatalog : IEntity, IBasicTracing
    {
        int ProjectID { get; set; }
        int CatalogID { get; set; }
        string Name { get; set; }
        string Captions { get; set; }
        string Definition { get; set; }
        int SortOrder { get; set; }
        bool IsSystem { get; set; }
        bool IsHidden { get; set; }
        bool IsReadonly { get; set; }
        CatalogType CatalogType { get; set; }
        string RequiredRoles { get; set; }
        string TableName { get; }
        IList<FieldDefinition> Fields { get; }

    }


    public interface ICatalogData : IEntity, IBasicTracing
    {
        int CatalogID { get; set; }
        string Data { get; set; }
    }


    public interface IImageData : IEntity
    {
        int CompanyID { get; set; }
    }

    public interface ICategory : IEntity, ICanRename, IBasicTracing
    {
        int ProjectID { get; set; }
        string Name { get; set; }
    }

    public interface ICountry : IEntity
    {
        string Name { get; set; }
        string Alpha2 { get; set; }
        string Alpha3 { get; set; }
        string NumericCode { get; set; }
    }

    public interface IEmailToken
    {
        int ID { get; set; }
        string Code { get; set; }
        string UserId { get; set; }
        EmailType Type { get; set; }
    }

    public interface IEmailTokenItem
    {
        int ID { get; set; }
        int EmailTokenID { get; set; }
        int OrderID { get; set; }
        bool Notified { get; set; }
        DateTime? NotifyDate { get; set; }
        bool Seen { get; set; }
        DateTime? SeenDate { get; set; }
    }

    public interface IEmailTokenItemError
    {
        int ID { get; set; }
        int EmailTokenID { get; set; }
        string TokenKey { get; set; }
        string Title { get; set; }
        string Message { get; set; }
        int? LocationID { get; set; }
        int? ProjectID { get; set; }
        bool Notified { get; set; }
        DateTime? NotifyDate { get; set; }
        bool Seen { get; set; }
        DateTime? SeenDate { get; set; }
        //ErrorNotificationType TokenType { get; set; }
    }

    public interface IEmailServiceSettings
    {
        int ID { get; set; }
        string UserID { get; set; }
        bool NotifyOrderReceived { get; set; }          // Flag indicating if this user is interested in receiving notifications about received orders.
        bool NotifyOrderPendingValidation { get; set; } // Flag indicating if this user is interested in receiving notifications about pending orders.
        bool NotifyOrderValidated { get; set; }         // Flag indicating if this user is interested in receiving notifications about validated orders.
        bool NotifyOrderConflict { get; set; }          // Flag indicating if this user is interested in receiving notifications about order conflicts.
        bool NotifyOrderReadyForProduction { get; set; }// Flag indicating if this user is interested in receiving notifications about orders ready for production.
        bool NotifyOrderCompleted { get; set; }         // Flag indicating if this user is interested in receiving notifications about completed orders.
        int NotificationPeriodInDays { get; set; }      // Number of days between consecutive communications. Valid range: 1 - 7. This will allow to space out email notifications by up to a week (this is per type of email).
        bool NotifyOrderProcesingErrors { get; set; }   // Flag indicating if this user is interested in receiving notifications about Order Processing Errors.
        bool NotifyOrderCancelled { get; set; }   // Flag indicating if this user is interested in receiving notifications about Order Processing Errors.
        bool NotifyOrderPoolUpdate { get; set; }
    }



    public interface IArticleDetail : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        int ArticleID { get; set; }
    }

    public interface IOrderPool : IEntity
    {
        int ProjectID { get; set; }
        string OrderNumber { get; set; }
        string Seasson { get; set; }
        int Year { get; set; }
        string ProviderCode1 { get; set; }
        string ProviderName1 { get; set; }
        string ProviderCode2 { get; set; }
        string ProviderName2 { get; set; }
        string Size { get; set; }
        string ArticleCode { get; set; }
        string CategoryCode1 { get; set; }
        string CategoryCode2 { get; set; }
        string CategoryCode3 { get; set; }
        string CategoryCode4 { get; set; }
        string CategoryCode5 { get; set; }
        string CategoryCode6 { get; set; }
        string CategoryText1 { get; set; }
        string CategoryText2 { get; set; }
        string CategoryText3 { get; set; }
        string CategoryText4 { get; set; }
        string CategoryText5 { get; set; }
        string CategoryText6 { get; set; }
        string ColorCode { get; set; }
        string ColorName { get; set; }
        DateTime CreationDate { get; set; }
        string DeletedBy { get; set; }
        DateTime? DeletedDate { get; set; }
        DateTime? ExpectedProductionDate { get; set; }
        DateTime? LastUpdatedDate { get; set; }
        string Price1 { get; set; }
        string Price2 { get; set; }
        string ProcessedBy { get; set; }
        DateTime? ProcessedDate { get; set; }
        int Quantity { get; set; }
    }

    public interface IArticleCompositionConfig : IEntity, IBasicTracing
    {
        int CompanyID { get; set; }
        int ProjectID { get; set; }
        int ArticleID { get; set; }
        string ArticleCode { get; set; }
    }

    public enum DeliveryStatus
    {
        NotSet = 0,
        Pending = 1,          // Order is pending delivery
        PartiallyShipped = 2, // Only part of the order has been shipped
        Shipped = 3,          // Order has been shipped
        Delivered = 4,        // Order has been delivered and confirmed 
    }

}
