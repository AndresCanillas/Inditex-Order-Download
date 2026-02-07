using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    /* TODO: Remove
	public class PrintJobRequestDTO
	{
		public int ProductID { get; set; }
		public int ArticleID { get; set; }
		public int LocationID { get; set; }
		public int PrinterID { get; set; }
		public int Quantity { get; set; }
	}

	public class BatchPrintRequestDTO
	{
		public int LocationID { get; set; }
		public int PrinterID { get; set; }
		public List<BatchPrintJob> Jobs { get; set; } = new List<BatchPrintJob>();
	}

	public class BatchPrintJob
	{
		public int ArticleID { get; set; }
		public List<BatchPrintDetail> Detail { get; set; } = new List<BatchPrintDetail>();
	}

	public class BatchPrintDetail
	{
		public int ProductID { get; set; }
		public int Quantity { get; set; }
	}
	*/


    public interface IBrandRepository : IGenericRepository<IBrand>
    {
        List<IBrand> GetByCompanyID(int companyid);
        List<IBrand> GetByCompanyID(PrintDB ctx, int companyid);

        void UpdateIcon(int brandid, byte[] content);
        void UpdateIcon(PrintDB ctx, int brandid, byte[] content);

        byte[] GetIcon(int brandid);
        byte[] GetIcon(PrintDB ctx, int brandid);

        IBrand GetSelectedBrand();
        IBrand GetSelectedBrand(PrintDB ctx);

        void AssignRFIDConfig(int brandid, int configid);
        void AssignRFIDConfig(PrintDB ctx, int brandid, int configid);
        List<IBrand> GetByCompanyIDME(int companyid);
        IEnumerable<Brand> GetAllByID(IEnumerable<int> brandIDs);
        IEnumerable<Brand> GetAllByID(PrintDB ctx, IEnumerable<int> brandIDs);
    }
}
