using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Services.Zebra.Commands
{
    public class OpenRawChannel : BaseCommand
    {
		public OpenRawChannel()
		{
			SetMessage("{\r\n\"open\" : \"v1.raw.zebra.com\"\r\n}");
		}
    }
}
