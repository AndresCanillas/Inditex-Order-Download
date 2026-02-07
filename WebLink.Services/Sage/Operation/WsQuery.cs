using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using Service.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace WebLink.Services.Sage
{
    [XmlRoot(ElementName = "query")]
    public class WsQuery : WsAbstractOperation
    {
       
        [XmlElement("listSize")]
        public string ListSize { get; set; }

        public override string SoapAction() { return "http://www.adonix.com/WSS/CAdxWebServiceXmlCC/queryRequest"; }

        public WsQuery() : base() { ListSize = "1"; }

        //public WsQuery(IAppConfig config) : base(config)
        //{
        //    ListSize = "1";
        //}
    }
}
