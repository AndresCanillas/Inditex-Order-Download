using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using Service.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Xml;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Sage
{

    public abstract class WsAbstractOperation
    {
        protected IAppConfig config { get; set; }

        [XmlElement("callContext")]
        public WsContext CallContext { get; set; }

        // Web Service Name defined in X3
        [XmlElement("publicName")]
        public string PublicName { get; set; }

        [XmlArray("objectKeys")]
        [XmlArrayItem("keys")]
        public List<WsKey> ObjectKeys { get; set; }

        //[XmlElement("objectKeys")]
        //public ObjectKeys ObjectKeys { get; set; }

        [XmlIgnore]
        public string ObjectXml { get; set; }

        [XmlElement(ElementName = "objectXml")]
        public XmlCDataSection ObjectXmlContent {
            get
            {
                XmlDocument doc = new XmlDocument();
                return doc.CreateCDataSection(ObjectXml);
            }
            set
            {
                ObjectXml = value.Value;
            }
        }
        public bool ShouldSerializeObjectXmlContent()
        {
            return !string.IsNullOrEmpty(ObjectXml);
        }

        // the value of this method is manaully set, copied from SOAPUI, WS-A Lower Tab for every action
        public abstract string SoapAction();

        public WsAbstractOperation() { }

        //public WsAbstractOperation(IAppConfig config)
        //{
        //    this.config = config;

        //    CallContext = new WsContext(config);
        //}

    }


    public class WsContext : IWsContext
    {
        //private IAppConfig config;

        // context lang: SPA -> Spanish
        [XmlElement("codeLang")]
        public string CodeLang { get; set; }

        // PoolAlias  created in SAGE X3
        [XmlElement("poolAlias")]
        public string PoolAlias { get; set; }

        // Random Identifier, using for tracking on error cases
        [XmlElement("poolId")]
        public string PoolId { get; set; }

        // use to format xml response
        [XmlElement("requestConfig")]
        public string RequestConfig { get; set; }

        public WsContext() { }

        //public WsContext(IAppConfig config)
        //{
        //    this.config = config;

        //    CodeLang = config["WebLink.Sage.CodeLang"];
        //    PoolAlias = config["WebLink.Sage.PoolName"];
        //    PoolId = config["WebLink.Sage.PoolID"];
        //    RequestConfig = config["WebLink.Sage.RequestConfig"];
        //}

    }

    //public class ObjectKeys
    //{
    //    [XmlArray("keys")]
    //    public List<WsKey> Keys { get; set; }
    //}

    public class WsKey : IWsKey
    {
        [XmlElement("key")]
        public string Key { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }
    }

    public class ObjectXml
    {
        [XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string Type { get; set; }
        [XmlIgnore]
        public string Text { get; set; }

        [XmlText]
        public System.Xml.XmlCDataSection TextCData
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(Text);
            }
            set
            {
                Text = value.Value;
            }
        }

        public ObjectXml()
        {
            Type = "xsd:string";
        }
    }


}
