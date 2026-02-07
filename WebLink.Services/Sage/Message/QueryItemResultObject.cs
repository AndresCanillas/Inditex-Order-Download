using WebLink.Contracts.Sage;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace WebLink.Services.Sage
{
    // https://stackoverflow.com/questions/1556874/user-xmlns-was-not-expected-deserializing-twitter-xml
    [XmlRoot("RESULT")]
    public class QueryItemResultObject : AXmlQueryTransfer, IXmlQueryTransfer
    {
        public QueryItemResultObject () : base() {}
    }
}
