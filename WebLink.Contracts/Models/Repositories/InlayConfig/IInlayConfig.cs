using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface IInlayConfig : IEntity, IBasicTracing
    {
        int InlayID { get; set; }
        string Description { get; set; }
        int? CompanyID { get; set; }
        int? ProjectID { get; set; }
        int? BrandID { get; set; }
        bool IsAuthorized { get; set; }
    }
}
