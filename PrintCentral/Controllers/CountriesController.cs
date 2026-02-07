using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace PrintCentral.Controllers
{
    public class CountriesController : Controller
    {

		private ICountryRepository repo;
		private IUserData userData;
		private ILocalizationService g;
		private ILogService log;

		public CountriesController(
            ICountryRepository repo,
			IUserData userData,
			ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
			this.userData = userData;
			this.g = g;
			this.log = log;
		}

		public IActionResult Index()
        {
            return View();
        }


		[HttpGet, Route("/countries/getlist")]
		public IEnumerable<ICountry> GetList()
		{
             var data = repo.GetList();
            return data;
		}      
	}  
}