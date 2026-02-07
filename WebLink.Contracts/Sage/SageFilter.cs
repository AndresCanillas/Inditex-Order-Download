using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Sage
{
    public interface ISageFilter
    {
        string Key { get; set; }
        string Val { get; set; }
    }

    public class SageFilter : ISageFilter
    {
        public string Key { get; set; }

        public string Val { get; set; }
    }


    
}
