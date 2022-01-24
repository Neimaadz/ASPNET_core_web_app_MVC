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
            List<string> listTypes = new List<string>();
            listTypes = HomeController.ReadTypesJSON();
            ViewBag.Types = listTypes;

            List<string> listLocalisations = new List<string>();
            listLocalisations = HomeController.ReadLocalisationsJSON();
            ViewBag.Localisations = listLocalisations;

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
