using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class CountryRepository : GenericRepository<ICountry, Country>, ICountryRepository
    {
		public CountryRepository(IFactory factory)
			: base(factory, (ctx) => ctx.Countries)
		{
		}


		protected override string TableName => "Countries";


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Country actual, ICountry data)
        {
        }


        public ICountry GetByAlpha2(string alpha2)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByAlpha2(ctx, alpha2);
			}
		}


		public ICountry GetByAlpha2(PrintDB ctx, string alpha2)
		{
			return ctx.Countries.FirstOrDefault(w => w.Alpha2.Equals(alpha2));
		}
	}
}
