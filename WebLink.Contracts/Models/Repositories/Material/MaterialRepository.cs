using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class MaterialRepository : GenericRepository<IMaterial, Material>, IMaterialRepository
    {
		public MaterialRepository(IFactory factory)
			: base(factory, (ctx) => ctx.Materials)
		{
		}


		protected override string TableName { get => "Materials"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Material actual, IMaterial data)
		{
			actual.Name = data.Name;
			actual.Properties = data.Properties;
		}
    }
}
