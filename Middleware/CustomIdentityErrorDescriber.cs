using Microsoft.AspNetCore.Identity;
using Service.Contracts;

namespace Print.Middleware
{
	public class CustomIdentityErrorDescriber : IdentityErrorDescriber
	{
		private ILocalizationService g;

		public CustomIdentityErrorDescriber(ILocalizationService g)
		{
			this.g = g;
		}

		public override IdentityError DefaultError() { return new IdentityError { Code = nameof(DefaultError), Description = g["An unknown failure has occurred."] }; }
		public override IdentityError ConcurrencyFailure() { return new IdentityError { Code = nameof(ConcurrencyFailure), Description = g["Optimistic concurrency failure, object has been modified in another thread."] }; }
		public override IdentityError PasswordMismatch() { return new IdentityError { Code = nameof(PasswordMismatch), Description = g["Incorrect password."] }; }
		public override IdentityError InvalidToken() { return new IdentityError { Code = nameof(InvalidToken), Description = g["Invalid token."] }; }
		public override IdentityError LoginAlreadyAssociated() { return new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = g["A user with this name already exists."] }; }
		public override IdentityError InvalidUserName(string userName) { return new IdentityError { Code = nameof(InvalidUserName), Description = g["User name is invalid, can only contain letters or digits."] }; }
		public override IdentityError InvalidEmail(string email) { return new IdentityError { Code = nameof(InvalidEmail), Description = g["Email is invalid."] }; }
		public override IdentityError DuplicateUserName(string userName) { return new IdentityError { Code = nameof(DuplicateUserName), Description = g["User Name is already taken."] }; }
		public override IdentityError DuplicateEmail(string email) { return new IdentityError { Code = nameof(DuplicateEmail), Description = g["Email is already taken."] }; }
		public override IdentityError InvalidRoleName(string role) { return new IdentityError { Code = nameof(InvalidRoleName), Description = g["Role name is invalid."] }; }
		public override IdentityError DuplicateRoleName(string role) { return new IdentityError { Code = nameof(DuplicateRoleName), Description = g["Role name is already taken."] }; }
		public override IdentityError UserAlreadyHasPassword() { return new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = g["User already has a password set."] }; }
		public override IdentityError UserLockoutNotEnabled() { return new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = g["Lockout is not enabled for this user."] }; }
		public override IdentityError UserAlreadyInRole(string role) { return new IdentityError { Code = nameof(UserAlreadyInRole), Description = g["User already in role."] }; }
		public override IdentityError UserNotInRole(string role) { return new IdentityError { Code = nameof(UserNotInRole), Description = g["User is not in role."] }; }
		public override IdentityError PasswordTooShort(int length) { return new IdentityError { Code = nameof(PasswordTooShort), Description = g["Passwords must be at least 8 characters."] }; }
		public override IdentityError PasswordRequiresNonAlphanumeric() { return new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = g["Passwords must have at least one non alphanumeric character."] }; }
		public override IdentityError PasswordRequiresDigit() { return new IdentityError { Code = nameof(PasswordRequiresDigit), Description = g["Passwords must have at least one digit ('0'-'9')."] }; }
		public override IdentityError PasswordRequiresLower() { return new IdentityError { Code = nameof(PasswordRequiresLower), Description = g["Passwords must have at least one lowercase ('a'-'z')."] }; }
		public override IdentityError PasswordRequiresUpper() { return new IdentityError { Code = nameof(PasswordRequiresUpper), Description = g["Passwords must have at least one uppercase ('A'-'Z')."] }; }
	}
}
