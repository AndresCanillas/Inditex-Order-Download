using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Automated
{
    // TODO: no registered process on AutomatedProcessManager
    public class CheckSageOrderGroupsStateProcess : IAutomatedProcess
    {
        IFactory factory;
        IAppConfig appConfig;
        ILogSection appLog;
        private int delta;

        public CheckSageOrderGroupsStateProcess(IFactory factory, IAppConfig cfg, ILogService appLog)
        {
            this.factory = factory;
            this.appConfig = cfg;
            this.appLog = appLog.GetSection("CheckSageOrders");

            delta = appConfig.GetValue<int>("WebLink.Sage.Processes.CheckOrders.FrequencyInSeconds");

            if (delta < 1 || !IsActive)
            {
                appLog.LogWarning($"{this.GetType().Name} cannot execute because Sage Configuration 'WebLink.Sage.IsActive' is false or does not exist");
                delta = 3600 * 2;
            }
        }

        public void OnExecute()
        {
            var orders = GetOrdersToTrackingAsync().GetAwaiter().GetResult();

            var externalInfo = ConsultingExternalOrdersAsync(orders).GetAwaiter().GetResult();

            UpdateOrderStatusAsync(externalInfo).GetAwaiter().GetResult();

        }

        private async Task UpdateOrderStatusAsync(List<GroupRelated> orders)
        {
            var tasks = new List<Task<IOrderGroup>>();

            foreach (var o in orders)
            {
                tasks.Add(Task.Run(() =>
                {

                    var repo = factory.GetInstance<IOrderGroupRepository>();

                    IOrderGroup data = repo.GetByID(o.Local.OrderGroupID);

                    if (o.Imported != null)
                    {

                        data.InvoiceStatus = string.IsNullOrEmpty(o.Imported.Invoice) ? SageInvoiceStatus.Unknow : (SageInvoiceStatus)Enum.Parse(typeof(SageInvoiceStatus), o.Imported.Invoice);
                        data.DeliveryStatus = string.IsNullOrEmpty(o.Imported.Delivered) ? SageDeliveryStatus.Unknow : (SageDeliveryStatus)Enum.Parse(typeof(SageDeliveryStatus), o.Imported.Delivered);
                        data.SageStatus = string.IsNullOrEmpty(o.Imported.OrderStatus) ? SageOrderStatus.Unknow : (SageOrderStatus)Enum.Parse(typeof(SageOrderStatus), o.Imported.OrderStatus);
                        data.CreditStatus = string.IsNullOrEmpty(o.Imported.Credit) ? SageCreditStatus.Unknow : (SageCreditStatus)Enum.Parse(typeof(SageCreditStatus), o.Imported.Credit);
                    }
                    else
                    {
                        data.InvoiceStatus = SageInvoiceStatus.Unknow;
                        data.DeliveryStatus = SageDeliveryStatus.Unknow;
                        data.SageStatus = SageOrderStatus.NotFound;
                        data.CreditStatus = SageCreditStatus.Unknow;
                    }


                    if (data.SageStatus == SageOrderStatus.Closed)
                    {
                        data.IsCompleted = true;
                        // TODO: log order mark as complete for the sysmte - orther is closed in ERP
                    }

                    return repo.Update(data);

                }));

            }

            //await Task.WaitAll(tasks.ToArray());
            await Task.WhenAll(tasks);
        }

        public async Task<IEnumerable<OrderGroupDetailDTO>> GetOrdersToTrackingAsync()
        {
            var orders = await Task.Run(() => {
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var groupRepo = factory.GetInstance<IOrderGroupRepository>();
                    return groupRepo.GetRegisteredInSage(ctx);
                }
            });

            return orders;
        }

        private async Task<List<GroupRelated>> ConsultingExternalOrdersAsync(IEnumerable<OrderGroupDetailDTO> orders)
        {

            var sageClient = factory.GetInstance<ISageClientService>();
            var pendingTask = new List<Task<ISageOrder>>();
            var sohList = new List<ISageOrder>();
            List<GroupRelated> ret = new List<GroupRelated>();
            //var maxRequest = 2; // TODO: add to config
            //ISageOrder[] work;

            var exceptions = new List<Exception>();

            // TODO: try to execute in parallel, now process one by one, very slowly
            // https://medium.com/@nirinchev/executing-a-collection-of-tasks-in-parallel-with-control-over-the-degree-of-parallelism-in-c-508d59ddffc6
            foreach (var ord in orders)
            {
                try
                {
                    ISageOrder task = await sageClient.GetOrderDetailAsync(ord.SageReference);
                    sohList.Add(task);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            foreach (var ex in exceptions)
            {
                appLog.LogException(ex);
            }

            foreach (var local in orders)
            {
                var mapped = new GroupRelated()
                {
                    Local = local,
                    Imported = sohList.FirstOrDefault(f => f.Reference.Equals(local.SageReference))
                };

                ret.Add(mapped);
            }

            return ret;
        }

        public TimeSpan GetIdleTime()
        {
            return TimeSpan.FromSeconds(delta);
        }

        private bool IsActive
        {
            get
            {
                var sageClientActive = !string.IsNullOrEmpty(appConfig["WebLink.Sage.IsActive"]) && appConfig.GetValue<bool>("WebLink.Sage.IsActive");
                var processActive = !string.IsNullOrEmpty(appConfig["WebLink.Sage.Processes.CheckOrders.IsActive"]) && appConfig.GetValue<bool>("WebLink.Sage.Processes.CheckOrders.IsActive");

                return sageClientActive && processActive;
            }
        }

        public void OnLoad()
        {
            throw new NotImplementedException();
        }

        public void OnUnload()
        {
            throw new NotImplementedException();
        }
    }

    internal class GroupRelated
    {
        public OrderGroupDetailDTO Local { get; set; }

        public ISageOrder Imported { get; set; }
    }
}
