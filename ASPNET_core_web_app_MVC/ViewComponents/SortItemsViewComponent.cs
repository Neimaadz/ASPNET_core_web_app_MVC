using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ASPNET_core_web_app_MVC.Controllers;
using Microsoft.AspNetCore.Http;

namespace ASPNET_core_web_app_MVC.ViewComponents
{
    [ViewComponent(Name = "SortItemsViewComponent")]
    public class SortItemsViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<string> types = HomeController.ReadTypesJSON();
            ViewBag.Types = types;

            List<string> localisations = HomeController.ReadLocalisationsJSON();
            ViewBag.Localisations = localisations;

            // get arguments from session in purpose to post in view form
            ViewBag.Search = HttpContext.Session.GetString("search");
            ViewBag.FilterType = HttpContext.Session.GetString("type");
            ViewBag.FilterLocalisation = HttpContext.Session.GetString("localisation");
            ViewBag.Sort = HttpContext.Session.GetString("sort");
            ViewBag.SortDirection = HttpContext.Session.GetString("direction");

            return View("SortItemsViewComponent");
        }

    }
    
}
