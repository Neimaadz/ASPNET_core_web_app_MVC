﻿using System;
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
            List<string> listTypes = new List<string>();
            listTypes = HomeController.ReadTypesJSON();
            ViewBag.Types = listTypes;

            List<string> listCommunes = new List<string>();
            listCommunes = HomeController.ReadCommunesJSON();
            ViewBag.Communes = listCommunes;

            return View("SortItemsViewComponent");
        }

    }
    
}
