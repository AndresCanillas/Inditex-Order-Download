//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Service.Contracts;
//using Service.Contracts.Messaging;

//namespace WebLink.Controllers
//{
//	[Authorize]
//	public class DataSyncController : Controller
//	{
//		private IFactory factory;
//		private ILogService log;
//		private DataSyncServiceConfig cfg;

//		public DataSyncController(IFactory factory, IAppConfig config, ILogService log)
//		{
//			this.factory = factory;
//			this.log = log.GetSection("DataSynchronization");
//			cfg = config.Bind<DataSyncServiceConfig>("DataSyncService");
//		}


//		[Route("/api/datasync")]
//		public async Task<IActionResult> DataSync()
//		{
//			try
//			{
//				if (HttpContext.WebSockets.IsWebSocketRequest && cfg.Operation.Mode == "Passive")
//				{
//					var ip = HttpContext.Connection.RemoteIpAddress.ToString();
//					using (var socket = await HttpContext.WebSockets.AcceptWebSocketAsync())
//					{
//						var dataSyncWS = new DataSyncWebSocket(factory, socket);
//						var statusCode = await dataSyncWS.HandshakeAsServer(ip);
//						return new StatusCodeResult(statusCode);
//					}
//				}
//				else return new BadRequestResult();
//			}
//			catch (Exception ex)
//			{
//				log.LogException(ex);
//				return new BadRequestResult();
//			}
//		}
//	}
//}