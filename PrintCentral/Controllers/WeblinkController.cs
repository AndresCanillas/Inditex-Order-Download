using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Core;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Controllers
{
	public class WeblinkController : Controller
    {
		private ILogService log;
		private IWSConnectionManager manager;

        public WeblinkController(
			ILogService log,
			IWSConnectionManager manager)
		{
			this.log = log;
			this.manager = manager;
		}

		public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.WebSockets.IsWebSocketRequest)
                {
					var ip = HttpContext.Connection.RemoteIpAddress.ToString();
                    log.LogMessage($"Received WS connection from {ip}.");
                    if (HttpContext.Request.Headers.ContainsKey("Sec-WebSocket-Protocol"))
                    {
                        var wsprotocol = HttpContext.Request.Headers["Sec-WebSocket-Protocol"];
                        HttpContext.Response.Headers.Add("Sec-WebSocket-Protocol", wsprotocol);
                    }
                    WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await manager.Accept(webSocket, ip);
                }
                else
                {
                    HttpContext.Response.StatusCode = 400;
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
			return new EmptyResult();
		}
	}
}