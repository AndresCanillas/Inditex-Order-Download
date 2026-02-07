using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class OrderSetValidatorService : IOrderSetValidatorService
	{
		private IFactory factory;
		private IProjectRepository projectRepo;
		private IOrderRepository orderRepo;
		private IOrderLogRepository orderLog;
		private IWizardRepository wizardRepo;
		private IWizardStepRepository wizardStepRepo;
		private IWizardCustomStepRepository customStepsRepo;
        private readonly IPluginManager<IOrderSetValidatorPlugin> pluginManager;

        public OrderSetValidatorService(
			IFactory factory,
			IProjectRepository projectRepo,
			IOrderRepository orderRepo,
			IOrderLogRepository orderLog,
			IWizardRepository wizardRepo,
			IWizardStepRepository wizardStepRepo,
			IWizardCustomStepRepository customStepsRepo,
            IPluginManager<IOrderSetValidatorPlugin> pluginManager
            )
		{
			this.factory = factory;
			this.projectRepo = projectRepo;
			this.orderRepo = orderRepo;
			this.orderLog = orderLog;
			this.wizardRepo = wizardRepo;
			this.wizardStepRepo = wizardStepRepo;
			this.customStepsRepo = customStepsRepo;
            this.pluginManager = pluginManager;
        }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="orderID"></param>
		/// <param name="projectID"></param>
		/// <param name="brandID"></param>
		/// <returns>0 OK, -1 wait</returns>
		public int Execute(int orderGroupID, int orderID, string orderNumber, int projectID, int brandID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				// get project settings
				var project = projectRepo.GetByID(ctx, projectID);
				//var order = orderRepo.GetByID(orderID, true);

				if (!project.EnableValidationWorkflow)
				{
					SetAsValidated(ctx, orderID, false);
					orderLog.Info(ctx, orderID, "Order not require Validation, take valid as it");
					return 1; // validation workflow not required
				}

				// configure validation wizard for this order
				SetWizardConfiguration(ctx, orderID, project);

                // add empty orders for composition
                //if (project.AllowAddOrChangeComposition)
                //{
                //	CreateCompositionOrderTemplate(orderGroupID);
                //}

                if(!string.IsNullOrEmpty(project.OrderSetValidatorPlugin))
                {
                    using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        using(var plugin = pluginManager.GetInstanceByName(project.OrderSetValidatorPlugin))
                        {
                            var takeAsValid = plugin.TakeAsValidated(new OrderPluginData() { OrderGroupID = orderGroupID, OrderID = orderID, BrandID = brandID, ProjectID = projectID });

                            if(takeAsValid == 1)
                            {
                                SetAsValidated(ctx, orderID);
                                return takeAsValid;
                            }
                        }
                    }
                }

				return 0;// validation workflow required
            }
		}

		private void SetAsValidated(PrintDB ctx, int orderID, bool hasWorkflow = true)
		{
			var order = orderRepo.ChangeStatus(ctx, orderID, OrderStatus.Validated);

			if(hasWorkflow)
			{
				wizardRepo.SetAsComplete(ctx, orderID);
			}
		}


		private void SetValidationRequired(PrintDB ctx, int orderID)
		{
            // TODO: check if wizard exist, must be reset steps and Wizard
			var order = orderRepo.ChangeStatus(ctx, orderID, OrderStatus.InFlow);
			orderLog.Info(ctx, orderID, $"Validation Required");
		}


		private void SetWizardConfiguration(PrintDB ctx, int orderID, IProject projectSettings)
		{
			IWizard wizard = wizardRepo.GetByOrder(ctx, orderID);

			if (wizard != null)
			{
				SetValidationRequired(ctx, orderID);
				return;
			}

			var steps = new List<IWizardStep>();
			wizard = new Wizard()
			{
				OrderID = orderID,
				IsCompleted = false

			};

			var availableSteps = customStepsRepo.GetAvailablesStepsFor(ctx, orderID);

            //// custom wizard to set order data if available
            //var setOrderDataStep = availableSteps.FirstOrDefault(w => w.Type == WizardStepType.SetOrderData);
            //if (setOrderDataStep != null)
            //{
            //    var step = GetStepByType(WizardStepType.SetOrderData, availableSteps);
            //    steps.Add(step);
            //}

            if (projectSettings.RequireItemAssignment)
            {
                var step = GetStepByType(WizardStepType.ItemAssignment, availableSteps);
                steps.Add(step);
            }

            // quantity edition is allow ?
            if (projectSettings.AllowQuantityEdition != (int)OrderQuantityEditionOption.NotAllow)
			{
				var step = GetStepByType(WizardStepType.Quantity, availableSteps);
				steps.Add(step);
			}

			// add extras is allow ?
			if (projectSettings.AllowExtrasDuringValidation != (int)OrderExtrasOption.NotAllow)
			{
				var step = GetStepByType(WizardStepType.Extras, availableSteps);
				steps.Add(step);
			}

			// Check if order labels are mark with Composition Required
			if (projectSettings.AllowAddOrChangeComposition)
			{
				var stepl = GetStepByType(WizardStepType.Labelling, availableSteps);
				steps.Add(stepl);
			}

            if (projectSettings.IncludeFiles)
            {
                var step = GetStepByType(WizardStepType.SupportFiles, availableSteps);
                steps.Add(step);
            }

			// add shipping address verification
			steps.Add(GetStepByType(WizardStepType.ShippingAddress, availableSteps));
			// add review - final step
			steps.Add(GetStepByType(WizardStepType.Review, availableSteps));

			var wizardInserted = wizardRepo.Insert(ctx, wizard);

			int position = 0;

            steps.OrderBy(o => o.Position).ToList().ForEach((stp) =>
			{
				stp.WizardID = wizardInserted.ID;
				stp.Position = position++;
				wizardStepRepo.Insert(ctx, stp);
			});

			// udpate order status
			SetValidationRequired(ctx, orderID);
		}

		//private void CreateCompositionOrderTemplate(int orderGroupID)
		//{

		//	//var selection = new List<OrderGroupSelectionDTO>()
		//	//{
		//	//	new OrderGroupSelectionDTO()
		//	//	{
		//	//		OrderGroupID = orderGroupID
		//	//	}
		//	//};

		//	//var filter = new OrderArticlesFilter()
		//	//{
		//	//	ArticleType = ArticleTypeFilter.Label,
		//	//	ActiveFilter = OrderActiveFilter.NoRejected,
		//	//	Source = OrderSourceFilter.NotFromValidation
		//	//};
		//	//var result = orderRepo.GetArticleDetailSelection(selection, filter);

		//	// all options are the same label, 
		//}


		private IWizardStep GetStepByType(WizardStepType type, IEnumerable<IWizardCustomStep> availables)
		{
			IWizardCustomStep found;
			// find in available configuration by project first

			found = availables.Where(w => w.ProjectID != null && w.Type.Equals(type)).FirstOrDefault();

			if (found == null)
			{
				found = availables.Where(w => w.BrandID != null && w.Type.Equals(type)).FirstOrDefault();
			}

			if (found == null)
			{
				found = availables.Where(w => w.CompanyID != null && w.Type.Equals(type)).FirstOrDefault();
			}

			if (found == null)
			{
				found = GetDefaultStepByType(type);
			}

			return new WizardStep() { 
				Name = found.Name,
				Description = found.Description,
				Type = found.Type,
				Url = found.Url,
				Position = found.Position,
				IsCompleted = false
			};
		}


		private IWizardCustomStep GetDefaultStepByType(WizardStepType type)
		{
			IWizardCustomStep def = new WizardCustomStep() { Type = type, Position = (int)type };

			switch (type)
			{

                case WizardStepType.ItemAssignment:
                    def.Url = "/validation/common/ItemAssignmentWizard.js";
                    def.Name = "Add Articles";
                    break;

                case WizardStepType.Quantity:
					def.Url = "/validation/common/QuantityWizard.js";
					def.Name = "Quantities";
					break;

				case WizardStepType.Labelling:
					def.Url = "/validation/common/LabellingCompoSimpleWizard.js";
					def.Name = "Define Composition";
                    break;

				//case WizardStepType.Composition:
				//	def.Url = "/validation/common/CompositionWizard.js";
				//	def.Name = "Composition";
				//	break;

				case WizardStepType.Extras:
					def.Url = "/validation/common/ArticleExtrasWizard.js";
					def.Name = "Add Extras";
					break;

				case WizardStepType.ShippingAddress:
					def.Url = "/validation/common/ShippingAddressWizard.js";
					def.Name = "Delivery Address";
					break;

				case WizardStepType.Review:
                    def.Url = "/validation/common/ReviewWizard.js";
                    def.Name = "Confirm Order";
                    break;

                case WizardStepType.SupportFiles:
                    def.Url = "/validation/common/SupportFilesWizard.js";
                    def.Name = "Support Files";
                    break;

                default:
					def.Url = "/validation/common/UnavailableWizard.js";
					break;
			}

			return def;
		}
	}
}
