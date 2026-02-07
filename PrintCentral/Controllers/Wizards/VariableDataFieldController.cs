using Microsoft.AspNetCore.Mvc;
using Services.Core;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    public class VariableDataFieldController : Controller
    {
        private ILogService log;
        private ICatalogRepository catalogRepo;


        public VariableDataFieldController(
            ILogService log,
            ICatalogRepository catalogRepo)
        {
            this.log = log;
            this.catalogRepo = catalogRepo;
        }

        public void GetCatalogData([FromBody]RqCatalogField rq)
        {

        }
    }

    public class RqCatalogField
    {
        public int ProjectID { get; set; }
        public string CatalogName { get; set; }
        public string KeyField { get; set; }
        public string DisplayField { get; set; }
    }
}