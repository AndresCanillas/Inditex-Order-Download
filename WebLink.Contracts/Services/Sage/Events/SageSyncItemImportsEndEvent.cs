using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Sage
{
    public class SageSyncItemImportsEndEvent : EQEventInfo
    {
        public string Identifier { get; set; }
        public int Processed { get; set; }

        public SageSyncItemImportsEndEvent(string identifier, int processed)
        {
            Identifier = identifier;
            Processed = processed;
        }
    }
}
