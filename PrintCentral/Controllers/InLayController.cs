using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    /// <summary>
    /// Este controller no se va a utilizar aun, ya que no se guardan imágenes en el printpackage.
    /// </summary>
    public class InLayController : Controller
    {
        private IInLayRepository repo;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;

        public InLayController(IInLayRepository repo, IUserData userData, ILocalizationService g, ILogService log)
        {
            this.repo = repo;
            this.userData = userData;
            this.g = g;
            this.log = log;
        }
    }
}