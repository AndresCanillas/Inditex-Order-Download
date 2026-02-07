using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Services.Zebra.Commands
{
	public class CutCommand : BaseCommand
	{
		public CutCommand()
		{
			SetMessage(zpl);
			IsOneWay = true;
		}

		private const string zpl = @"
^XA
^MMC,Y
^FO50,50^A0N,150,150^FDCutter enabled^FS
^XZ
";
	}

	/*
	 * Based on feedback from Zebra: It is necesary to calibrate media and manually adjust cut position with the Tear-Off command: ~TA
	 * This is a trial and error process and cannot be automated. Once the tear off position is calibrated correctly, the cutter will
	 * work as expected, cutting in between labels.
	 * 
	 */


}
