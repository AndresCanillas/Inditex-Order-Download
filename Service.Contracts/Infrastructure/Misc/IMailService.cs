using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;

namespace Service.Contracts
{
    [Obsolete("Replace for SendGrid - Not Working")]
	public interface IMailService
	{
		bool IsValidAddress(string email);
		void Enqueue(string to, string subject, string body);
		void SendMail(string to, string cc, string subject, string body);
		Task SendMailAsync(string to, string cc, string subject, string body);
		void SendHtmlMail(string to, string cc, string subject, string body, List<LinkedResource> resources);
		Task SendHtmlMailAsync(string to, string cc, string subject, string body, List<LinkedResource> resources);
	}

	public class MailService : IMailService
	{
		class EmailInfo
		{
			public string To;
			public string Subject;
			public string Body;
			public bool IsHtml;

			public EmailInfo(string to, string subject, string body, bool isHtml)
			{
				To = to;
				Subject = subject;
				Body = body;
				IsHtml = isHtml;
			}
		}

		private ConcurrentQueue<EmailInfo> pendingMessages;
		private ILogService log;
		private IAppConfig config;
		private Timer timer;
		private int loggedWarningCount = 0;

		public MailService(ILogService log, IAppConfig config)
		{
			this.log = log;
			this.config = config;
			pendingMessages = new ConcurrentQueue<EmailInfo>();
			timer = new Timer(CheckPendingMessages, null, (int)TimeSpan.FromSeconds(10).TotalMilliseconds, Timeout.Infinite);
		}


		private void CheckPendingMessages(object state)
		{
			EmailInfo email = null;
			bool emailConfigIsValid = GetEmailConfig(out _, out _, out _, out _, out _);
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				if (pendingMessages.TryDequeue(out email))
				{
					SendMail(email.To, null, email.Subject, email.Body);
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (email != null)
					pendingMessages.Enqueue(email);
			}
			finally
			{
				if (pendingMessages.Count > 0 && emailConfigIsValid)
					timer.Change((int)TimeSpan.FromSeconds(5).TotalMilliseconds, Timeout.Infinite);
				else
					timer.Change((int)TimeSpan.FromSeconds(30).TotalMilliseconds, Timeout.Infinite);
			}
		}


		static Regex emailRegex = new Regex(@"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
			+ @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
			+ @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
			+ @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$");

		/// <summary>
		/// Checks whether the given input is a valid email address.
		/// </summary>
		/// <param name="email">The email address to validate.</param>
		public bool IsValidAddress(string email)
		{
			if (String.IsNullOrEmpty(email))
				return false;
			else
			{
				if (email.IndexOfAny(new char[] { ',', ';' }) >= 0)
				{
					string[] emails = email.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
					if (emails != null && emails.Length > 0)
					{
						foreach (string str in emails)
						{
							if (!emailRegex.IsMatch(str))
								return false;
						}
						return true;
					}
					else return false;
				}
				else
				{
					return emailRegex.IsMatch(email);
				}
			}
		}


		public void Enqueue(string to, string subject, string body)
		{
			if (String.IsNullOrWhiteSpace(to))
				throw new Exception("to cannot be null or empty.");
			if (String.IsNullOrWhiteSpace(subject))
				throw new Exception("subject cannot be null or empty.");
			if (String.IsNullOrWhiteSpace(body))
				throw new Exception("body cannot be null or empty.");
			pendingMessages.Enqueue(new EmailInfo(to, subject, body, true));
		}


		private bool GetEmailConfig(out string smtpServer, out int port, out bool useSSL, out string account, out string password)
		{
			var enabled = config.GetValue("WebLink.Email.Enabled", false);
			smtpServer = config.GetValue("WebLink.Email.Server", "");
			port = config.GetValue("WebLink.Email.Port", 25);
			useSSL = config.GetValue("WebLink.Email.UseSSL", false);
			account = config.GetValue("WebLink.Email.FromAddress", "");
			password = config.GetValue("WebLink.Email.Password", "");

			if (!enabled)
			{
				if(loggedWarningCount % 100 == 0)
					log.LogWarning("SMTP configuration is disabled. Email messages will not be sent.");
				loggedWarningCount++;
				return false;
			}
			if (String.IsNullOrWhiteSpace(smtpServer) || String.IsNullOrWhiteSpace(account))
			{
				if (loggedWarningCount % 100 == 0)
					log.LogWarning("Configuration Error: SMTP configuration no set. Cannot send email messages.");
				loggedWarningCount++;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Sends an email using the provided parameters.
		/// </summary>
		/// <param name="to">The addresses that are to receive the message. Can be a comma or semicolon delimited list.</param>
		/// <param name="cc">The addresses that are to receive the message as CC. Can be a comma or semicolon delimited list.</param>
		/// <param name="subject">The subject of the message.</param>
		/// <param name="body">The body of the message.</param>
		public void SendMail(string to, string cc, string subject, string body)
		{
			SendMailAsync(to, cc, subject, body).Wait();
		}

		/// <summary>
		/// Sends an email using the provided parameters.
		/// </summary>
		/// <param name="to">The addresses that are to receive the message. Can be a comma or semicolon delimited list.</param>
		/// <param name="cc">The addresses that are to receive the message as CC. Can be a comma or semicolon delimited list.</param>
		/// <param name="subject">The subject of the message.</param>
		/// <param name="body">The body of the message.</param>
		public async Task SendMailAsync(string to, string cc, string subject, string body)
		{
			if (String.IsNullOrWhiteSpace(to))
				throw new InvalidOperationException("Argument 'to' cannot be null or empty");

			if (String.IsNullOrWhiteSpace(subject))
				subject = "No Subject";

			if (GetEmailConfig(out var server, out var port, out var useSSL, out var account, out var password))
			{
				MailMessage msg = new MailMessage();
				msg.IsBodyHtml = true;
				try
				{
					msg.From = new MailAddress(account);
				}
				catch (Exception ex)
				{
					throw new Exception("Email address -From- " + account + " was not accepted by the underlying SMTP system.", ex);
				}
				string[] list = to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string address in list)
				{
					if (!String.IsNullOrWhiteSpace(address))
					{
						MailAddress ma;
						try
						{
							ma = new MailAddress(address);
						}
						catch (Exception ex)
						{
							throw new Exception("Email address -To- " + address + " was not accepted by the underlying SMTP system.", ex);
						}
						if (!msg.To.Contains(ma))
							msg.To.Add(address);
					}
				}
				if (!String.IsNullOrWhiteSpace(cc))
				{
					list = cc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string address in list)
					{
						if (address != null && address.Trim() != "")
							msg.CC.Add(address);
					}
				}
				msg.Subject = subject;
				msg.Body = body;
				SmtpClient smtp = new SmtpClient(server, port);
				smtp.EnableSsl = useSSL;
				if (!String.IsNullOrWhiteSpace(password))
					smtp.Credentials = new NetworkCredential(account, password);
				await smtp.SendMailAsync(msg);
			}
		}


		/// <summary>
		/// Sends an email using the provided parameters.
		/// </summary>
		/// <param name="to">The addresses that are to receive the message. Can be a comma or semicolon delimited list.</param>
		/// <param name="cc">The addresses that are to receive the message as CC. Can be a comma or semicolon delimited list.</param>
		/// <param name="subject">The subject of the message.</param>
		/// <param name="body">The body of the message.</param>
		/// <param name="resources">A list of resources referenced by the email body (can be null or empty).</param>
		public void SendHtmlMail(string to, string cc, string subject, string body, List<LinkedResource> resources)
		{
			SendHtmlMailAsync(to, cc, subject, body, resources).Wait();
		}

		/// <summary>
		/// Sends an email using the provided parameters.
		/// </summary>
		/// <param name="to">The addresses that are to receive the message. Can be a comma or semicolon delimited list.</param>
		/// <param name="cc">The addresses that are to receive the message as CC. Can be a comma or semicolon delimited list.</param>
		/// <param name="subject">The subject of the message.</param>
		/// <param name="body">The body of the message.</param>
		/// <param name="resources">A list of resources referenced by the email body (can be null or empty).</param>
		public async Task SendHtmlMailAsync(string to, string cc, string subject, string body, List<LinkedResource> resources)
		{
			if (GetEmailConfig(out var server, out var port, out var useSSL, out var account, out var password))
			{
				MailMessage msg = new MailMessage();
				try
				{
					msg.From = new MailAddress(account);
				}
				catch (Exception ex)
				{
					throw new Exception("Email address -From- " + account + " was not accepted by the underlying SMTP system.", ex);
				}
				string[] list = to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string address in list)
				{
					if (!String.IsNullOrWhiteSpace(address))
					{
						MailAddress ma;
						try
						{
							ma = new MailAddress(address);
						}
						catch (Exception ex)
						{
							throw new Exception("Email address -To- " + address + " was not accepted by the underlying SMTP system.", ex);
						}
						if (!msg.To.Contains(ma))
							msg.To.Add(address);
					}
				}
				if (!String.IsNullOrWhiteSpace(cc))
				{
					list = cc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string address in list)
					{
						if (address != null && address.Trim() != "")
							msg.CC.Add(address);
					}
				}
				var view = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
				msg.AlternateViews.Add(view);
				if (resources != null)
				{
					foreach (var res in resources)
					{
						view.LinkedResources.Add(res);
					}
				}
				msg.Subject = subject;
				SmtpClient smtp = new SmtpClient(server, port);
				smtp.EnableSsl = useSSL;
				if (!String.IsNullOrWhiteSpace(password))
					smtp.Credentials = new NetworkCredential(account, password);
				await smtp.SendMailAsync(msg);
			}
		}


		// Pulled from https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
		public static bool IsValidEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return false;

			try
			{
				// Normalize the domain
				email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
									  RegexOptions.None, TimeSpan.FromMilliseconds(200));

				// Examines the domain part of the email and normalizes it.
				string DomainMapper(Match match)
				{
					// Use IdnMapping class to convert Unicode domain names.
					var idn = new IdnMapping();

					// Pull out and process domain name (throws ArgumentException on invalid)
					var domainName = idn.GetAscii(match.Groups[2].Value);

					return match.Groups[1].Value + domainName;
				}
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
			catch (ArgumentException)
			{
				return false;
			}

			try
			{
				return Regex.IsMatch(email,
					@"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
					@"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
					RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
		}
	}
}

