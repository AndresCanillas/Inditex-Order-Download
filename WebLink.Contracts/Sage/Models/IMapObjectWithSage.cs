using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    // TODO: Apply this interface into Articles, Address, and Companies
    public interface IMapObjectWithSage
    {
        bool SyncWithSage { get; set; }
        string SageReference { get; set; }
    }
}
