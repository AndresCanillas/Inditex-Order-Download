using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebLink.Contracts
{
	public interface IUserFeatures
	{
		bool CanSelectCompany { get; }			// Determines if the user can use the option to select another company.
		bool CanSelectBrandProject { get; }		// Determines if the user can use the option to select a brand and project.
		bool CanSeeCompanyFilter { get; }		// Determines if the user can see data from other companies in most reports and views.

		bool CanSeeVMenu { get; }				// Determines if the user can see any options in the Vertical Menu

		bool CanSeeVMenu_UploadMenu { get; }        // Determines if the user can see the "Upload" option in the vertical menu
		bool CanSeeVMenu_UploadOrder { get; }		// Determines if the user can see the option to upload orders within the "Upload" menu
		bool CanSeeVMenu_UploadData { get; }        // Determines if the user can see the option to upload catalog data within the "Upload" menu
		bool CanSeeVMenu_UploadPoolFile { get; }       // Determines if the user can see the option to upload the order pool file within the "Upload" menu
		bool CanSeeVMenu_UploadOrdersReport { get; }// Determines if the user can see the uploaded orders report within the "Upload" menu

		bool CanSeeVMenu_PrintMenu { get; }      // Determines if the user can see the "Print" option in the vertical menu
		bool CanSeeVMenu_PrintLabels { get; }    // Determines if the user can see the "Print Labels" option within the "Print" menu
		bool CanSeeVMenu_PrintJobsReport { get; }// Determines if the user can see the "Pending Jobs" option within the "Print" menu
		bool CanSeeVMenu_Printers { get; }       // Determines if the user can see the "Printers" option within the "Print" menu

		bool CanSeeVMenu_Admin { get; }          // Determines if the user can see the "Administration" option within the vertical menu
		bool CanSeeVMenu_AdminBrands { get; }    // Determines if the user can see the "Brands" option within the "Administration" vertical menu
		bool CanSeeVMenu_AdminArticles { get; }  // Determines if the user can see the "List of Articles" option within the "Administration" vertical menu
		bool CanSeeVMenu_AdminLocations { get; } // Determines if the user can see the "Manage Locations" option within the "Administration" vertical menu
		bool CanSeeVMenu_AdminUsers { get; }     // Determines if the user can see the "Manage Users" option within the "Administration" vertical menu

		bool CanSeeMainAdminMenu { get; }					// Determines if the user can see the "Administration" option within the top right menu that leads to the main Administration page
		bool Admin_Companies_CanSee { get; }				// Determines if the user can see all the companies (true) or only his/her own company (false) within the administration treeview
		bool Admin_Companies_CanAdd { get; }				// Determines if the user can Add new companies within the administration treeview
		bool Admin_Companies_CanEdit { get; }               // Determines if the user can Edit existing companies within the administration treeview
		bool Admin_Companies_CanRename { get; }             // Determines if the user can rename existing companies within the administration treeview
		bool Admin_Companies_CanDelete { get; }             // Determines if the user can delete existing companies within the administration treeview
		bool Admin_Companies_CanEditLogo { get; }           // Determines if the user can change the company logo while editing a company
		bool Admin_Companies_CanEditMDSettings { get; }     // Determines if the user can edit the MD related fields while editing a company
		bool Admin_Companies_CanEditProductionSettings { get; }// Determines if the user can edit Production related fields while editing a company
		bool Admin_Companies_CanEditProviders { get; }      // Determines if the user can edit the list of providers while editing a company
		bool Admin_Companies_CanEditFTPSettings { get; }    // Determines if the user can edit the ftp account while editing a company
		bool Admin_Companies_CanEditRFIDSettings { get; }   // Determines if the user can edit the rfid settings while editing a company

		bool Admin_Locations_CanSee { get; }				// Determines if the user can see the locations within the administration treeview
		bool Admin_Locations_CanAdd { get; }                // Determines if the user can create new locations within the administration treeview
		bool Admin_Locations_CanEdit { get; }               // Determines if the user can edit existing locations within the administration treeview
		bool Admin_Locations_CanRename { get; }               // Determines if the user can edit existing locations within the administration treeview
		bool Admin_Locations_CanDelete { get; }             // Determines if the user can delete locations within the administration treeview
		bool Admin_Locations_CanEditMDSettings { get; }     // Determines if the user can edit MD related fields while editing a location

		bool Admin_Printers_CanSee { get; }                 // Determines if the user can see the printers within the administration treeview
		bool Admin_Printers_CanAdd { get; }                 // Determines if the user can create new printers within the administration treeview
		bool Admin_Printers_CanEdit { get; }                // Determines if the user can edit existing printers within the administration treeview. If can see is true but can edit is false, the view should appear as readonly
		bool Admin_Printers_CanRename { get; }              // Determines if the user can edit existing printers within the administration treeview. If can see is true but can edit is false, the view should appear as readonly
		bool Admin_Printers_CanDelete { get; }              // Determines if the user can delete printers within the administration treeview
		bool Admin_Printers_CanChangeCompany { get; }       // Determines if the user can execute the Change Company action while editing a printer
		bool Admin_Printers_CanSendCommand { get; }         // Determines if the user can execute the send command action while editing a printer

		bool Admin_Brands_CanSee { get; }					// Determines if the user can see the brands within the administration treeview
		bool Admin_Brands_CanAdd { get; }                   // Determines if the user can create new brands within the administration treeview
		bool Admin_Brands_CanEdit { get; }                  // Determines if the user can edit existing brands within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Brands_CanRename { get; }                  // Determines if the user can edit existing brands within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Brands_CanDelete { get; }                // Determines if the user can delete brands within the administration treeview
		bool Admin_Brands_CanEditLogo { get; }				// Determines if the user can edit the logo of the brand while editing it
		bool Admin_Brands_CanEditFTPSettings { get; }       // Determines if the user can edit the FTP settings of the brand while editing it
		bool Admin_Brands_CanEditRFIDSettings { get; }      // Determines if the user can edit the RFID settings of the brand while editing it

        bool Admin_Projects_CanSee { get; }                 // Determines if the user can see the projects within the administration treeview
		bool Admin_Projects_CanAdd { get; }                 // Determines if the user can create new projects within the administration treeview
		bool Admin_Projects_CanEdit { get; }                // Determines if the user can edit existing projects within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Projects_CanRename { get; }                // Determines if the user can edit existing projects within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Projects_CanDelete { get; }              // Determines if the user can delete projects within the administration treeview
		bool Admin_Projects_CanEditLogo { get; }            // Determines if the user can edit the logo of the project while editing it
		bool Admin_Projects_CanEditFTPSettings { get; }     // Determines if the user can edit the FTP settings of the project while editing it
		bool Admin_Projects_CanEditRFIDSettings { get; }    // Determines if the user can edit the RFID settings of the project while editing it
		bool Admin_Projects_CanAddImages { get; }           // User can add images to project
		bool Admin_Projects_CanSeeImages { get; }
		bool Admin_Projects_CanEditImages { get; }
		bool Admin_Projects_CanDeleteImages { get; }

		bool Admin_Packs_CanSee { get; }                    // Determines if the user can see the packs option within the administration treeview
		bool Admin_Packs_CanAdd { get; }                    // Determines if the user can create new packs within the administration treeview
		bool Admin_Packs_CanEdit { get; }                   // Determines if the user can edit existing packs within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Packs_CanRename { get; }                   // Determines if the user can edit existing packs within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Packs_CanDelete { get; }                 // Determines if the user can delete packs within the administration treeview
		bool Admin_Packs_CanEditMDSettings { get; }         // Determines if the user can edit MD related fields while editing packs

		bool Admin_Articles_CanSee { get; }                 // Determines if the user can see the articles option within the administration treeview
		bool Admin_Articles_CanAdd { get; }                 // Determines if the user can create new articles within the administration treeview
		bool Admin_Articles_CanEdit { get; }                // Determines if the user can edit existing articles within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Articles_CanRename { get; }                // Determines if the user can edit existing articles within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Articles_CanDelete { get; }              // Determines if the user can delete articles within the administration treeview
		bool Admin_Articles_CanEditMDSettings { get; }      // Determines if the user can edit MD related fields while editing articles

        bool Admin_Labels_CanSee { get; }                   // Determines if the user can see the labels option within the administration treeview
		bool Admin_Labels_CanAdd { get; }                   // Determines if the user can create new labels within the administration treeview
		bool Admin_Labels_CanEdit { get; }                  // Determines if the user can edit existing labels within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Labels_CanRename { get; }                  // Determines if the user can edit existing labels within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Labels_CanDelete { get; }                // Determines if the user can delete labels within the administration treeview

		bool Admin_Catalogs_CanSee { get; }                 // Determines if the user can see the catalogs option within the administration treeview
		bool Admin_Catalogs_CanAdd { get; }                 // Determines if the user can create new catalogs els within the administration treeview
		bool Admin_Catalogs_CanEdit { get; }                // Determines if the user can edit existing catalogs within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Catalogs_CanRename { get; }              // Determines if the user can rename existing Catalog within the administration treeview
		bool Admin_Catalogs_CanDelete { get; }              // Determines if the user can delete catalogs within the administration treeview

        bool Admin_Mappings_CanSee { get; }                 // Determines if the user can see the mappings option within the administration treeview
		bool Admin_Mappings_CanAdd { get; }                 // Determines if the user can add new mappings within the administration treeview
		bool Admin_Mappings_CanEdit { get; }                // Determines if the user can edit existing mappings within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Mappings_CanRename { get; }              // Determines if the user can rename existing Mapping within the administration treeview
		bool Admin_Mappings_CanDelete { get; }              // Determines if the user can delete mappings within the administration treeview

        bool Admin_Users_CanSee { get; }                    // Determines if the user can see the "Users" option within the administration treeview
		bool Admin_Users_CanAdd { get; }                    // Determines if the user can add new users within the administration treeview
		bool Admin_Users_CanEdit { get; }                   // Determines if the user can edit existing users within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Users_CanRename { get; }                 // Determines if the user can edit the name of a user while editing it
		bool Admin_Users_CanDelete { get; }                 // Determines if the user can delete users within the administration treeview
		bool Admin_Users_CanHiddeUser { get; }              // Determines if the user can see the "Hidden" checkbox while editing a user
		bool Admin_Users_CanCreateWithoutEmail { get; }     // Determines if the user can create a new user with a name that is not a a valid Email address (normally it is required that the name of the user is a valid email address)
		bool Admin_Users_CanResetPassword { get; }			// Determines if the user can reset the password of other users
		bool Admin_Users_CanAssignPublicRoles { get; }		// Determines if the user can assign the following roles: PrinterOperator, DataUpload, ProdManager & CompanyAdmin
		bool Admin_Users_CanAssignIDTRoles { get; }			// Determines if the user can assign the following roles: IDTProdManager, IDTCommercial, IDTLabelDesign
		bool Admin_Users_CanAssignSysAdminRole { get; }     // Determines if the user can assign the SysAdmin role
		bool CanAssignRole(string role);

		bool Admin_Materials_CanSee { get; }                // Determines if the user can see the materials option within the administration treeview
		bool Admin_Materials_CanAdd { get; }                // Determines if the user can add new materials within the administration treeview
		bool Admin_Materials_CanEdit { get; }               // Determines if the user can edit existing materials within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
		bool Admin_Materials_CanRename { get; }             // Determines if the user can rename existing Materials within the administration treeview
		bool Admin_Materials_CanDelete { get; }             // Determines if the user can delete materials within the administration treeview

		bool PrintJobs_CanAssignFactory { get; }
		bool PrintJobs_CanAssignDueDate { get; }
		bool PrintJobs_CanAssignPrinter { get; }
		bool PrintJobs_CanCancel { get; }
		bool PrintJobs_CanDownloadMDB { get; }

		bool Admin_Artifact_CanAdd { get; }
		bool Admin_Artifact_CanEdit { get; }
		bool Admin_Artifact_CanDelete { get; }

		bool Admin_Categories_CanSee { get; }               
		bool Admin_Categories_CanAdd { get; }               
		bool Admin_Categories_CanEdit { get; }              
		bool Admin_Categories_CanRename { get; }            
		bool Admin_Categories_CanDelete { get; } 
		
		bool Can_Change_Provider { get; } // Broker

        bool Admin_Fonts_CanSee { get; }                // Determines if the user can see the fonts option within the administration treeview
        bool Admin_Fonts_CanAdd { get; }                // Determines if the user can add new fonts within the administration treeview
        bool Admin_Fonts_CanEdit { get; }               // Determines if the user can edit existing fonts within the administration treeview. If can see is true but can edit is false, then the view should appear as readonly
        bool Admin_Fonts_CanDelete { get; }             // Determines if the user can delete fonts within the administration treeview

        bool Can_Change_ERPConfiguration { get; }

        bool Can_Orders_Delete { get; }                 // Determines if the user can execute delete orders action

        bool Can_UploadDelivery { get; }               // Determines if the user can upload delivery notes
    }
}
