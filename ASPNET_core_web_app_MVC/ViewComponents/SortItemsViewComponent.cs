using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ASPNET_core_web_app_MVC.ViewComponents
{
    [ViewComponent(Name = "SortItemsViewComponent")]
    public class SortItemsViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<string> allTypes = new List<string>();
            allTypes = ReadTypesJSON();
            ViewBag.Types = allTypes;

            List<string> allCommunes = new List<string>();
            allCommunes = ReadCommunesJSON();
            ViewBag.Communes = allCommunes;

            return View("SortItemsViewComponent");
        }



        // =======================================================================
        // Read all items from JSON file
        // =======================================================================
        List<string> ReadTypesJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Types.json";
            List<string> myList = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var JSONQuery = from items in JSONFile["Types"]
                            orderby items["Name"] ascending
                            select items;

            foreach (var item in JSONQuery)
            {
                myList.Add(item.ToObject<Models.ModelDataType>().Name);
            }

            return myList;
        }

        // =======================================================================
        // Read all items from JSON file
        // =======================================================================
        List<string> ReadCommunesJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Communes.json";
            List<string> myList = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var JSONQuery = from items in JSONFile["Communes"]
                            orderby items["Name"] ascending
                            select items;

            foreach (var item in JSONQuery)
            {
                myList.Add(item.ToObject<Models.ModelDataCommune>().Name);
            }

            return myList;
        }

    }
    
}
