using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Sage
{

    

    public abstract class WsRequestMessage : IWsRequestMessage
    {
        [XmlIgnore]
        public string Url { get; set; }

        [XmlElement(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public string Header { get; set; }

        public string Action { get; set; }

        public WsRequestMessage()
        {

        }

        //public WsRequestMessage(IAppConfig cfg)
        //{
        //    this.cfg = cfg;
        //    Url = cfg["WebLink.Sage.Url"];
        //}
    }

    [XmlRoot("Envelope")]
    public class WsRequestQuery : WsRequestMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public BodyQuery Body { get; set; }

        //public WsRequestQuery(IAppConfig cfg) : base(cfg) { }

    }

    public class BodyQuery
    {
        [XmlElement(ElementName ="query", Namespace = "http://www.adonix.com/WSS")]
        public WsQuery Operation {get;set;}
    }

    [XmlRoot("Envelope")]
    public class WsRequestRead : WsRequestMessage
    {

        [XmlElement("Body")]
        public BodyRead Body { get; set; }

        public WsRequestRead() : base() { }

        //public WsRequestRead(IAppConfig cfg) : base(cfg) { }

    }

    public class BodyRead
    {
        [XmlElement("read")]
        public WsRead Operation { get; set; }
    }

    [XmlRoot("Envelope")]
    public class WsRequestSave : WsRequestMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public BodySave Body { get; set; }


        public WsRequestSave() : base() { }
        //public WsRequestSave(IAppConfig cfg) : base(cfg) { }
    }

    public class BodySave
    {
        [XmlElement("save")]
        public WsSave Operation { get; set; }
    }

    [XmlRoot("Envelope")]
    public class WsRequestUpdate : WsRequestMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public BodyUpdate Body { get; set; }

        public WsRequestUpdate() : base() { }
        //public WsRequestUpdate(IAppConfig cfg) : base(cfg) { }
    }

    public class BodyUpdate
    {
        [XmlElement("modify")]
        public WsUpdate Operation { get; set; }

    }

    


   

}
