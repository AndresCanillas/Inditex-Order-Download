using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Sage
{
    public class SageSyncArticleProcessEvent : EQEventInfo
    {
        public string Identifier { get; set; }
        public int TotalItems { get; set; }
        public int Processed { get; set; }
        public string Name { get; set; }
        public string Reference { get; set; }

        public SageSyncArticleProcessEvent(string identifier, int totalItems, int processed, string lastArticleName, string lastArticleSageReference)
        {
            Identifier = identifier;
            TotalItems = totalItems;
            Processed = processed;
            Name = lastArticleName;
            Reference = lastArticleSageReference;

        }
    }
}
