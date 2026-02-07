using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace WebLink.Services.Sage
{
    public class WsSave : WsAbstractOperation
    {
        public override string SoapAction() { return "http://www.adonix.com/WSS/CAdxWebServiceXmlCC/saveRequest"; }

        public WsSave() : base() { }
    }
}
