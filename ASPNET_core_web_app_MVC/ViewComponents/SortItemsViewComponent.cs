using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ASPNET_core_web_app_MVC.Controllers;

namespace ASPNET_core_web_app_MVC.ViewComponents
{
    [ViewComponent(Name = "SortItemsViewComponent")]
    public class SortItemsViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<string> allTypes = new List<string>();
            allTypes = HomeController.ReadTypesJSON();
            ViewBag.Types = allTypes;

            List<string> allCommunes = new List<string>();
            allCommunes = HomeController.ReadCommunesJSON();
            ViewBag.Communes = allCommunes;

            return View("SortItemsViewComponent");
        }

    }
    
}
