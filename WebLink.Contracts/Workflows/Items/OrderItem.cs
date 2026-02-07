using Service.Contracts.WF;

namespace WebLink.Contracts.Workflows
{
	public class OrderItem : WorkItem
	{
		public int OrderGroupID;
		public int OrderID;
		public string OrderNumber;
		public int CompanyID;
		public int BrandID;
		public int ProjectID;
		public bool WaitForValidation;
		public string SageOrderNumber;
		public string ProjectPrefix;
		public bool IsQA;
		public bool IsBillable;
		public bool ConflictDetected;
		public bool IsCustomPartialOrder;
		public string CompanyName;
		public string BrandName;
		public string ProjectName;
		public bool WaitForOrderResumed;
		public string PrimaryCustomer;
		public string SecondaryCustomer;

		public bool SendOrderReceivedEmailCompleted;
		public bool OrderExistVerifierCompleted;
		public bool CreatePrintPackageCompleted;
        public bool SendOrderConflictEmailCompleted;
        public bool SendOrderValidatedEmailCompleted;
        public bool CheckIsBillableCompleted;
		public bool CreateOrderDetailDocumentCompleted;
        public bool CreateOrderPreviewDocumentCompleted;
        public bool CreateProdSheetDocumentCompleted;
        public bool MarkAsBilledCompelted;
        public bool PerformOrderBillingCompleted;
		public bool OrderSetValidatorCompleted;
        public bool SendOrderCompletedEmailCompleted;
        public bool RunOrderReceivedPluginsCompleted;
        public bool RunOrderValidatedPluginsCompleted;
        public bool RunReadyToPrintPluginsCompleted;
        public bool RunReverseFlowCompleted;
        public bool RunPreValidationPluginsCompleted;

        public int FileDropEventCount;
        public int? OrderWorkflowConfigID;
        public bool HasReverseFlowStrategy;
        public bool OrderAuditCompleted;
        public bool WaitForOrderAudit;
        public int CompositionAuditID;
    }
}
