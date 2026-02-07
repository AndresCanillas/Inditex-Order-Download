using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class RequestLabelType
    {
        public OrderGroupSelectionDTO[] Selection { get; set; }
        public LabelType LabelType { get; set; }
    }
}
