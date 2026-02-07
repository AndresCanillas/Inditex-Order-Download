using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public  interface IWizardCustomStepRepository: IGenericRepository<IWizardCustomStep>
    {
		IEnumerable<IWizardCustomStep> GetByProjectID(int id);
		IEnumerable<IWizardCustomStep> GetByProjectID(PrintDB ctx, int id);

		IEnumerable<IWizardCustomStep> GetGenericSteps();
		IEnumerable<IWizardCustomStep> GetGenericSteps(PrintDB ctx);

		IEnumerable<IWizardCustomStep> GetAvailablesStepsFor(int orderID);
		IEnumerable<IWizardCustomStep> GetAvailablesStepsFor(PrintDB ctx, int orderID);
	}
}