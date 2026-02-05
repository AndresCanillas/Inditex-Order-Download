using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{ 
	public static class MiscHelper
	{
		public static string Coalesce(params string[] values)
		{
			foreach (var v in values)
			{
				if (!String.IsNullOrWhiteSpace(v))
					return v;
			}
			return null;
		}
	}
}
