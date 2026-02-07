using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Services.Zebra.Commands
{
	public class PauseCommand: BaseCommand
	{
		public PauseCommand(bool pause)
		{
			if(pause)
				SetMessage("~PP");
			else
				SetMessage("~PS");
			IsOneWay = true;
		}
	}
}
