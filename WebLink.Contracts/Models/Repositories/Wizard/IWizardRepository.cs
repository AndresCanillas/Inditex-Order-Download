using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface IWizardRepository: IGenericRepository<IWizard>
    {
		IWizard GetByOrder(int orderID);
		IWizard GetByOrder(PrintDB ctx, int orderID);

		void Reset(int wizardID);
		void Reset(PrintDB ctx, int wizardID);

		float UpdateProgress(int wizardID);
		float UpdateProgress(PrintDB ctx, int wizardID);

		void UpdateProgressByGroup(IEnumerable<int> ordersIDs);
		void UpdateProgressByGroup(PrintDB ctx, IEnumerable<int> ordersIDs);

		void SetAsComplete(int orderID);
		void SetAsComplete(PrintDB ctx, int orderID);

		IEnumerable<IWizardStep> GetSteps(int wizardID);
		IEnumerable<IWizardStep> GetSteps(PrintDB ctx, int wizardID);
	}
}
