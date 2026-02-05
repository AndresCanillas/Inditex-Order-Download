using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class StackTraceExtensions
	{
		public static string ToShortString(this StackTrace stackTrace)
		{
			var sb = new StringBuilder(1000);

			foreach (StackFrame frame in stackTrace.GetFrames())
				sb.AppendLine($"{frame?.GetMethod()?.Name} ({Path.GetFileName(frame?.GetFileName())} @ {frame?.GetFileLineNumber()})");

			return sb.ToString();
		}
	}
}
