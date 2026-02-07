using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
   public enum OrderQuantityEditionOption
    {
        NotAllow,
        MaxFixedValue,
        MaxPercentajeValue
    }

    public enum OrderExtrasOption
    {
        NotAllow,
        MaxFixedValue,
        MaxPercentajeValue
    }

    public enum OrderCompoEditionOption
    {
        NotAllow,
        Allow
    }
}
