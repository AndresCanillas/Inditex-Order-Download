using WebLink.Contracts.Sage;
using System.Xml.Serialization;

namespace WebLink.Services.Sage
{
	[XmlRoot(ElementName = "RESULT")]
	public class ResultObject : AXmlTransfer, IXmlTransfer { }
}
