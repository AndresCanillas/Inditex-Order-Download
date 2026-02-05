using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class TaskInfoAttribute: Attribute
	{
		public string Name;
		public string Description;
		public bool CanRunOutOfFlow;
	}
}
