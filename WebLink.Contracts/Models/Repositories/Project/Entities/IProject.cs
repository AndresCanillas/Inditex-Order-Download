using Service.Contracts.Database;
using WebLink.Contracts.Models.Print;

namespace WebLink.Contracts.Models
{
    public interface IProject : IEntity, ICanRename, IBasicTracing
    {
        int BrandID { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string ProjectCode { get; set; }
        bool EnableFTPFolder { get; set; }
        string FTPFolder { get; set; }
        string FTPClients { get; set; }                        // JSON array containing the configuration of FTP clients (for each client configured here, we need to periodically connect to the specified FTP server to retrieve files)
        int? RFIDConfigID { get; set; }
        int? OrderWorkflowConfigID { get; set; }
        int? SLADays { get; set; }
        int? DefaultFactory { get; set; }                      // Default: null (Factory is not preassigned)

        string CustomerSupport1 { get; set; }                    // Costumer assigned to this project (for emails & notifications). If null then check Companies for company wide defaults; if that is null too, then no email / notification can be sent to this role.
        string CustomerSupport2 { get; set; }                    // Backup in case main costumer is not available, if set, then both will receive notification and emails.
        string ClientContact1 { get; set; }                      // Main responsible for this project on the client side (for emails & notifications). If null then we check Companies for company wide defaults; if that is null too, then an error notification stating that no responsible has been setup for this project/company is sent to SysAdmin.
        string ClientContact2 { get; set; }                      // Backup responsible for this project on the client side. If null then only the Main responsible will be sent a notification.
        bool Hidden { get; set; }
        // Default: null (DeliveryDate is not preassigned)
        bool EnableValidationWorkflow { get; set; }            // Default false. If true, then orders processed with this project require validation before they are sent to production.
        bool AllowVariableDataEdition { get; set; }            // Default false. If true, then users can edit the variable data while validating an order, when creating a new order manually with the Print menu. NOTE: Users can still browse the system catalogs and make editions to the data there unless the catalogs themselves are configured as read only.
        int AllowQuantityEdition { get; set; }                // Default false. If true, then users can edit the quantities of the order during the validation process.
        int? MaxQuantityPercentage { get; set; }               // Default 5 (this is always a percentage, valid range is 0-100). IF not null, this limits the maximum value users can set for the Quantity field on each order detail. For instance if Quantity is 100 and MaxQuantityPercentage is 5%, then quantity can be set to a maximum of 105 during validation. If null then it means that this creteria is not to be used for limiting the value of the quantity filed.
        int? MaxQuantity { get; set; }                         // Default null (this is a hard limit to quantity, valid range is 0-999999). IF not null, this sets the maximum hard limit for the Quantity field on each order detail. This is enforced on top of MaxQuantityPercentage. If null, then this criteria will not be used to limit the quantity field.
        int AllowExtrasDuringValidation { get; set; }         // Default 0. If the value is diffent to 0, then users can add extras while validating an order.  1 is a maximun percentaje value, 2 is a maximun hard limit value
        int? MaxExtrasPercentage { get; set; }                 // Default 5 (this is always a percentage, valid range is 0-100). IF not null, this limits how many extras can be added while validating an order. If null it means that the system should not restrict the number of extras that can be added to the order based of a percentage (as long as AllowExtrasDuringValidation is true, you can add extras).
        int? MaxExtras { get; set; }                           // Default null (this is a hard limit to extras, valid range is 0-999999). IF not null, this sets the maximum hard limit for the Extras field on each order detail. This is enforced on top of MaxExtrasPercentage. If null, then this criteria will not be used to limit the extras field.
        bool AllowAddOrChangeComposition { get; set; }
        CITemplateConfig TemplateConfiguration { get; set; }
        bool AllowExceptions { get; set; }                      // Default false, allow to add exceptions care instructions while composition data is loaded
        bool EnableSectionWeight { get; set; }
        bool EnableShoeComposition { get; set; }                // never use, this options was move to the labels
        bool EnableAllLangs { get; set; }                       // never use
        [NoTrim]
        string SectionsSeparator { get; set; }                  // default '/'
        [NoTrim]
        string SectionLanguageSeparator { get; set; }           // default '/'
        [NoTrim]
        string FibersSeparator { get; set; }                    // default '\n'
        [NoTrim]
        string FiberLanguageSeparator { get; set; }             // default '/'
        [NoTrim]
        string CISeparator { get; set; }                        // default '/'
        [NoTrim]
        string CILanguageSeparator { get; set; }                // default '*'

        UpdateHandlerType UpdateType { get; set; }
        bool AllowOrderChangesAfterValidation { get; set; }    // Default false. If true, then orders can be updated even after being validated (sent to production), this also enables the Comparison Workflow to validate and create additional print jobs. If false, any attempt to change an order after it has been validated is denied with an error (messages and emails are sent accordingly in either case).

        bool EnableMultipleFiles { get; set; }              // never used

        bool TakeOrdersAsValid { get; set; }                // Default false. if project requires validtion workflow, orders will be mark as valid on reception
        string OrderSetValidatorPlugin { get; set; }          // method to set dynamically if order is take as valid or not, dev can set a custom logic here, default plugin is created  IOrderSetValidatorPlugin
        bool DisablePrintLocal { get; set; }                // Default false. Enable or disable to send order to PrintLocal App, only print from PrintCentral
        ProjectSetProductionType ProductionTypeStrategy { get; set; }           // Set ProductionType for all orders inner proyect defined by strategy selected
        string OrderPlugin { get; set; }
        string WizardCompositionPlugin { get; set; }
        bool AllowAdditionals { get; set; }           // Default false, allow to add additionals care instructions while composition data is loaded
        bool AllowMadeInCompoShoesFiber { get; set; } // This property could be remove, shoe Composition label it depend from label properties

        string CustomOrderDataReport { get; set; } // JSON String custom columns from VariableData Table to show a report with variable data for customer


        bool RequireItemAssignment { get; set; }

        bool IncludeFiles { get; set; }  //Default false. if project requires Include Files, configuration will be mark as valid
                                         // ijsanchezm begin PC_dev_I04
        bool IsApplyAutomaticPercentage { get; set; }
        bool AllowEditQuantity { get; set; }
        bool IsApplyAutomaticExtraIncrement { get; set; }
        bool AllowUserEditExtraQuantities { get; set; }
        MadeInEnable AllowUpdateMadeIn { get; set; }
        bool RemoveDuplicateTextFromComposition { get; set; }
        bool EnableOrderPool { get; set; }
        bool EnablePoolFile { get; set; }
        string PoolFileHandler { get; set; }
        DocumentDownloadOption DocumentPreviewDownloadOption { get; set; }

       bool HasCompoAudit { get; set; } 
       bool ForceOverwriteOrderOnManualValidation { get; set; }

        bool AllowQuantityZero { get; set; }
    }

}
