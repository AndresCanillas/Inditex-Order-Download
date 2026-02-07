using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class BillingRepository : IBillingRepository
    {
		private IFactory factory;

		public BillingRepository(IFactory factory)
		{
			this.factory = factory;
		}


        public List<IBillingInfo> GetByProviderID(int id)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByProviderID(ctx, id);
			}
        }


		public List<IBillingInfo> GetByProviderID(PrintDB ctx, int id)
		{
			return new List<IBillingInfo>(
				from b in ctx.BillingsInfo
				join p in ctx.ProviderBillingsInfo on b.ID equals p.BillingInfoID
				where p.ProviderID == id
				select b).ToList();
		}
	}
}
