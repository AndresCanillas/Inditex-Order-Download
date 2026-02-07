using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace WebLink.Services.Sage
{
    public class WsUpdate : WsAbstractOperation
    {
        public override string SoapAction() { return "http://www.adonix.com/WSS/CAdxWebServiceXmlCC/modifyRequest"; }

        public WsUpdate() : base() { }

        //public WsUpdate(IAppConfig config) : base(config) { }
    }
}
