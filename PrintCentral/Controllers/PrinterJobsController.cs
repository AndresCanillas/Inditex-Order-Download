using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Middleware;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
    public class PrinterJobsController : Controller
    {
		private ILogService log;
		private IPrinterJobRepository repo;
		private IOrderRepository orderRepo;
		private IEventQueue events;
		private IZPrinterManager printerManager;
		private UserManager<Contracts.Models.AppUser> userManager;
		private IUserDataCacheService userDataCache;
		private IDynamicExportClient exportClient;
		private ILocalizationService g;

		public PrinterJobsController(
			ILogService log,
			IPrinterJobRepository repo,
			IOrderRepository orderRepo,
			IEventQueue events,
			IZPrinterManager printerManager,
			UserManager<Contracts.Models.AppUser> userManager,
			IUserDataCacheService userDataCache,
			IDynamicExportClient exportClient,
			ILocalizationService g)
		{
			this.log = log;
			this.repo = repo;
			this.orderRepo = orderRepo;
			this.events = events;
			this.printerManager = printerManager;
			this.userManager = userManager;
			this.userDataCache = userDataCache;
			this.exportClient = exportClient;
			this.g = g;
		}


		[HttpGet]
		[Route("/printerjobs/getbyid/{jobid}")]
		public IPrinterJob GetByID(int jobid)
		{
			try
			{
				return repo.GetByID(jobid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet]
		[Route("/printerjobs/GetPendingJobs")]
		public IEnumerable<JobHeaderDTO> GetPendingJobs()
		{
			try
			{
				return repo.GetPendingJobs();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpPost]
		[Route("/printerjobs/FilterPrinterJobs")]
		public OperationResult FilterPrinterJobs([FromBody]PrinterJobFilter filter)
		 {
			try
			{
				return new OperationResult(true, "", repo.GetPendingJobs(filter));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, "Could not retrieve information from server.");
			}
		}


		[HttpGet]
		[Route("/printerjobs/GetPrinterJobs/{id}")]
		public IEnumerable<JobHeaderDTO> GetPrinterJobs(int id)
		{
			try
			{
				var result = repo.GetPrinterJobs(id);
				return result;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet]
		[Route("/PrinterJobs/GetJobDetails/{id}/{applySort}")]
        public List<PrintJobDetailDTO> GetJobDetails(int id, bool applySort)
		{
			try
			{
				return new List<PrintJobDetailDTO>(repo.GetJobDetails(id, applySort));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpPost]
		[Route("/PrinterJobs/UpdateOrderDueDate")]
		public OperationResult UpdateOrderDueDate([FromBody]JobHeaderDTO jobData)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!User.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager, Roles.SysAdmin, Roles.IDTProdManager))
					return OperationResult.Forbid;
				repo.SetDueDate(jobData.JobID, jobData.DueDate.Value);
				return new OperationResult(true, g["Due date updated!"]);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("/PrinterJobs/AssignLocation")]
		public OperationResult AssignLocation([FromBody]JobHeaderDTO jobData)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!User.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager, Roles.SysAdmin, Roles.IDTProdManager))
					return OperationResult.Forbid;
				repo.SetJobLocation(jobData.JobID, jobData.ProductionLocationID.Value);
				return new OperationResult(true, g["Job assigned to factory"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("/PrinterJobs/AssignPrinter")]
		public OperationResult AssignPrinter([FromBody]JobHeaderDTO jobData)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_PrintJobsReport)
					return OperationResult.Forbid;
				repo.SetJobPrinter(jobData.JobID, jobData.ProductionLocationID.Value, jobData.AssignedPrinter.Value);
				return new OperationResult(true, g["Job assigned to printer"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("/printerjobs/startjob/{id}")]
		public OperationResult StartJob(int id)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.StartJob(id);
				return new OperationResult(true, g["Job Started"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}

		[HttpPost]
		[Route("/printerjobs/pausejob/{id}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult PauseJob(int id)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.PauseJob(id);
				return new OperationResult(true, g["Job Paused"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("/printerjobs/canceljob/{id}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult CancelJob(int id)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				log.LogMessage($"Cancelling job (ID {id})");
                var job = repo.GetByID(id);
				repo.CancelJob(id);
                events.Send(new OrderChangeStatusEvent() { OrderID = job.CompanyOrderID, OrderStatus = (int)OrderStatus.Cancelled});
                return new OperationResult(true, g["Job Cancelled"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

        [HttpPost]
		[Route("/printerjobs/ActivateJob/{id}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
        public OperationResult ActivateJob(int id)
        {
            try
            {
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.ActivateJob(id);
                return new OperationResult(true, g["Job Activated"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost]
		[Route("/printerjobs/changeprinter/{jobid}/{printerid}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult ChangePrinter(int jobid, int printerid)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.ChangePrinter(jobid, printerid);
				return new OperationResult(true, g["Job moved successfully!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("/printerjobs/AddExtras")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult AddExtras([FromBody]ExtrasRequest rq)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				var job = repo.GetByID(rq.JobID);
				if (job == null)
					return new OperationResult(false, g["Job does not exist or user does not have permissions to access it."]);
				if(job.Status != JobStatus.Cancelled)
					return new OperationResult(true, g["Extra labels added"], repo.AddExtras(job.ID, rq.DetailID, rq.Quantity));
				else
					return new OperationResult(false, g["Cannot add extas because this job has been cancelled."]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost]
		[Route("PrinterJobs/PrintSample/{projectid}/{printerid}/{articleid}/{orderid}/{detailid}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult PrintSample(int projectid, int printerid, int articleid, int orderid, int detailid)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				var printer = printerManager.GetPrinter(printerid);
				if (printer == null)
					return new OperationResult(false, g["Printer is offline."]);
				printer.PrintSample(projectid, articleid, orderid, detailid);
				return new OperationResult(true, g["Sample sent to printer"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost]
		[Route("PrinterJobs/ResetJob/{jobid}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult ResetJob(int jobid)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.ResetProgress(jobid);
				return new OperationResult(true, g["Print job has been reset!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost]
		[Route("PrinterJobs/SetDetailProgress/{detailid}/{progress}")]
		[AuthorizeRoles(Roles.SysAdmin, Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator)]
		public OperationResult SetDetailProgress(int detailid, int progress)
		{
			try
			{
				var userData = userDataCache.GetUserData(User);
				if (!userData.CanSeeVMenu_Printers)
					return OperationResult.Forbid;
				repo.SetDetailProgress(detailid, progress);
				return new OperationResult(true, g["Task progress updated!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		public async Task<IActionResult> Notifications()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				await ProcessJobStatusNotifications(socket, User);
				return new EmptyResult();
			}
			else return new BadRequestResult();
		}


		private async Task ProcessJobStatusNotifications(WebSocket socket, ClaimsPrincipal user)
		{
			WebSocketReceiveResult result;
			Action<PrinterJobEvent> eventHandler = (e) => {
				var userData = userDataCache.GetUserData(user);
				if (e.CompanyID == 0 || e.CompanyID == userData.SelectedCompanyID || user.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager))
				{
					socket.SendAsync(
						new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e))),
						WebSocketMessageType.Text, true, CancellationToken.None);
				}
			};
			var token = events.Subscribe<PrinterJobEvent>((e) => eventHandler(e));
			try
			{
				var buffer = new byte[1024];
				do
				{
					result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				} while (socket.State == WebSocketState.Open && result.CloseStatus == null);
			}
			catch (Exception ex)
			{
				log.LogException("Error while sending PrinterJob status updates to the client.", ex);
			}
			finally
			{
				events.Unsubscribe<PrinterJobEvent>(token);
			}
		}
    }

	public class ExtrasRequest
	{
		public int JobID { get; set; }
		public int DetailID { get; set; }
		public int Quantity { get; set; }
	}
}