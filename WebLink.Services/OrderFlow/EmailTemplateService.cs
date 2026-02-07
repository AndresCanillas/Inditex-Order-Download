using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using SendGrid.Helpers.Mail;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Services
{
	public class EmailTemplateService : IEmailTemplateService
	{
		private IFactory factory;

		public EmailTemplateService(IFactory factory)
		{
			this.factory = factory;
		}

		public IEmail CreateFromTemplate<T>(string templateName, T data)
		{
			var msg = factory.GetInstance<EmailObject>();
			msg.Body = ProcessTemplate(templateName, data);
			return msg;
		}

		private string ProcessTemplate<T>(string templateName, T data)
		{
			var dir = Path.GetDirectoryName(typeof(OrderEmailService).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\"));
			var template = Path.Combine(dir, templateName);
			var templateContent = File.ReadAllText(template);
			StringBuilder sb = new StringBuilder((int)(templateContent.Length * 1.2));
			int pos = 0;
			int idx1 = templateContent.IndexOf("@(");
			if (idx1 < 0)
				return templateContent;
			while (idx1 > 0)
			{
				idx1 += 2;
				int idx2 = templateContent.IndexOf(")", idx1);
				if (idx2 > idx1)
				{
					string propertyName = templateContent.Substring(idx1, idx2 - idx1);
					var value = Reflex.GetMember(data, propertyName);
					if (value == null)
						value = "";
					sb.Append(templateContent.Substring(pos, idx1 - pos - 2));
					sb.Append(value.ToString());
					pos = idx2 + 1;
					idx1 = templateContent.IndexOf("@(", pos);
				}
				else
				{
					break;
				}
			}
			sb.Append(templateContent.Substring(pos));
			return sb.ToString();
		}
	}


	public class EmailObject: IEmail
	{
		private IAppInfo appInfo;
		private ILogService log;
		private bool enabled;
		private string apiKey;
		private SendGridMessage msg;

		public EmailObject(IAppConfig config, IAppInfo appInfo, ILogService log)
		{
			this.appInfo = appInfo;
			this.log = log;
			enabled = config.GetValue<bool>("WebLink.Email.Enabled");
			apiKey = config.GetValue<string>("WebLink.Email.Key");
			msg = new SendGridMessage()
			{
				From = new EmailAddress("noreply@indetgroup.com"),
			};
		}

		public string Body { get; set; }

		public string To { get; set; }

		public string Cc { get; set; }

		public string Subject { get; set; }

		public void EmbbedImage(string resourceID, string relativePath)
		{
			var fullFilePath = Path.Combine(appInfo.AssemblyDir, relativePath);

			var attchment = new Attachment()
			{
				Filename = Path.GetFileName(fullFilePath),
				ContentId = resourceID,
				Disposition = "inline",
				Content = Convert.ToBase64String(File.ReadAllBytes(fullFilePath)),
				Type = MimeTypes.GetMimeType(Path.GetExtension(fullFilePath))
			};
			msg.AddAttachment(attchment);
		}

		public void AttachFile(string filePath)
		{
			var attchment = new Attachment()
			{
				Filename = Path.GetFileName(filePath),
				Disposition = "attachment",
				Content = Convert.ToBase64String(File.ReadAllBytes(filePath)),
				Type = MimeTypes.GetMimeType(Path.GetExtension(filePath)),
			};
			msg.AddAttachment(attchment);
		}

		public async Task SendAsync()
		{
			if (!enabled)
			{
				// Make sure we only send notifications if the WebLink.Email.Enabled flag is set to true
				log.LogMessage($"System tried to send a message to: {To}. This message was not Sent because the 'Enabled' flag in Email settings is set to false.");
				return;
			}

			string[] list = To.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string address in list)
			{
				if (!String.IsNullOrWhiteSpace(address) && IsValidEmailAddress(address))
				{
					try
					{
						msg.AddTo(address);
					}
					catch (Exception ex)
					{
						throw new Exception("Email address -To- " + address + " was not accepted by the underlying SMTP system.", ex);
					}
				}
			}
			if (!String.IsNullOrWhiteSpace(Cc))
			{
				list = Cc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string address in list)
				{
					if (!String.IsNullOrWhiteSpace(address) && IsValidEmailAddress(address))
					{
						try
						{
							msg.AddCc(address);
						}
						catch (Exception ex)
						{
							throw new Exception("Email address -Cc- " + address + " was not accepted by the underlying SMTP system.", ex);
						}
					}
				}
			}
			msg.Subject = Subject;
			msg.HtmlContent = Body;


#if !DEBUG
            var client = new SendGridClient(apiKey);
			var response = await client.SendEmailAsync(msg);
			if (!response.IsSuccessStatusCode)
            {
                var ex = new Exception($@"Could not send email 
                Subject: {Subject}
                To:  '{To}' - Cc: '{Cc}' {Environment.NewLine} 
                The response was: {response.StatusCode}");
                log.LogException("problema para enviar email {0}", ex);
                log.LogMessage("SendGrid Response Headers [{0}]", response.Headers.ToString());
                log.LogMessage("SendGrid Response Body [{0}]", response.Body.ReadAsStringAsync().Result);

                throw ex;
            }
#else
            await Task.FromResult(HttpStatusCode.Accepted);
#endif
        }



		// Pulled from https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
		public static bool IsValidEmailAddress(string email)
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
