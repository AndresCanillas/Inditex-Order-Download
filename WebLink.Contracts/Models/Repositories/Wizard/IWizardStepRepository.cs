using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface IWizardStepRepository : IGenericRepository<IWizardStep>
    {
		void MarkAsComplete(int wizardStepID);
		void MarkAsComplete(PrintDB ctx, int wizardStepID);
		void MarkAsCompleteByGroup(int position, IEnumerable<int> ordersIDs);
    }
}
