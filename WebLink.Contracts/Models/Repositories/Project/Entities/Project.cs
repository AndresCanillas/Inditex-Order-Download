using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models.Print;
namespace WebLink.Contracts.Models
{
    public class Project : IProject, ICompanyFilter<Project>, ISortableSet<Project>
    {

        public const string FILE_CONTAINER_NAME = "ProjectContainer";

        #region IEntity
        public int ID { get; set; }
        #endregion

        #region IBasicTracing
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracing


        public int BrandID { get; set; }

        public Brand Brand { get; set; }

        [MaxLength(35)]
        public string Name { get; set; }

        [MaxLength(4000)]
        public string Description { get; set; }

        [MaxLength(20)]
        public string ProjectCode { get; set; }

        public bool EnableFTPFolder { get; set; }

        public string FTPFolder { get; set; }

        public string FTPClients { get; set; }                        // JSON array containing the configuration of FTP clients (for each client configured here, we need to periodically connect to the specified FTP server to retrieve files)

        public int? RFIDConfigID { get; set; }

        public int? OrderWorkflowConfigID { get; set; }

        public RFIDConfig RFIDConfig { get; set; }

        public int? SLADays { get; set; }                             // Default: null (DeliveryDate is not preassigned)

        public int? DefaultFactory { get; set; }                      // Default: null (Factory is not preassigned)

        // Configuration used to download orders for this project from client hosted FTP servers


        // Determines project responsibles
        public string CustomerSupport1 { get; set; }                    // Costumer assigned to this project (for emails & notifications). If null then check Companies for company wide defaults; if that is null too, then no email / notification can be sent to this role.
        public string CustomerSupport2 { get; set; }                    // Backup in case main costumer is not available, if set, then both will receive notification and emails.
        public string ClientContact1 { get; set; }                      // Main responsible for this project on the client side (for emails & notifications). If null then we check Companies for company wide defaults; if that is null too, then an error notification stating that no responsible has been setup for this project/company is sent to SysAdmin.
        public string ClientContact2 { get; set; }                      // Backup responsible for this project on the client side. If null then only the Main responsible will be sent a notification.
        public bool Hidden { get; set; }

        // The following fields determine how order validation will operate in this project
        #region workflow config options

        public bool EnableValidationWorkflow { get; set; }            // Default false. If true, then orders processed with this project require validation before they are sent to production.

        // TODO: verificar si esta propiedad es necesaria, ya que se cruza con la opcion tipo de actualización permitida
        // https://smartdots.visualstudio.com/DESARROLLO/_workitems/edit/1398 y multiplicaria los casos


        // ???: esto se acordo que iba a determinarse de forma automatica  agregando configuraion en las etiquetas
        // Si requiren composicion o no.  La data variable, por ahora es compo y cantidades, no se si se refiere a que el cliente pueda editar los catalogo de data variable
        public bool AllowVariableDataEdition { get; set; }            // Default false. If true, then users can edit the variable data while validating an order, when creating a new order manually with the Print menu. NOTE: Users can still browse the system catalogs and make editions to the data there unless the catalogs themselves are configured as read only.
        public int AllowQuantityEdition { get; set; }                // Default 0. If value is different to 0, then users can edit the quantities of the order during the validation process. 1 is a maximun percentaje value, 2 is a hard limit value
        public int? MaxQuantityPercentage { get; set; }               // Default 5 (this is always a percentage, valid range is 0-100). IF not null, this limits the maximum value users can set for the Quantity field on each order detail. For instance if Quantity is 100 and MaxQuantityPercentage is 5%, then quantity can be set to a maximum of 105 during validation. If null then it means that this creteria is not to be used for limiting the value of the quantity filed.
        public int? MaxQuantity { get; set; }                         // Default null (this is a hard limit to quantity, valid range is 0-999999). IF not null, this sets the maximum hard limit for the Quantity field on each order detail. This is enforced on top of MaxQuantityPercentage. If null, then this criteria will not be used to limit the quantity field.
        public int AllowExtrasDuringValidation { get; set; }         // Default 0. If the value is diffent to 0, then users can add extras while validating an order.  1 is a maximun percentaje value, 2 is a maximun hard limit value
        public int? MaxExtrasPercentage { get; set; }                 // Default 5 (this is always a percentage, valid range is 0-100). IF not null, this limits how many extras can be added while validating an order. If null it means that the system should not restrict the number of extras that can be added to the order based of a percentage (as long as AllowExtrasDuringValidation is true, you can add extras).
        public int? MaxExtras { get; set; }                           // Default null (this is a hard limit to extras, valid range is 0-999999). IF not null, this sets the maximum hard limit for the Extras field on each order detail. This is enforced on top of MaxExtrasPercentage. If null, then this criteria will not be used to limit the extras field.
        public bool AllowAddOrChangeComposition { get; set; }
        public bool TakeOrdersAsValid { get; set; }                 // Default false. if project requires validtion workflow, orders will be mark as valid on reception
        public string OrderSetValidatorPlugin { get; set; }          // method to set dynamically if order is take as valid or not, dev can set a custom logic here, default plugin is created  IOrderSetValidatorPlugin
        public CITemplateConfig TemplateConfiguration { get; set; }
        public bool AllowExceptions { get; set; }
        public bool EnableSectionWeight { get; set; }
        public bool EnableShoeComposition { get; set; }
        public bool EnableAllLangs { get; set; }
        [NoTrim]
        public string SectionsSeparator { get; set; }
        [NoTrim]
        public string SectionLanguageSeparator { get; set; }
        [NoTrim]
        public string FibersSeparator { get; set; }
        [NoTrim]
        public string FiberLanguageSeparator { get; set; }
        [NoTrim]
        public string CISeparator { get; set; }
        [NoTrim]
        public string CILanguageSeparator { get; set; }
        public string WizardCompositionPlugin { get; set; }
        public bool AllowAdditionals { get; set; }
        public bool AllowMadeInCompoShoesFiber { get; set; }
        public bool IncludeFiles { get; set; }  //Default false. if project requires Include Files, configuration will be mark as valid
        // ijsanchezm begin PC_dev_I04
        public bool IsApplyAutomaticPercentage { get; set; } = false;
        public bool AllowEditQuantity { get; set; } = true;

        public bool IsApplyAutomaticExtraIncrement { get; set; } = false;
        public bool AllowUserEditExtraQuantities { get; set; } = true;
        // ijsanchezm end
        public bool RequireItemAssignment { get; set; }

        #endregion workflow config options

        #region order updates

        public UpdateHandlerType UpdateType { get; set; }

        public bool AllowOrderChangesAfterValidation { get; set; }    // Default false. If true, then orders can be updated even after being validated (sent to production), this also enables the Comparison Workflow to validate and create additional print jobs. If false, any attempt to change an order after it has been validated is denied with an error (messages and emails are sent accordingly in either case).

        public bool EnableMultipleFiles { get; set; }

        // Inicialmente seleccionar una columna o mapping - pero que se puedan seleccionar varios enel futuro
        // Que sea una asocicacion de base de datos, en caso de que modifiquen el catalogo estaria creando inconsistencias
        // Esta puede ser una relacion virtual o una tabla fisica
        // TODO: falta agregar a la interfaz
        //public List<string> GroupFileColums { get; set; }

        #endregion order updates

        public bool DisablePrintLocal { get; set; }                     // Default false. Enable or disable to send order to PrintLocal App, only print from PrintCentral
        public ProjectSetProductionType ProductionTypeStrategy { get; set; }        // Default 0 -> Set IDT Factory.  Set ProductionType for all orders inner proyect defined by strategy selected
        public string OrderPlugin { get; set; }

        public string CustomOrderDataReport { get; set; } = "{}"; // JSON String custom columns from VariableData Table
        public MadeInEnable AllowUpdateMadeIn { get; set; }

        public bool RemoveDuplicateTextFromComposition { get; set; }

        public bool EnableOrderPool { get; set; }
        public string OrderPoolFileProcessor { get; set; }
        public bool EnablePoolFile { get; set; }
        public string PoolFileHandler { get; set; }
        public DocumentDownloadOption DocumentPreviewDownloadOption { get; set; }
        public bool HasCompoAudit { get; set; }
        public bool ForceOverwriteOrderOnManualValidation { get ; set ; }

        public bool AllowQuantityZero { get; set; }

        #region methods

        public void Rename(string name) => Name = name;

        public int GetCompanyID(PrintDB db) =>
            (from b in db.Brands
             where b.ID == BrandID
             select b.CompanyID).Single();

        public async Task<int> GetCompanyIDAsync(PrintDB db) => await
            (from b in db.Brands
             where b.ID == BrandID
             select b.CompanyID).SingleAsync();

        public IQueryable<Project> FilterByCompanyID(PrintDB db, int companyid) =>
            from p in db.Projects
            join b in db.Brands on p.BrandID equals b.ID
            where b.CompanyID == companyid
            select p;

        public IQueryable<Project> ApplySort(IQueryable<Project> qry) => qry.OrderBy(p => p.Name);






        #endregion methods
    }

    public enum UpdateHandlerType
    {
        NotAllow,
        Auto,
        RequestConfirm,
        UseMergeTool,
        AlwaysNew

    }
}
