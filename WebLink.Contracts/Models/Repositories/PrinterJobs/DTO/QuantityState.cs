using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class QuantityState
    {
        public int OrderID { get; set; }

        public int PrinterJobDetailID { get; set; }

        public int Value { get; set; }
    }
}
