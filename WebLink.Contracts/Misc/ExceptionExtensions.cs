using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
	public static class ExceptionExtensions
	{
		public static bool IsNameIndexException(this Exception ex)
		{
			if (ex.Message.Contains("IX_Name"))
				return true;
			while(ex.InnerException != null)
			{
				ex = ex.InnerException;
				if (ex.Message.Contains("IX_Name"))
					return true;
			}
			return false;
		}
	}
}
