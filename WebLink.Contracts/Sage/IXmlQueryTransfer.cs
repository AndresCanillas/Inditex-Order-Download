using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace WebLink.Contracts.Sage
{
    public interface IXmlQueryTransfer
    {
        List<Lin> Lines { get; set; }
        int Dimension { get; set; }
        int Size { get; set; }
    }

    public abstract class AXmlQueryTransfer : IXmlQueryTransfer
    {
        [XmlElement(ElementName = "LIN")]
        public List<Lin> Lines { get; set; }
        [XmlAttribute(AttributeName = "DIM")]
        public int Dimension { get; set; }
        [XmlAttribute(AttributeName = "SIZE")]
        public int Size { get; set; }
    }
}
