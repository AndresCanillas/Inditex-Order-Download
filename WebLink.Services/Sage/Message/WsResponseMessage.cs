using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Sage
{

    /**
     *   Responses
     * 
     **/
    

    public abstract class WsResponseMessage : IWsResponseMessage
    {

    }

    [XmlType(Namespace = "http://www.adonix.com/WSS", TypeName = "CAdxResultXml")]
    public class WsReturnResult : IWsReturnResult
    {
        [XmlElement(ElementName = "resultXml", Namespace = "")]
        public string ResultXml { get; set; }

        [XmlElement(ElementName = "status")]
        public int Status { get; set; }

        //[XmlElement(ElementName = "messages")]
        //public ResultMesssages Messages { get; set; }
        //[XmlArray("messages")]
        //[XmlArrayItem("message")]
        //[XmlElement(ElementName = "messages",  Namespace = "http://schemas.xmlsoap.org/soap/encoding/")]
        // Parser from XmlDocument, this property is dynamic object
        public List<WsMessage> Messages { get; set; }

        //[XmlElement(ElementName = "messages", Namespace = "http://schemas.xmlsoap.org/soap/encoding/")]
        //public List<MessageRef> MessageRefs { get; set; }

        public WsReturnResult() {

            //Messages = new List<ResultMesssages>();
        }

        public virtual string AllMessages(string separator = ", ")
        {
            return string.Join(separator, Messages.Select(s => s.Message));
        }
    }


    public enum WsMessageType
    {
        Info =  0,
        Warning,
        Error
    }

    //[XmlRoot(ElementName = "messages")]
    public class WsMessage
    {
        //[XmlElement(ElementName = "type")]
        public WsMessageType Type { get; set; }
        //[XmlElement(ElementName = "message")]
        public string Message { get; set; }

    }



    public class MessageRef
    {
        [XmlAttribute("href")]
        public string Href { get; set; }

    }

    //[XmlRoot(ElementName = "multiRef")]
    //public class MultiRef
    //{
    //    [XmlElement(ElementName = "type")]
    //    public string Type { get; set; }
    //    [XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
    //    public string _Type { get; set; }
    //    [XmlElement(ElementName = "message")]
    //    public string Message { get; set; }
    //    [XmlAttribute(AttributeName = "id")]
    //    public string Id { get; set; }
    //    [XmlAttribute(AttributeName = "root", Namespace = "http://schemas.xmlsoap.org/soap/encoding/")]
    //    public string Root { get; set; }
    //    [XmlAttribute(AttributeName = "encodingStyle", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    //    public string EncodingStyle { get; set; }
    //}


    #region Query
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class WsResponseQuery : WsResponseMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public QueryResponseBody Body { get; set; }

    }


    public class QueryResponseBody
    {
        [XmlElement(ElementName = "queryResponse", Namespace = "http://www.adonix.com/WSS")]
        public QueryResponse Response { get; set; }

        public QueryResponseBody() { }
    }


    public class QueryResponse
    {
        [XmlElement(ElementName = "queryReturn", Namespace = "")]
        public WsReturnResult Return { get; set; }

        [XmlAttribute(AttributeName = "encodingStyle", Namespace = "")]
        public string EncodingStyle { get; set; }

        public QueryResponse() { }
    }

    //[XmlType(Namespace = "http://www.adonix.com/WSS", TypeName = "CAdxResultXml")]
    //public class QueryReturn
    //{
    //    [XmlElement(ElementName = "resultXml", Namespace = "")]
    //    public string ResultXml { get; set; }

    //    [XmlElement(ElementName = "status")]
    //    public int Status { get; set; }

    //    public QueryReturn() { }
    //}

    #endregion Query


    #region Read

    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class WsResponseRead : WsResponseMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public ReadResponseBody Body { get; set; }

    }


    public class ReadResponseBody
    {
        [XmlElement(ElementName = "readResponse", Namespace = "http://www.adonix.com/WSS")]
        public ReadResponse Response { get; set; }

        //public ReadResponseBody() { }
    }


    public class ReadResponse
    {
        [XmlElement(ElementName = "readReturn", Namespace = "")]
        public WsReturnResult Return { get; set; }

        [XmlAttribute(AttributeName = "encodingStyle", Namespace = "")]
        public string EncodingStyle { get; set; }

        //public ReadResponse() { }
    }

    //[XmlType(Namespace = "http://www.adonix.com/WSS", TypeName = "CAdxResultXml")]
    //public class ReadReturn
    //{
    //    [XmlElement(ElementName = "resultXml", Namespace = "")]
    //    public string ResultXml { get; set; }

    //    [XmlElement(ElementName = "status")]
    //    public int Status { get; set; }

    //    public ReadReturn() { }
    //}

    #endregion Read


    #region Save
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlType(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", TypeName = "CAdxMessage")]
    public class WsResponseSave : WsResponseMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public SaveResponseBody Body { get; set; }

    }


    
    public class SaveResponseBody
    {
        [XmlElement(ElementName = "saveResponse", Namespace = "http://www.adonix.com/WSS")]
        public SaveResponse Response { get; set; }

    }


    public class SaveResponse
    {
        [XmlElement(ElementName = "saveReturn", Namespace = "")]
        public WsReturnResult Return { get; set; }

        [XmlAttribute(AttributeName = "encodingStyle", Namespace = "")]
        public string EncodingStyle { get; set; }



        //public ReadResponse() { }
    }
    #endregion Save


    #region Update

    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlType(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", TypeName = "CAdxMessage")]
    public class WsResponseUpdate : WsResponseMessage
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public UpdateResponseBody Body { get; set; }

    }



    public class UpdateResponseBody
    {
        [XmlElement(ElementName = "modifyResponse", Namespace = "http://www.adonix.com/WSS")]
        public UpdateResponse Response { get; set; }

    }


    public class UpdateResponse
    {
        [XmlElement(ElementName = "modifyReturn", Namespace = "")]
        public WsReturnResult Return { get; set; }

        [XmlAttribute(AttributeName = "encodingStyle", Namespace = "")]
        public string EncodingStyle { get; set; }
    }

    #endregion Update
}
