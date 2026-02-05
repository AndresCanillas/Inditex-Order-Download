using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public class PagedOperationResult : OperationResult
	{
		public int TotalCount;

		public PagedOperationResult() { }

		public PagedOperationResult(bool success, string message, int totalCount, object data = null)
		{
			Success = success;
			Message = message;
			Data = data;
			TotalCount = totalCount;
		}
	}
}
