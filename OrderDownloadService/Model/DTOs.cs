using System;
using System.Xml.Serialization;

namespace OrderDonwLoadService.Model
{
    public class User
    {
        public string UserName { get; set; }
        public int? CompanyId { get; set; }
    }

    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CompanyId { get; set; }
    }

    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BrandId { get; set; }
        public string FTPClients { get; set; }
        public string FtpFolder { get; set; }
        public bool EnableFtpFolder { get; set; }

    }




    public class Catalog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProjectId { get; set; }
    }

    public class CatalogData
    {
        public string French { get; set; }
        public string MadeInID { get; set; }
    }

    public class CompanyOrderDTO
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public int OrderGroupID { get; set; }

    }
    public class OrderUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class OrderReportFilter
    {
        public int ProjectId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime OrderDateTo { get; set; }
        public int CompanyID { get; set; }

    }

    public enum ProductionType
    {
        All = 0,
        IDTLocation = 1,
        CustomerLocation = 2
    }



    [XmlRoot(ElementName = "IDP_VAS_MSG", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
    public class InditexOrderXmlResponse
    {
        [XmlElement(ElementName = "Type", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string Type { get; set; }

        [XmlElement(ElementName = "Id", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string Id { get; set; }

        [XmlElement(ElementName = "Number", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string Number { get; set; }

        [XmlElement(ElementName = "Message", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string Message { get; set; }

        [XmlElement(ElementName = "LogNo", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string LogNo { get; set; }

        [XmlElement(ElementName = "MessageV1", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string MessageV1 { get; set; }

        [XmlElement(ElementName = "MessageV2", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string MessageV2 { get; set; }

        [XmlElement(ElementName = "MessageV3", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string MessageV3 { get; set; }

        [XmlElement(ElementName = "MessageV4", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string MessageV4 { get; set; }
    }

    public class Response
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

    }

}
