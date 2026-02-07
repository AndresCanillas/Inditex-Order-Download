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
    public class CheckSageOrderStateProcess : IAutomatedProcess
    {
        IFactory factory;
        IAppConfig appConfig;
        ILogSection appLog;
        private int delta;

        public CheckSageOrderStateProcess(IFactory factory, IAppConfig cfg, ILogService appLog)
        {
            this.factory = factory;
            this.appConfig = cfg;
            this.appLog = appLog.GetSection("CheckSageOrders");

            delta = appConfig.GetValue<int>("WebLink.Sage.Processes.CheckOrders.FrequencyInSeconds");

            if (delta < 1)
            {
                appLog.LogWarning($"{this.GetType().Name} Frequency was to short, will be assigned default value 1 hour");
                delta = 3600;
            }
        }

        public TimeSpan GetIdleTime()
        {
            return TimeSpan.FromSeconds(delta);
        }

        public void OnExecute()
        {
            if(!IsActive)
                return;

            var orders = GetOrdersToTrackingAsync().GetAwaiter().GetResult();

            var externalInfo = ConsultingExternalOrdersAsync(orders).GetAwaiter().GetResult();

            UpdateOrderStatusAsync(externalInfo).GetAwaiter().GetResult();

        }

        public void OnLoad()
        {
            //throw new NotImplementedException();
        }

        public void OnUnload()
        {
            //throw new NotImplementedException();
        }

        public async Task<IEnumerable<OrderDetailDTO>> GetOrdersToTrackingAsync ()
        {
            var orders = await Task.Run( () => {
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var orderRepo = factory.GetInstance<IOrderRepository>();
                    return orderRepo.GetRegisteredInSage(ctx);
                }
            });

            return orders;
        }

        private async Task<List<OrdersRelated>> ConsultingExternalOrdersAsync(IEnumerable<OrderDetailDTO> orders)
        {

            var sageClient = factory.GetInstance<ISageClientService>();
            var pendingTask = new List<Task<ISageOrder>>();
            var sohList = new List<ISageOrder>();
            List<OrdersRelated> ret = new List<OrdersRelated>();
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
                }catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                //pendingTask.Add(task);

                //await task
                //    .ContinueWith(t => { 
                //        pendingTask.Remove(t);
                //        sohList.Add(t.Result);
                //    }, TaskContinuationOptions.OnlyOnRanToCompletion)
                //    .ContinueWith(t => { exceptions.Add(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);


                //if (pendingTask.Count >= maxRequest)
                //{
                //    work = await Task.WhenAll(pendingTask);
                //    sohList.AddRange(work);
                //}

            }

            //try
            //{
            //    if (pendingTask.Count > 0)
            //    {
            //        work = await Task.WhenAll(pendingTask);
            //        sohList.AddRange(work);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    exceptions.Add(ex);
            //}


            foreach(var ex in exceptions)
            {
                appLog.LogException(ex);
            }

            foreach (var local in orders)
            {
                var mapped = new OrdersRelated()
                {
                    Local = local,
                    Imported = sohList.FirstOrDefault(f => f.Reference.Equals(local.SageReference))
                };

                ret.Add(mapped);
            }

            return ret;
        }

        private async Task UpdateOrderStatusAsync(List<OrdersRelated> orders)
        {
            var tasks = new List<Task<IOrder>>();

            foreach(var o in orders)
            {
                    tasks.Add(Task.Run(() =>
                    {

                        var repo = factory.GetInstance<IOrderRepository>();
                        var groupRepo = factory.GetInstance<IOrderGroupRepository>();

                        IOrder data = repo.GetByID(o.Local.OrderID);
                        IOrderGroup group = groupRepo.GetByID(o.Local.OrderGroupID);

                        if (o.Imported != null)
                        {

                            data.InvoiceStatus = string.IsNullOrEmpty(o.Imported.Invoice) ? SageInvoiceStatus.Unknow : (SageInvoiceStatus)Enum.Parse(typeof(SageInvoiceStatus), o.Imported.Invoice);
                            data.DeliveryStatus = string.IsNullOrEmpty(o.Imported.Delivered) ? SageDeliveryStatus.Unknow : (SageDeliveryStatus)Enum.Parse(typeof(SageDeliveryStatus), o.Imported.Delivered);
                            data.SageStatus = string.IsNullOrEmpty(o.Imported.OrderStatus) ? SageOrderStatus.Unknow : (SageOrderStatus)Enum.Parse(typeof(SageOrderStatus), o.Imported.OrderStatus);
                            data.CreditStatus = string.IsNullOrEmpty(o.Imported.Credit) ? SageCreditStatus.Unknow : (SageCreditStatus)Enum.Parse(typeof(SageCreditStatus), o.Imported.Credit);

                            group.InvoiceStatus = data.InvoiceStatus;
                            group.DeliveryStatus = data.DeliveryStatus;
                            group.SageStatus = data.SageStatus;
                            group.CreditStatus = data.CreditStatus;
                        }else
                        {
                            data.InvoiceStatus = SageInvoiceStatus.Unknow;
                            data.DeliveryStatus = SageDeliveryStatus.Unknow;
                            data.SageStatus = SageOrderStatus.NotFound;
                            data.CreditStatus = SageCreditStatus.Unknow;

                            group.InvoiceStatus = data.InvoiceStatus;
                            group.DeliveryStatus = data.DeliveryStatus;
                            group.SageStatus = data.SageStatus;
                            group.CreditStatus = data.CreditStatus;
                        }

                        groupRepo.Update(group);

                        return repo.Update(data);

                    }));
                
            }

            //await Task.WaitAll(tasks.ToArray());
            await Task.WhenAll(tasks);
        }

        private bool IsActive
        {
             get {
                var sageClientActive = appConfig.GetValue<bool>("WebLink.Sage.IsActive", false);
                var processActive = appConfig.GetValue<bool>("WebLink.Sage.Processes.CheckOrders.IsActive", false);

                return sageClientActive && processActive;
            }
        }
    }


    internal class OrdersRelated
    {
        public OrderDetailDTO Local { get; set; }

        public ISageOrder Imported { get; set; }
    }
}
