using System.IO;

namespace Services.Core
{
	public static class StringExtensions
	{
		public static string SanitizeFileName(this string fileName)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
		}


		public static string Sanitize(this string input)
		{
			if(input == null)
				return null;

			return input.Replace("'", "''").Replace("--", string.Empty);
		}
	}
}
