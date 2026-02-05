using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Service.Contracts.Database
{
	public static class DataReaderExtensions
	{
		public static object GetNullable(this IDataReader rd, string fieldName, object defaultValue = null)
		{
			var value = rd[fieldName];
			if (value is DBNull)
				return defaultValue;
			else
				return value;
		}
	}
}
