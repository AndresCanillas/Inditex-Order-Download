using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
	public interface IDBConnectionManager
	{
		string WebLinkDB { get; }
		string UsersDB { get; }
		string CatalogDB { get; }
		IDBX OpenDatabase(string database);
		IDBX OpenWebLinkDB();
		IDBX OpenUsersDB();
		IDBX OpenCatalogDB();
		IDBX OpenIDTECIBrands();
		DynamicDB CreateDynamicDB();
        IDBX OpenHerculesDB();
    }
}
