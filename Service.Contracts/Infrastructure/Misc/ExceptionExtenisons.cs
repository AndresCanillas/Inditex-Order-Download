using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Service.Contracts
{
	public static class ExceptionExtenisons
	{
		public static Exception GetRoot(this Exception ex)
		{
			while (ex.InnerException != null) ex = ex.InnerException;
			return ex;
		}

		public static string MinimalStackTrace(this Exception exception)
		{
			if (exception == null || String.IsNullOrWhiteSpace(exception.StackTrace))
				return string.Empty;

			StringBuilder result = new StringBuilder();
			string[] lines = exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			Regex regex = new Regex(@"\sin\s(.+):line\s(\d+)", RegexOptions.Compiled);

			foreach (string line in lines)
			{
				Match match = regex.Match(line);
				if (match.Success)
				{
					string fileName = match.Groups[1].Value;
					string lineNumber = match.Groups[2].Value;
					int idx = fileName.LastIndexOf("\\");
					if(idx > 0)
					{
						fileName = fileName.Substring(idx + 1);
					}
					result.AppendLine($"{fileName}@{lineNumber}");
				}
			}

			return result.ToString().TrimEnd();
		}
	}
}
