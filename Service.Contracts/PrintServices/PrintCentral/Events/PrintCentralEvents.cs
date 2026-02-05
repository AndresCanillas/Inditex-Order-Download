using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintCentral
{
	public class PrintPackageReadyEvent : EQEventInfo
	{
		public int OrderID;
		public int ProductionLocation;
		public string ProjectPrefix;

        public PrintPackageReadyEvent(int OrderID, int ProductionLocation, string ProjectPrefix)
        {
            this.OrderID = OrderID;
            this.ProductionLocation = ProductionLocation;
            this.ProjectPrefix = ProjectPrefix;
        }
	}


	public class SmartdotsUserChangedEvent : EQEventInfo
	{
		// The following fields are always set
		public Operation Operation { get; set; }
		public string Id { get; set; }
		public string UserName { get; set; }

		// The following fields are set only if Operation is ProfileChanged
		public int? LocationID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
		public string Language { get; set; }
		public bool ShowAsUser { get; set; }
		public string PwdHash { get; set; }     // IMPORTANT: PwdHash will be null if the password was not changed, otherwise, it will contain the new hash required to authenticate the user.
		public string Roles { get; set; }		// A comma delimited list with the user roles, this information is only available when Operation is ProfileChanged

		// The following field is set only if Operation is RoleAdded/RoleRemoved
		public string Role { get; set; }
	}


	public enum Operation
	{
		ProfileChanged,
		RoleAdded,
		RoleRemoved,
		UserDeleted
	}

	public class EQEncodedJobs : EQEventInfo
	{
		public int JobID;
		public int Quantity;
		public string Barcodes;
	}
}
