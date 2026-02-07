using Service.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
   
    public interface ICompositionAuditService
    {
        Task<OperationResult> Save(CompositionAudit audit);
        Task<OperationResult> IsValidUser(int orderId, string currentUser);
        Task<OperationResult> GetOrderStatus(int orderid);
    }

    public class CompositionAuditService : ICompositionAuditService
    {
        private IFactory factory;
        private ILocalizationService g;
        private IEventQueue events;
        public CompositionAuditService(IFactory factory, ILocalizationService g, IEventQueue events)
        {
            this.factory = factory;
            this.g = g;
            this.events = events;
        }

        public async Task<OperationResult> GetOrderStatus(int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var validationuser = ctx.CompanyOrders.FirstOrDefault(c => c.ID == orderid).ValidationUser;
                var lastAuditCompositionOrder = ctx.CompositionAudits.OrderByDescending(x=>x.CreatedDate).FirstOrDefault(o=>o.OrderID == orderid);
                if(validationuser == null) 
                {
                    return new OperationResult(false, message: g["Invalid Validation User. Order ID not found "]);
                }
                if(lastAuditCompositionOrder == null) {
                    return new OperationResult(true, message: g[""], data: new { EnabledAudit = true, OpenForm= true }); 
                }

                if(lastAuditCompositionOrder.Status == AuditStatus.Error)
                {
                    var contactMessage = g[$"The audit was failed. Please contact {validationuser} to restart the processs / La auditoria ha fallado. Por favor contacta con {validationuser} para reiniciar el proceso"]; 
                    List<string> GlobalizeErrors = new List<string>();
                    var errors = lastAuditCompositionOrder.AuditMessages.Split("|");
                    foreach(var error in errors)
                    {
                        GlobalizeErrors.Add(g[error]);
                    }
                    return new OperationResult(true, message: "", data: new { EnabledAudit = false,OpenForm=true, ContactMessage = contactMessage, AuditDate= lastAuditCompositionOrder.AuditDate, Errors = GlobalizeErrors });
                }
                if(lastAuditCompositionOrder.Status == AuditStatus.Pending)
                {
                    return new OperationResult(true, message: g["The order audit is running. Please wait"], data: new { EnabledAudit = false, OpenForm = false });
                }

                return new OperationResult(true, message: g["The audit was a success. The status of the order will change immediately"], data: new { EnabledAudit = false, OpenForm= false });
            }
        }

        public async Task<OperationResult> IsValidUser(int orderID, string currentUser )
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var companyOrder = ctx.CompanyOrders.FirstOrDefault(o => o.ID == orderID);
                if(companyOrder == null)
                {
                    return new OperationResult(false, message: g["Invalid Order. Order ID not found "]);
                }

                if(companyOrder.OrderStatus != OrderStatus.CompoAuditNeeded)
                {
                    return new OperationResult(false, message: g["Invalid Order Status. Order has to be in Composition Audit Needed to continue audit. "]);
                }
                var validationuser = ctx.CompanyOrders.FirstOrDefault(c => c.ID == orderID).ValidationUser;
                if(validationuser == currentUser)
                {
                    return new OperationResult(false, message: g["Invalid User. The auditor user must be different from the validator user. "]);
                }

                return new OperationResult(true, message :"" );
            }
        }

        public async Task<OperationResult> Save(CompositionAudit audit)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                
                var companyOrder = ctx.CompanyOrders.FirstOrDefault(o => o.ID == audit.OrderID);
                if(companyOrder == null)
                {
                    return new OperationResult(false, message: g["Invalid Order. Order ID not found "]);
                }
                if (companyOrder.OrderStatus != OrderStatus.CompoAuditNeeded)
                {
                    return new OperationResult(false, message: g["Invalid Order Status. Order has to be in Composition Audit Needed to continue audit. "]);
                }
                var brandid = ctx.Projects.FirstOrDefault(p=>p.ID ==companyOrder.ProjectID).BrandID; 
                var validationuser = ctx.CompanyOrders.FirstOrDefault(c => c.ID == audit.OrderID).ValidationUser;
                //if(validationuser == audit.CreatedBy)
                //{
                //    return new OperationResult(false, message: g["Invalid User. The auditor user must be different from the validator user. "]);
                //}
                var insertedAudit =  ctx.CompositionAudits.Add(audit);
                await ctx.SaveChangesAsync();
                var auditID = audit.ID; 

                events.Send(new OrderAuditEvent(companyOrder.OrderGroupID,
                                                 companyOrder.ID,
                                                 companyOrder.OrderNumber,
                                                 companyOrder.CompanyID,
                                                 brandid,
                                                 companyOrder.ProjectID,
                                                 auditID)); 
            }

            return new OperationResult(true, message: g["Composition audit saved"]); 
        }
    }
}
