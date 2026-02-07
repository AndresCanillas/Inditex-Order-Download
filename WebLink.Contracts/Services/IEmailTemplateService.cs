using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts
{
	public interface IEmailTemplateService
	{
		IEmail CreateFromTemplate<T>(string templateName, T data);
	}

	public interface IEmail
	{
		string To { get; set; }
		string Subject { get; set; }
		void EmbbedImage(string resourceID, string imagePath);
		void AttachFile(string filePath);
		Task SendAsync();
	}
}
