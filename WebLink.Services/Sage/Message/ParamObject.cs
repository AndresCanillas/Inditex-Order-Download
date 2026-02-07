using WebLink.Contracts.Sage;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace WebLink.Services.Sage
{
	[XmlRoot(ElementName = "PARAM")]
	public class ParamObject : AXmlTransfer, IXmlTransfer
	{
		public ParamObject() : base()
		{
			Fields = new List<Fld>();
			Groups = new List<Grp>();
			Tables = new List<Tab>();
			Lists = new List<Lst>();

		}

	}

}
