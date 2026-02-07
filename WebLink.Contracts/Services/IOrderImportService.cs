using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts;
using Service.Contracts.Documents;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	/* ===================================================================================================================== 
	 * Service used to incorportate orders into the system, this covers two possible paths: 
	 *		A) Orders created manually using the Print Labels option.
	 *		   When the user wants to create an order manually, by capturing the order data through the web application,
	 *		   the system shows a grid where the user can capture the article, the quantity and all the variable data.
	 *		   This information is then taken to create the order by calling CreateOrder method.
	 * 
	 *		B) Orders created from the result of a DataImport process.
	 *		   The data import process covers the following cases:
	 *			> A file received through FTP
	 *			> A file uploaded through the web portal
	 *			> A file received through the WebAPI
	 *			
	 *			For all these cases, we invoke CompleteOrderUpload method to create the order.
	 * 
	 * ===================================================================================================================== */
	public interface IOrderImportService
	{
		string CreateOrder(IUserData userData, CreateOrderDTO orderData);
		string CreateOrder(PrintDB ctx, IUserData userData, CreateOrderDTO orderData);

		Task CompleteOrderUpload(DataImportJobInfo job);
		Task CompleteOrderUpload(PrintDB ctx, DataImportJobInfo job);
	}

    // Object copied into FTPFileWatcherService
    public class UploadOrderDTO
    {
        public ProductionType ProductionType { get; set; }
        public int PrinterID { get; set; }
        public int FactoryID { get; set; }
        public int ProjectID { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public bool IsStopped { get; set; } = false;
        public bool IsBillable { get; set; } = true;
        public string OrderCategoryClient { get; set; }
        public string MDOrderNumber { get; set; }
        public int LocationID { get { return FactoryID; } set { this.FactoryID = value; } }
	}

	public class CreateOrderDTO : UploadOrderDTO
	{
		public string Data { get; set; }
	}
}
