using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterError : EnumAttributes
	{
		public static PrinterError NONE;

		public static PrinterError MEDIA_OUT;

		public static PrinterError RIBBON_OUT;

		public static PrinterError HEAD_OPEN;

		public static PrinterError PRINTHEAD_SHUTDOWN;

		public static PrinterError MOTOR_OVERTEMP;

		public static PrinterError INVALID_HEAD;

		public static PrinterError THERMISTOR_FAULT;

		public static PrinterError PAPER_FEED_ERROR;

		public static PrinterError PAUSED;

		public static PrinterError BASIC_RUNTIME_ERROR;

		public static PrinterError BASIC_FORCED;

		private static List<PrinterError> allVals;

		static PrinterError()
		{
			PrinterError.NONE = new PrinterError(0, 0, "None");
			PrinterError.MEDIA_OUT = new PrinterError(2, 1, "Paper Out");
			PrinterError.RIBBON_OUT = new PrinterError(2, 2, "Ribbon Out");
			PrinterError.HEAD_OPEN = new PrinterError(2, 4, "Head Open");
			PrinterError.PRINTHEAD_SHUTDOWN = new PrinterError(2, 16, "Printhead Shutdown");
			PrinterError.MOTOR_OVERTEMP = new PrinterError(2, 32, "Motor Overtemp");
			PrinterError.INVALID_HEAD = new PrinterError(2, 128, "Invalid Head");
			PrinterError.THERMISTOR_FAULT = new PrinterError(2, 512, "Thermistor Fault");
			PrinterError.PAPER_FEED_ERROR = new PrinterError(2, 16384, "Paper Feed");
			PrinterError.PAUSED = new PrinterError(2, 65536, "Paused");
			PrinterError.BASIC_RUNTIME_ERROR = new PrinterError(2, 1048576, "Basic Runtime Error");
			PrinterError.BASIC_FORCED = new PrinterError(2, 2097152, "Basic Forced");
			PrinterError.allVals = new List<PrinterError>()
			{
				PrinterError.NONE,
				PrinterError.MEDIA_OUT,
				PrinterError.RIBBON_OUT,
				PrinterError.HEAD_OPEN,
				PrinterError.PRINTHEAD_SHUTDOWN,
				PrinterError.MOTOR_OVERTEMP,
				PrinterError.INVALID_HEAD,
				PrinterError.THERMISTOR_FAULT,
				PrinterError.PAPER_FEED_ERROR,
				PrinterError.PAUSED,
				PrinterError.BASIC_RUNTIME_ERROR,
				PrinterError.BASIC_FORCED
			};
		}

		private PrinterError(int segment, int value, string description) : base(segment, value, description)
		{
		}

		public static HashSet<PrinterError> GetEnumSetFromBitmask(int segment, int availableErrorBitfield)
		{
			HashSet<PrinterError> printerErrors = new HashSet<PrinterError>();
			foreach (PrinterError allVal in PrinterError.allVals)
			{
				if ((availableErrorBitfield & allVal.Value) == 0 || allVal.Segment != segment)
				{
					continue;
				}
				printerErrors.Add(allVal);
			}
			return printerErrors;
		}
	}
}