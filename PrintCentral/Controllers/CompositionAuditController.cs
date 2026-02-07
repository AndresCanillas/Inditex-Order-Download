using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;


namespace PrintCentral.Controllers
{
    public class CompositionAuditController : Controller
    {
        private static readonly HttpClient client = new HttpClient();

        private ILocalizationService g;
        private ILogService log;

        private IOrderRepository orderRepository;
        private readonly PrintDB printDB;
        private IFactory factory;
        private ICompositionAuditService compositionAuditService;



        public CompositionAuditController(IOrderPoolRepository repo,
                                          ILocalizationService g,
                                          ILogService log,
                                          IOrderRepository orderRepository,
                                          IFactory factory
,
                                          ICompositionAuditService compositionAuditService)
        {
            this.g = g;
            this.log = log;
            this.orderRepository = orderRepository;
            this.factory = factory;
            printDB = factory.GetInstance<PrintDB>();
            this.compositionAuditService = compositionAuditService;
        }
        [HttpPost, Route("/compositionaudit/isvaliduser/{orderid}")] 
        public async Task<OperationResult> IsValidUser (int orderid)
        {
            try
            {
                return await compositionAuditService.IsValidUser(orderid, User.Identity.Name);
            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
            
        }

        [HttpPost, Route("/compositionaudit/getorderstatus/{orderid}")]
        public async Task<OperationResult> GetOrderStatus (int orderid)
        {
            try
            {
                return await compositionAuditService.GetOrderStatus(orderid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]); 

            }
        }

        [HttpPost, Route("/compositionaudit/save")]
        public async Task<OperationResult> Save([FromBody] CompositionAuditDTO auditCompo)
        {
            try
            {

                return await compositionAuditService.Save(new CompositionAudit()
                {
                    OrderID = auditCompo.OrderID, 
                    AuditCompo = auditCompo.AuditCompo,
                    CreatedBy = User.Identity.Name,
                    CreatedDate = DateTime.Now,
                    Status = AuditStatus.Pending
                }); 
                

            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }



    }
    public class CompositionAuditDTO
    {
        public int OrderID { get; set; } 
        public string AuditCompo { get; set; }
    }
}
