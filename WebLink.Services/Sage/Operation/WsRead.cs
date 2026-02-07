using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Serialization;

namespace WebLink.Services.Sage
{
    [XmlRoot(ElementName = "read")]
    public class WsRead : WsAbstractOperation
    {

        public override string SoapAction() { return "http://www.adonix.com/WSS/CAdxWebServiceXmlCC/readRequest"; }

        public WsRead() : base() { }

        //public WsRead(IAppConfig config) : base (config)
        //{

        //}

    }
}
