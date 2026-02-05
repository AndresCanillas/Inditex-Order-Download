using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Services.Core
{
	public static class ValidationHelper
	{
		public const string PasswordComplexityPolicy = "Invalid password: Password must be at least 8 characters long, include at least one digit, one lower case and one upper case letter.";
		public const string SamePasswordPolicy = "The new password cannot be the same as the old password.";

		public static bool IsValidIdentifier(string str)
		{
			if(string.IsNullOrWhiteSpace(str))
				throw new InvalidOperationException($"The string '{str}' is not a valid identifier.");

			string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
			return Regex.IsMatch(str, pattern);
		}

		public static void ValidateIdentifier(string str)
		{
			if(!IsValidIdentifier(str))
				throw new InvalidOperationException($"The string '{str}' is not a valid identifier.");
		}

		public static bool IsValidPassword(string password)
		{
			if(password.Length < 8)
				return false;
			if(!password.Any(c => Char.IsDigit(c)))
				return false;
			if(!password.Any(c => Char.IsUpper(c)))
				return false;
			if(!password.Any(c => Char.IsLower(c)))
				return false;
			return true;
		}

		public static void ValidatePassword(string password)
		{
			if(!IsValidPassword(password))
				throw new InvalidOperationException(PasswordComplexityPolicy);
		}
	}
}
