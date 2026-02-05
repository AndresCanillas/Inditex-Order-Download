using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class CmdLineHelper
	{
		public static string ExtractCmdArgument(string argName)
		{
			return ExtractCmdArgument(Environment.CommandLine, argName, true, null);
		}


		public static string ExtractCmdArgument(string argName, bool required)
		{
			return ExtractCmdArgument(Environment.CommandLine, argName, required, null);
		}

		public static string ExtractCmdArgument(string argName, bool required, string defaultValue)
		{
			return ExtractCmdArgument(Environment.CommandLine, argName, required, defaultValue);
		}

		public static string ExtractCmdArgument(string commandLine, string argName, bool required)
		{
			return ExtractCmdArgument(commandLine, argName, required, null);
		}


		public static string ExtractCmdArgument(string commandLine, string argName, bool required, string defaultValue)
		{
			string searchTerm;
			if (argName.StartsWith("/"))
				searchTerm = argName + "=";
			else
				searchTerm = "/" + argName + "=";
			int index = commandLine.IndexOf(searchTerm);
			if (index >= 0)
			{
				index += searchTerm.Length;
				if (commandLine[index] != '"')
				{
					int endIndex = commandLine.IndexOf(' ', index);
					if (endIndex < 0) endIndex = commandLine.Length;
					if (index < endIndex)
					{
						return commandLine.Substring(index, endIndex - index);
					}
				}
				else
				{
					index++;
					int endIndex = commandLine.IndexOf('"', index);
					if (endIndex < 0) throw new Exception("Found an unterminated string in the command line.");
					else if (index <= endIndex)
					{
						return commandLine.Substring(index, endIndex - index);
					}
				}
			}
			if (required)
				throw new Exception("Required argument was not supplied: " + argName);
			else
				return defaultValue;
		}

        public static string RunCommand(
            string command,
            string arguments,
            string workdir = null,
            bool asAdmin = false,
            bool hidden = true,
            bool echo = false,
            bool waitForCompletion = true)
        {
            var p = new Process();
            var info = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = hidden,
                WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };

            if(workdir != null)
                info.WorkingDirectory = workdir;

            if(!(Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major < 6) && asAdmin)
                info.Verb = "runas";

            p.StartInfo = info;

            if(echo && !info.FileName.EndsWith("QuickDeploy"))
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine($"{info.FileName} {info.Arguments}");
                Console.WriteLine();
                Console.ForegroundColor = color;
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            p.OutputDataReceived += (sender, args) =>
            {
                if(args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                    if(echo)
                        Console.WriteLine(args.Data);
                }
            };

            p.ErrorDataReceived += (sender, args) =>
            {
                if(args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                }
            };

            p.Start();

            // Begin reading output and error streams asynchronously
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if(waitForCompletion)
            {
                p.WaitForExit();

                if(p.ExitCode != 0)
                {
                    throw new Exception($"{outputBuilder}\r\n{errorBuilder}\r\nCommand failed with ExitCode {p.ExitCode}");
                }

                return outputBuilder.ToString();
            }

            return string.Empty;
        }
    }
}
