using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class BillingsController : Controller
    {
        private IBillingRepository repo;
        private ILocalizationService g;
        private ILogService log;

        public BillingsController(
            IBillingRepository repo,
            ILocalizationService g,
            ILogService log)
        {
            this.repo = repo;
            this.g = g;
            this.log = log;
        }


        [HttpGet, Route("/billings/getbyprovider/{id}")]
        public List<IBillingInfo> GetByProviderID(int id)
        {
            try
            {
                return repo.GetByProviderID(id);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }
    }
}