using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Service.Contracts;
using WebLink.Contracts;
using Middleware;
using Services.Core;

namespace WebLink.Controllers
{
	[Authorize]
	public class EventsController : Controller
	{
		private IEventQueue events;
		private IUserData userData;
		private ILogService log;
		private EventForwardingOptions options;

		public EventsController(
			IEventQueue events,
			IUserData userData,
			ILogService log,
			EventForwardingOptions options)
		{
			this.events = events;
            this.userData = userData;
			this.log = log;
			this.options = options;
		}

		[Route("/events/listen")]
		public async Task<IActionResult> Listen()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				await ProcessEventNotifications(socket, User);
				return new EmptyResult();
			}
			else return new BadRequestResult();
		}


		private async Task ProcessEventNotifications(WebSocket socket, ClaimsPrincipal user)
		{
			WebSocketReceiveResult result;
			Action<EQEventInfo> eventHandler = (e) => {
				if (options.IsAllowed(e) )
				{
					if (e.EventName == "OrderEntityEvent"
					|| userData.IsIDT
					|| (e.CompanyID == userData.SelectedCompanyID  ))
					{
						socket.SendAsync(
							new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e))),
							WebSocketMessageType.Text, true, CancellationToken.None);
					}
				}
				
			};

			events.OnEventRegistered += eventHandler;

			try
			{
				var buffer = new byte[1024];
				do
				{
					result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				} while (socket.State == WebSocketState.Open && result.CloseStatus == null);
			}
            catch (Exception)
            {
                //log.LogException("Error while sending Event Notification to the client.", ex); // disabled this log, is not required

            }
            finally
			{
				events.OnEventRegistered -= eventHandler;
			}
		}
	}
}