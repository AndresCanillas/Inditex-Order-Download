using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using System;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class OrderLogService : IOrderLogService
	{
		private IOrderLogRepository orderLogRepo;
		private ILogService log;

		public OrderLogService(
			IOrderLogRepository orderLogRepo,
			ILogService log)
		{
			this.orderLogRepo = orderLogRepo;
			this.log = log;
		}


		public void Log(int orderID, string message, OrderLogLevel level, string comments = null)
		{
			IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, level, comments);
			try
			{
				orderLogRepo.Log(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public void Info(int orderID, string message, string comments = null) => Log(orderID, message, OrderLogLevel.INFO, comments);


		public void Warn(int orderID, string message, string comments = null) => Log(orderID, message, OrderLogLevel.WARN, comments);


		public void Error(int orderID, string message, string comments = null) => Log(orderID, message, OrderLogLevel.ERROR, comments);


		public void Debug(int orderID, string message, string comments = null) => Log(orderID, message, OrderLogLevel.DEBUG, comments);



		public async Task LogAsync(int orderID, string message, OrderLogLevel level, string comments = null)
		{
			try
			{
				IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, level, comments);
				await orderLogRepo.LogAsync(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public async Task InfoAsync(int orderID, string message, string comments = null)
		{
			try
			{
				IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, OrderLogLevel.INFO, comments);
				await orderLogRepo.LogAsync(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public async Task WarnAsync(int orderID, string message, string comments = null)
		{
			try
			{
				IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, OrderLogLevel.WARN, comments);
				await orderLogRepo.LogAsync(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public async Task ErrorAsync(int orderID, string message, string comments = null)
		{
			try
			{
				IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, OrderLogLevel.ERROR, comments);
				await orderLogRepo.LogAsync(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public async Task DebugAsync(int orderID, string message, string comments = null)
		{
			try
			{
				IOrderLog logEntry = orderLogRepo.InitLog(orderID, message, OrderLogLevel.DEBUG, comments);
				await orderLogRepo.LogAsync(logEntry);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}
	}
}
