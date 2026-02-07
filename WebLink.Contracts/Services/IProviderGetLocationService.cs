using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Services
{
    public interface IProviderGetLocationService
    {
        int GetLocation(int companyId, int projectid, string countryCode, string catalogName, string filterField, string selectField);
    }
}
