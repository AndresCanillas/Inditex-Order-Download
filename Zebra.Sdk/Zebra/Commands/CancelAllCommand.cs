using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Services.Zebra.Commands
{
	public class CancelAllCommand: BaseCommand
	{
		public CancelAllCommand()
		{
			SetMessage("~JA");
			IsOneWay = true;
		}
	}
}
