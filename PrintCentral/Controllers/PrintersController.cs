using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.LabelService;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services.Zebra.Commands;

namespace WebLink.Controllers
{
	[Authorize]
	public class PrintersController : Controller
	{
		private IPrinterRepository repo;
		private IZPrinterManager printerManager;
		private ILocalizationService g;
		private ILogService log;
		private IBLabelServiceClient labelService;
        private IUserData userData;


        public PrintersController(
			IPrinterRepository repo,
			IZPrinterManager printerManager,
			IBLabelServiceClient labelService,
			ILocalizationService g,
			ILogService log,
            IUserData userData
            )
        {
			this.repo = repo;
			this.printerManager = printerManager;
			this.labelService = labelService;
			this.g = g;
			this.log = log;
            this.userData = userData;
        }

        [HttpPost, Route("/printers/insert")]
		public OperationResult Insert([FromBody]Printer data)
		{
			try
			{
                if (!userData.Admin_Printers_CanAdd)
                    return OperationResult.Forbid;
                data.LastSeenOnline = DateTime.Now;
				return new OperationResult(true, g["Printer Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/printers/update")]
		public OperationResult Update([FromBody]Printer data)
		{
			try
			{
                if (!userData.Admin_Printers_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Printer saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/printers/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Printers_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Printer Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/printers/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Printers_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Printer Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming printer."]);
			}
		}

		[HttpGet, Route("/printers/getbyid/{id}")]
		public IPrinter GetByID(int id)
		{
			try
			{
				return repo.GetByID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/printers/getlist")]
		public List<IPrinter> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/printers/getbylocationid/{locationid}")]
		public List<IPrinter> GetByLocationID(int locationid)
		{
			try
			{
				return repo.GetByLocationID(locationid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/printers/getbycompanyid/{companyid}")]
		public List<IPrinter> GetByCompanyID(int companyid)
		{
			try
			{
				return repo.GetByCompanyID(companyid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/printers/getdrivers")]
		public List<PrinterDriverInfo> GetPrinterDrivers()
		{
			try
			{
				return repo.GetPrinterDrivers();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/printers/getsettings/{printerid}/{articleid}")]
		public IPrinterSettings GetSettings(int printerid, int articleid)
		{
			try
			{
				return repo.GetSettings(printerid, articleid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpPost, Route("/printers/updatesettings")]
		public OperationResult UpdateSettings([FromBody]PrinterSettings settings)
		{
			try
			{
                if (!userData.CanSeeVMenu_Printers)
                    return OperationResult.Forbid;
                repo.UpdateSettings(settings);
				return new OperationResult(true, g["Printer settings updated!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpGet, Route("/printers/getstates")]
		public List<CompactPrinterState> GetPrinterStates()
		{
			try
			{
				var states = printerManager.GetPrinterStates();
				var result = new List<CompactPrinterState>(states.Count);
				foreach (var st in states)
					result.Add(new CompactPrinterState(st));
				return result;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpPost, Route("/printers/changelocation/{printerid}/{locationid}")]
		public OperationResult ChangeLocation(int printerid, int locationid)
		{
			try
			{
                if (!userData.Admin_Printers_CanChangeCompany)
                    return OperationResult.Forbid;
                repo.ChangeLocation(printerid, locationid);
				return new OperationResult(true, g["Printer moved!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/printers/sendcommand")]
		public async Task<OperationResult> SendCommand([FromBody]PrinterCommandDTO command)
		{
			try
			{
				if (!userData.Admin_Printers_CanSendCommand)
					return OperationResult.Forbid;
				_ = repo.GetByID(command.PrinterID);  // NOTE: Ensures user has access to this printer!!
				var printer = printerManager.GetPrinter(command.PrinterID);
				if(printer != null)
				{
					var cmd = new BaseCommand();
					cmd.SetMessage(command.Command);
					await printer.RawChannel.SendCommand(cmd);
					return new OperationResult(true, g["Command sent!"]);
				}
				else return new OperationResult(false, g["Printer is not online"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}
	}

	public class PrinterCommandDTO
	{
		public int PrinterID { get; set; }
		public string Command { get; set; }
	}
}