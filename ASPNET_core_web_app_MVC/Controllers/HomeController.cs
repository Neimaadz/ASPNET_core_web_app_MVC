using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Xml.Linq;
using ASPNET_core_web_app_MVC.Authentication;
using ASPNET_core_web_app_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASPNET_core_web_app_MVC.Controllers
{
    [Authorize]
    [Route("")]
    [ApiController]
    public class HomeController : Controller
    {
        // private readonly ILogger<HomeController> _logger;
        private readonly IJwtAuth jwtAuth;
        private string UriUserXML = $@"{Directory.GetCurrentDirectory()}/Data/Users.xml";
        private string UriItemsJSON = $@"{Directory.GetCurrentDirectory()}/Data/Items.json";

        // public HomeController(ILogger<HomeController> logger)
        public HomeController(IJwtAuth jwtAuth)
        {
            // _logger = logger;
            this.jwtAuth = jwtAuth;
        }

        [AllowAnonymous]
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }


        // =======================================================================
        // All items
        // =======================================================================
        [HttpGet("items")]
        [HttpGet("items/{filter=null}+{sort=null}+{direction=null}")]
        public IActionResult Items(string filter, string sort, string direction)
        {
            List<string> allTypes = new List<string>();
            allTypes = ReadTypesJSON();
            ViewBag.Types = allTypes;

            List<string> allCommunes = new List<string>();
            allCommunes = ReadCommunesJSON();
            ViewBag.Communes = allCommunes;

            List<Item> allItems = new List<Item>();
            allItems = ReadUserItemsJSON(filter, sort, direction); // read only user's items

            return View(allItems);
        }

        public IActionResult SortItemsLayout()
        {
            List<string> allTypes = new List<string>();
            allTypes = ReadTypesJSON();
            ViewBag.Types = allTypes;

            List<string> allCommunes = new List<string>();
            allCommunes = ReadCommunesJSON();
            ViewBag.Communes = allCommunes;

            return View();
        }

        // =======================================================================
        // Add items
        // =======================================================================
        [HttpGet("additems")]
        public IActionResult AddItems()
        {
            List<string> allTypes = new List<string>();
            allTypes = ReadTypesJSON();
            ViewBag.Types = allTypes;

            List<string> allCommunes = new List<string>();
            allCommunes = ReadCommunesJSON();
            ViewBag.Communes = allCommunes;

            return View();
        }

        [HttpPost("additems")]  // define route : https://localhost:<port>/items
        public IActionResult AddItems([FromForm] ItemCredential itemCredential)
        {
            List<Item> ListItems = ReadItemsJSON(); // to find the highest id in ALL items list
            int itemId = ListItems.Max(item => item.ItemId);    // find the highest id
            string name = itemCredential.Name;
            string date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string type = itemCredential.Type;
            string localistion = itemCredential.Localisation;
            string description = itemCredential.Description;

            Item item = new Item()
            {
                ItemId = itemId + 1,
                UserId = Int32.Parse(User.FindFirstValue("id")),
                Name = name,
                Date = date,
                Type = type,
                Localisation = localistion,
                Description = description
            };

            WriteItemJSON(item);

            return RedirectToAction("items", new { filter="", sort= "Date", direction="ASC"});
        }


        // =======================================================================
        // Edit item
        // =======================================================================
        [HttpPost("editItems/{itemId}")]
        public IActionResult EditItems(int itemId, [FromForm] Item item)
        {
            EditItemByItemId(itemId, item);
            return RedirectToAction("items", new { filter = "", sort = "Date", direction = "ASC" });
        }


        // =======================================================================
        // Delete item
        // =======================================================================
        [HttpPost("deleteItems")]
        public IActionResult DeleteItems(int itemId)
        {
            DeleteItemByItemId(itemId);
            return RedirectToAction("items", new { filter = "", sort = "Date", direction = "ASC" });
        }


        // =======================================================================
        // All users
        // =======================================================================
        [HttpGet("users")]
        public IActionResult Users()
        {
            List<User> allUsers = new List<User>();
            allUsers = ReadUsersXML();

            return View(allUsers);
        }


        // =======================================================================
        // Signin
        // =======================================================================
        [AllowAnonymous]
        [HttpGet("signin")]
        public IActionResult Signin()
        {
            bool isAuthenticated = (HttpContext.User != null) && HttpContext.User.Identity.IsAuthenticated;

            if (isAuthenticated)
            {
                return RedirectToAction("index");
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        public IActionResult Signin([FromForm] UserCredential userCredential)
        {
            string username = userCredential.Username;
            string password = userCredential.Password;

            User user = new User()
            {
                Username = username,
                Password = password
            };

            if (username.Equals(null) || password.Equals(null))
            {
                ErrorViewModel error = new ErrorViewModel();
                error.StatusCode = Unauthorized().StatusCode;
                error.Message = "Invalid login or password !";

                return View("Error", error);
                //return RedirectToAction("signin", Unauthorized());
            }
            else
            {
                List<User> allUsers = new List<User>();
                allUsers = ReadUsersXML();

                if (!IsUserAlreadyExist(allUsers, user))
                {
                    ViewBag.user = user;
                    WriteUserXML(user);

                    return RedirectToAction("login");
                }
                else
                {
                    ErrorViewModel error = new ErrorViewModel();
                    error.StatusCode = Unauthorized().StatusCode;
                    error.Message = "Login already exist !";

                    return View("Error", error);
                    //return RedirectToAction("signin", Unauthorized());
                }

            }
        }


        // =======================================================================
        // Login
        // =======================================================================
        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login()
        {
            bool isAuthenticated = (HttpContext.User != null) && HttpContext.User.Identity.IsAuthenticated;

            if (isAuthenticated)
            {
                return RedirectToAction("index");
            }

            return View();
        }

        [AllowAnonymous]
        // POST <HomeController>
        [HttpPost("authentication")]
        public IActionResult Authentication([FromForm] UserCredential userCredential)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = jwtAuth.Authentication(userCredential.Username, userCredential.Password);

            if (token == null)
            {
                ErrorViewModel error = new ErrorViewModel();
                error.StatusCode = Unauthorized().StatusCode;
                error.Message = "Invalid login or password !";

                return View("Error", error);
                //return RedirectToAction("login", Unauthorized() );
            }
            HttpContext.Response.Cookies.Append("Authorization", $"{tokenHandler.WriteToken(token)}",
                new CookieOptions { Expires = token.ValidTo });

            return RedirectToAction("index");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Authorization");

            return RedirectToAction("login");
        }



        /*
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */



        // =======================================================================
        // Items JSON File : read all items from JSON file
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
        // Items JSON File : read all items from JSON file
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


        // =======================================================================
        // Items find : find item by item id
        // =======================================================================
        Item FindItemByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> ListItems = ReadItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            ListItems.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Items edit : find item by item id
        // =======================================================================
        void EditItemByItemId(int itemId, Item item)
        {
            List<Item> Items = ReadItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            Items.ForEach(x => {
                if (x.ItemId.Equals(itemId))
                {
                    x.Name = item.Name;
                    x.Type = item.Type;
                    x.Localisation = item.Localisation;
                    x.Description = item.Description;
                }
            });
            var allItems = new { Items };   // Permet d'ajouter la propriété "Items" dans JSON

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);
        }


        // =======================================================================
        // Items delete : delete to JSON file
        // =======================================================================
        void DeleteItemByItemId(int itemId)
        {
            int index = 0;
            List<Item> Items = ReadItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            Items.ForEach(x => { if (x.ItemId.Equals(itemId)) index=Items.IndexOf(x); });
            Items.RemoveAt(index);
            var allItems = new { Items };   // Permet d'ajouter la propriété "Items" dans JSON

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);

        }


        // =======================================================================
        // Items JSON File : write to JSON file
        // =======================================================================
        void WriteItemJSON(Item item)
        {
            List<Item> Items = ReadItemsJSON(); // to add item into ALL items list

            Items.Add(item);
            var allItems = new { Items };   // Permet d'ajouter la propriété "Items" dans JSON

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);

        }

        // =======================================================================
        // Items JSON File : read ONLY user's items from JSON file
        // =======================================================================
        List<Item> ReadUserItemsJSON(string filter, string sort, string direction)
        {
            List<Item> allItems = new List<Item>();

            // Définir ma source de données
            var listItems = ReadItemsJSON();
            var currentId = User.FindFirstValue("id");  // get the user id from token

            var propertyInfo = typeof(Item);

            if (direction == "ASC")
            {
                var itemsByUser = listItems.Where(items => items.UserId == Int32.Parse(currentId))
                    .OrderBy(x => propertyInfo.GetProperty(sort).GetValue(x, null));    // get property of Sort

                // Appel de la requête
                foreach (var items in itemsByUser)
                {
                    allItems.Add(items);
                }
            }
            else if (direction == "DSC")
            {
                var itemsByUser = listItems.Where(items => items.UserId == Int32.Parse(currentId))
                    .OrderByDescending(x => propertyInfo.GetProperty(sort).GetValue(x, null));

                // Appel de la requête
                foreach (var items in itemsByUser)
                {
                    allItems.Add(items);
                }
            }
            else if (filter != null)
            {
                var itemsByUser = listItems.Where(items => items.UserId == Int32.Parse(currentId)
                    && (items.Type == filter || items.Localisation == filter));

                // Appel de la requête
                foreach (var items in itemsByUser)
                {
                    allItems.Add(items);
                }
            }
            // Default
            else
            {
                var itemsByUser = listItems.Where(items => items.UserId == Int32.Parse(currentId))
                    .OrderBy(x => propertyInfo.GetProperty("Date").GetValue(x, null));

                // Appel de la requête
                foreach (var items in itemsByUser)
                {
                    allItems.Add(items);
                }
            }

            return allItems;
        }


        // =======================================================================
        // Items JSON File : read all items from JSON file
        // =======================================================================
        List<Item> ReadItemsJSON()
        {
            List<Item> allItems = new List<Item>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriItemsJSON));
            var JSONItems = from items in JSONFile["Items"]
                            select items;

            foreach (var item in JSONItems)
            {
                allItems.Add(item.ToObject<Item>());
            }

            return allItems;
        }


        // =======================================================================
        // Users XML File : read all users from XML file
        // =======================================================================
        List<User> ReadUsersXML()
        {
            List<User> allUsers = new List<User>();

            var XMLFile = XElement.Load(UriUserXML);
            var XMLUsers = from element in XMLFile.Descendants("User")
                           select new User()
                           {
                               Id = Convert.ToInt32(element.Element("Id").Value),
                               Username = element.Element("Username").Value,
                               Password = element.Element("Password").Value,
                           };

            foreach (var user in XMLUsers)
            {
                allUsers.Add(user);
            }

            return allUsers;
        }


        // =======================================================================
        // Users XML File : write to XML file
        // =======================================================================
        void WriteUserXML(User user)
        {
            List<User> ListUsers = ReadUsersXML();
            int userId = ListUsers.Max(user => user.Id);    // find the highest id

            XDocument XMLFile = XDocument.Load(UriUserXML);

            XMLFile.Element("Users")
                .Elements("User")
                .Where(item => Convert.ToInt32(item.Element("Id").Value) == userId).FirstOrDefault()
                .AddAfterSelf(new XElement("User", new XElement("Id", userId + 1), new XElement("Username", user.Username), new XElement("Password", user.Password)));

            XMLFile.Save(UriUserXML);
        }


        // =======================================================================
        // Check if user already exist
        // =======================================================================
        bool IsUserAlreadyExist(List<User> users, User user)
        {
            User findUser = users.Find(x => x.Username.Equals(user.Username));

            if (findUser != null)
            {
                return true;
            }

            return false;
        }









    }
}