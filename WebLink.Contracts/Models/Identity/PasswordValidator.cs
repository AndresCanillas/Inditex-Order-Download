using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class PasswordValidator
	{
		private IdentityOptions options;

		public PasswordValidator(IdentityOptions options)
		{
			this.options = options;
		}

		public PasswordValidationResult Validate(string password)
		{
			if (password == null)
				throw new InvalidOperationException("password argument cannot be null");

			if (options.Password.RequireDigit && !password.Any(c => Char.IsDigit(c)))
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			if (options.Password.RequireLowercase && !password.Any(c => Char.IsLower(c)))
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			if (options.Password.RequireUppercase && !password.Any(c => Char.IsUpper(c)))
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			if (options.Password.RequireNonAlphanumeric && !password.Any(c => !Char.IsLetter(c) && !Char.IsNumber(c)))
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			if (options.Password.RequiredLength > 0 && password.Length < options.Password.RequiredLength)
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			if (options.Password.RequiredUniqueChars > 0 && password.DistinctChars() < options.Password.RequiredUniqueChars)
				return new PasswordValidationResult(false, options.PasswordRestrictions());
			return new PasswordValidationResult(true, null);
		}
	}

	public class PasswordValidationResult
	{
		public PasswordValidationResult(bool success, string error)
		{
			Success = success;
			ErrorMessage = error;
		}

		public bool Success { get; set; }
		public string ErrorMessage { get; set; }
	}

	public static class PWDValidationExtensions
	{
		public static bool Any(this string str, Predicate<char> predicate)
		{
			foreach(char c in str)
			{
				if (predicate(c))
					return true;
			}
			return false;
		}

		public static int DistinctChars(this string str)
		{
			List<char> found = new List<char>(str.Length);
			foreach (char c in str)
			{
				if (!found.Contains(c))
					found.Add(c);
			}
			return found.Count;
		}

		public static string PasswordRestrictions(this IdentityOptions options)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Password must have:");
			if (options.Password.RequireDigit)
				sb.AppendLine("  - A numerical character");
			if (options.Password.RequireLowercase)
				sb.AppendLine($"  - At least one lower case character");
			if (options.Password.RequireUppercase)
				sb.AppendLine($"  - At least one upper case character");
			if (options.Password.RequireNonAlphanumeric)
				sb.AppendLine($"  - At least one special character (that is not a number or alphabet character)");
			if (options.Password.RequiredUniqueChars > 0)
				sb.AppendLine($"  - Must contain more than {options.Password.RequiredUniqueChars} unique characters");
			if (options.Password.RequiredLength > 0)
				sb.AppendLine($"  - Be at least {options.Password.RequiredLength} characters long");
			return sb.ToString();
		}
	}
}
