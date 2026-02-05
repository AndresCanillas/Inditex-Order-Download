using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Print.Middleware
{
	public class ServerEndPoint
	{
		public int Port;
		public bool UseSSL;
		public string SSLSource;
		public string CertName;
		public bool Redirect;
		public string RedirectPort;
		public string RedirectProtocol;
	}
}
