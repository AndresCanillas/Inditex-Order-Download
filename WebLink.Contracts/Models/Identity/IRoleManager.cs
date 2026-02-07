using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public interface IRoleManager
	{
		IEnumerable<AppRole> Roles { get; }
		Task<AppRole> FindByNameAsync(string role);
		Task CreateAsync(AppRole role);
	}

	public class RoleManager : IRoleManager
	{
		private IFactory factory;

		public RoleManager(IFactory factory)
		{
			this.factory = factory;
		}

		public IEnumerable<AppRole> Roles
		{
			get
			{
				using (var ctx = factory.GetInstance<IdentityDB>())
				{
					return ctx.Roles.AsNoTracking().ToList();
				}
			}
		}

		public async Task<AppRole> FindByNameAsync(string role)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await ctx.Roles.Where(r => r.Name == role).FirstOrDefaultAsync();
			}
		}

		public async Task CreateAsync(AppRole role)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				role.Id = Guid.NewGuid().ToString();
				ctx.Roles.Add(role);
				await ctx.SaveChangesAsync();
			}
		}
	}
}
