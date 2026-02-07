using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class GroupFileColumnRepository : GenericRepository<IGroupFileColumn, GroupFileColumn>, IGroupFileColumnRepository
    {
		public GroupFileColumnRepository(IFactory factory)
			: base(factory, (ctx) => ctx.GroupFileColumns)
		{
		}


		protected override string TableName => "GroupFileColumns";


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, GroupFileColumn actual, IGroupFileColumn data)
		{
		}

		public IEnumerable<GroupFileColumn> GetByProject(int projectid)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByProject(ctx, projectid);
			}
		}


		public IEnumerable<GroupFileColumn> GetByProject(PrintDB ctx, int projectid)
		{
			return ctx.GroupFileColumns
				.Where(w => w.ProjectId.Equals(projectid))
				.Select(s => s)
				.ToList();
		}
	}
}
