using System;

namespace Service.Contracts.Database
{
	/// <summary>
	/// Enumeration used to specify the sort order of database queries
	/// </summary>
	enum DBSortDirection
	{
		/// <summary>
		/// Rows are sorted from lowest to highest.
		/// </summary>
		Ascending,
		/// <summary>
		/// Rows are sorted from highest to lowest.
		/// </summary>
		Descending
	}
}
