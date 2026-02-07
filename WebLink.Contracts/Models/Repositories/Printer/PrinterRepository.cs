using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.LabelService;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
	public class PrinterRepository : GenericRepository<IPrinter, Printer>, IPrinterRepository
    {
		private ILocationRepository locationRepo;
		private IBLabelServiceClient labelService;
		private ILogService log;

		public PrinterRepository(
			IFactory factory,
			ILocationRepository locations,
			IBLabelServiceClient labelService,
			ILogService log,
			IAppConfig config
			)
		: base(factory, (ctx)=>ctx.Printers)
		{
			this.locationRepo = locations;
			this.labelService = labelService;
			this.labelService.Url = config["WebLink:LabelService"];
			this.log = log;
        }


		protected override string TableName { get => "Printers"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Printer entity, IPrinter data)
		{
			var location = locationRepo.GetByID(ctx, data.LocationID); // Note: this ensures user HAS access to the assigned location
			entity.Name = data.Name;
			entity.LocationID = location.ID;
            entity.PrinterType = data.PrinterType;
			if(userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
			{
				entity.DeviceID = data.DeviceID.ToUpper();
				entity.DriverName = data.DriverName;
			}
		}


		public IPrinter GetByDeviceID(string deviceid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByDeviceID(ctx, deviceid);
			}
		}


		public IPrinter GetByDeviceID(PrintDB ctx, string deviceid)
		{
			var printer = ctx.Printers.Where(p => p.DeviceID == deviceid).AsNoTracking().FirstOrDefault();
			if (printer == null)
				throw new Exception($"Could not find printer with DeviceID {deviceid}");
			return printer;
		}


		public List<IPrinter> GetByLocationID(int locationid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByLocationID(ctx, locationid);
			}
		}


		public List<IPrinter> GetByLocationID(PrintDB ctx, int locationid)
		{
			return new List<IPrinter>(
				All(ctx)
				.Where(p => p.LocationID == locationid)
			);
		}


		public List<IPrinter> GetByCompanyID(int companyid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCompanyID(ctx, companyid);
			}
		}


		public List<IPrinter> GetByCompanyID(PrintDB ctx, int companyid)
		{
			var userData = factory.GetInstance<IUserData>();
			if (!userData.IsIDT)  // Prevents non-IDT users from querying other companies records...
				companyid = userData.CompanyID;

			return new List<IPrinter>(
				from p in ctx.Printers
				join l in ctx.Locations on p.LocationID equals l.ID
				where l.CompanyID == companyid
				select p);
		}


		public IPrinterSettings GetSettings(int printerid, int articleid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetSettings(ctx, printerid, articleid);
			}
		}


		public IPrinterSettings GetSettings(PrintDB ctx, int printerid, int articleid)
		{
			var userData = factory.GetInstance<IUserData>();
			var printer = ctx.Printers.Where(p => p.ID == printerid).AsNoTracking().FirstOrDefault();
			if (printer == null)
				throw new Exception($"Could not find printer with ID {printerid}");
			var article = ctx.Articles.Where(p => p.ID == articleid).AsNoTracking().FirstOrDefault();
			if (article == null)
				throw new Exception($"Could not find article with ID {articleid}");
			AuthorizeOperation(ctx, userData, printer);
			var settings = ctx.PrinterSettings.Where(p => p.PrinterID == printerid && p.ArticleID == articleid).FirstOrDefault();
			if (settings == null)
			{
				settings = new PrinterSettings();
				settings.PrinterID = printerid;
				settings.ArticleID = articleid;
				settings.XOffset = 0;
				settings.YOffset = 0;
				settings.Speed = "3";
				settings.Darkness = "10";
				ctx.PrinterSettings.Add(settings);
				ctx.SaveChanges();
			}
			return settings;
		}


		public void UpdateSettings(IPrinterSettings settings)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				UpdateSettings(ctx, settings);
			}
		}


		public void UpdateSettings(PrintDB ctx, IPrinterSettings settings)
		{
			var userData = factory.GetInstance<IUserData>();
			var printer = ctx.Printers.Where(p => p.ID == settings.PrinterID).AsNoTracking().FirstOrDefault();
			if (printer == null)
				throw new Exception($"Could not find printer with ID {settings.PrinterID}");
			AuthorizeOperation(ctx, userData, printer);
			var record = ctx.PrinterSettings.Where(p => p.ID == settings.ID).FirstOrDefault();
			if (record == null)
				throw new Exception($"Could not find settings for this printer/article combination: PrinterID {settings.PrinterID}, ArticleID: {settings.ArticleID}");
			Reflex.Copy(record, settings);
			ctx.PrinterSettings.Update(record);
			ctx.SaveChanges();
		}


		public void UpdateLastSeen(string deviceid, string productName, string firmware)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				UpdateLastSeen(ctx, deviceid, productName, firmware);
			}
		}


		public void UpdateLastSeen(PrintDB ctx, string deviceid, string productName, string firmware)
		{
			var printer = ctx.Printers.Where(p => p.DeviceID == deviceid).FirstOrDefault();
			if (printer != null)
			{
				printer.ProductName = productName;
				printer.FirmwareVersion = firmware;
				printer.LastSeenOnline = DateTime.Now;
				ctx.SaveChanges();
			}
			else
			{
				log.LogWarning("Received a connection from an unregistered printer, no labels will be output to this device until the system is properly configured for this device.\r\nPrinter Data:\r\n\tSerial: {0}\r\n\tName:{1}\r\n\tFirmware{2}\r\n",
					deviceid, productName, firmware);
			}
		}


		public void ChangeLocation(int printerid, int locationid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				ChangeLocation(ctx, printerid, locationid);
			}
		}


		public void ChangeLocation(PrintDB ctx, int printerid, int locationid)
		{
			// Note: calling GetByID ensures user has access to both the printer and the intended location!
			var printer = GetByID(ctx, printerid);
			var location = locationRepo.GetByID(ctx, locationid);
			printer.LocationID = locationid;
			Update(ctx, printer);

			// Delete all jobs assigned to this printer
			var jobs = ctx.PrinterJobs.Where(p => p.AssignedPrinter == printerid).ToList();
			foreach (var j in jobs)
			{
				j.AssignedPrinter = null;
			}

			ctx.SaveChanges();
		}


		public List<PrinterDriverInfo> GetPrinterDrivers()
		{
			try
			{
				var response = labelService.GetPrinterDriversAsync().Result;
				if (response.Success)
					return response.Drivers;
				else
					log.LogWarning("Label service did not return a valid response for method GetPrinterDrivers: {0}", response.ErrorMessage);
				return null;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}
	}
}
