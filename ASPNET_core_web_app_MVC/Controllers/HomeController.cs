using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Xml.Linq;
using ASPNET_core_web_app_MVC.Authentication;
using ASPNET_core_web_app_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Items()
        {
            // delete argument stored in session
            HttpContext.Session.Remove("type");
            HttpContext.Session.Remove("localisation");
            HttpContext.Session.Remove("sort");
            HttpContext.Session.Remove("direction");

            List<Item> listItems = new List<Item>();
            listItems = ReadUserItems(); // read only user's items

            return Items(listItems);
        }

        public IActionResult Items(List<Item> listItems)
        {
            List<string> listTypes = new List<string>();
            listTypes = ReadTypesJSON();
            ViewBag.Types = listTypes;

            List<string> listCommunes = new List<string>();
            listCommunes = ReadCommunesJSON();
            ViewBag.Communes = listCommunes;

            return View("Items", listItems);
        }

        // =======================================================================
        // Sort items
        // =======================================================================
        [HttpPost("sortitems")]
        public IActionResult SortItems(string type = "", string localisation="", string sort="", string direction="")
        {
            // Store arguments into session in purpose to use in SortItemViewComponent
            HttpContext.Session.SetString("type", type);
            HttpContext.Session.SetString("localisation", localisation);
            HttpContext.Session.SetString("sort", sort);
            HttpContext.Session.SetString("direction", direction);

            List<Item> listItems = new List<Item>();
            listItems = SortUserItems(type, localisation, sort, direction);

            return Items(listItems);
        }

        // =======================================================================
        // Search items
        // =======================================================================
        [HttpPost("searchitems")]
        public IActionResult SearchItems([FromForm] string search)
        {
            List<Item> listItems = new List<Item>();
            listItems = SearchUserItems(search);

            return Items(listItems);
        }

        // =======================================================================
        // Add items
        // =======================================================================
        [HttpGet("additems")]
        public IActionResult AddItems()
        {
            List<string> listTypes = new List<string>();
            listTypes = ReadTypesJSON();
            ViewBag.Types = listTypes;

            List<string> listCommunes = new List<string>();
            listCommunes = ReadCommunesJSON();
            ViewBag.Communes = listCommunes;

            return View();
        }

        [HttpPost("additems")]  // define route : https://localhost:<port>/items
        public IActionResult AddItems([FromForm] ItemCredential itemCredential)
        {
            List<Item> ListItems = ReadItemsJSON(); // to find the highest id in ALL items list
            int itemId = ListItems.Max(item => item.ItemId);    // find the highest id

            Item item = new Item()
            {
                ItemId = itemId + 1,
                UserId = Int32.Parse(User.FindFirstValue("id")),
                Name = itemCredential.Name,
                Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                Type = itemCredential.Type,
                Localisation = itemCredential.Localisation,
                Description = itemCredential.Description
            };

            WriteItemJSON(item);

            return RedirectToAction("Items");
        }

        // =======================================================================
        // Edit item
        // =======================================================================
        [HttpPost("editItems/{itemId}")]
        public IActionResult EditItems(int itemId, [FromForm] Item item)
        {
            Item itemtoEdit = FindItemByItemId(itemId);
            if (itemtoEdit.UserId == Int32.Parse(User.FindFirstValue("id")))
            {
                EditItemByItemId(itemId, item);
                return RedirectToAction("Items");
            }
            else
            {
                return Unauthorized();
            }
        }


        // =======================================================================
        // Delete item
        // =======================================================================
        [HttpPost("deleteItems/{itemId}")]
        public IActionResult DeleteItems(int itemId)
        {
            Item item = FindItemByItemId(itemId);
            if (item.UserId == Int32.Parse(User.FindFirstValue("id")))
            {
                DeleteItemByItemId(itemId);
                return RedirectToAction("Items");
            }
            else
            {
                return Unauthorized();
            }
        }


        // =======================================================================
        // All users
        // =======================================================================
        [Authorize(Roles = "admin")]    // use role to authorize
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
         *              METHODS
         * 
         * 
         * 
         * 
         * 
         */



        // =======================================================================
        // Sort only User's Items
        // =======================================================================
        List<Item> SortUserItems(string type, string localisation, string sort, string direction)
        {
            List<Item> listSortedItems = new List<Item>();

            // Définir ma source de données
            var listItems = ReadItemsJSON();
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token
            var query = listItems.Where(items => items.UserId == currentUserId);

            // Appel de la requête
            foreach (var item in query)
            {
                listSortedItems.Add(item);
            }

            var propertyInfo = typeof(Item);

            // use PREVIOUS list to restrict area sort and filter
            if (direction == "ASC")
            {
                var myQuery = listSortedItems.OrderBy(x => propertyInfo.GetProperty(sort).GetValue(x, null)); // get property of Sort;
                listSortedItems = new List<Item>(); // clear the PREVIOUS list in order to add new params sort and filter elements

                foreach (var item in myQuery.ToList())
                {
                    listSortedItems.Add(item);
                }

            }
            if (direction == "DSC")
            {
                var myQuery = listSortedItems.OrderByDescending(x => propertyInfo.GetProperty(sort).GetValue(x, null));
                listSortedItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listSortedItems.Add(item);
                }

            }
            if (type != "")
            {
                var myQuery = listSortedItems.Where(items => items.Type == type);
                listSortedItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listSortedItems.Add(item);
                }

            }
            if (localisation != "")
            {
                var myQuery = listSortedItems.Where(items => items.Localisation == localisation);
                listSortedItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listSortedItems.Add(item);
                }
            }

            return listSortedItems;
        }


        // =======================================================================
        // Search User's Items
        // =======================================================================
        List<Item> SearchUserItems(string search)
        {
            List<Item> listSearchedItems = new List<Item>();

            // Définir ma source de données
            var listItems = ReadItemsJSON();
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            var myQuery = listItems.Where(items => items.UserId == currentUserId &&
                items.Name.ToLower().Contains(search.ToLower()));

            // Appel de la requête
            foreach (var item in myQuery)
            {
                listSearchedItems.Add(item);
            }

            return listSearchedItems;
        }


        // =======================================================================
        // Find Item by itemId
        // =======================================================================
        Item FindItemByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> listItems = ReadItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            listItems.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Edit Item
        // =======================================================================
        void EditItemByItemId(int itemId, Item item)
        {
            List<Item> Items = ReadItemsJSON(); // name is important to write with this specific name in JSON
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            Items.ForEach(x => {
                if (x.ItemId.Equals(itemId) && x.UserId.Equals(currentUserId))
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
        // Delete Item by itemId
        // =======================================================================
        void DeleteItemByItemId(int itemId)
        {
            int index = 0;
            List<Item> Items = ReadItemsJSON(); // name is important to write with this specific name in JSON
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            Items.ForEach(x => { if (x.ItemId.Equals(itemId) && x.UserId.Equals(currentUserId)) index=Items.IndexOf(x); });
            Items.RemoveAt(index);

            var allItems = new { Items };   // Permet d'ajouter la propriété "Items" dans JSON

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);

        }


        // =======================================================================
        // Read only User's items
        // =======================================================================
        List<Item> ReadUserItems()
        {
            List<Item> listUserItems = new List<Item>();

            // Définir ma source de données
            var listItems = ReadItemsJSON();
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            var myQuery = listItems.Where(items => items.UserId == currentUserId);

            // Appel de la requête
            foreach (var item in myQuery)
            {
                listUserItems.Add(item);
            }

            return listUserItems;
        }


        // =======================================================================
        // Write Items into JSON File
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
        // Read All items from JSON File
        // =======================================================================
        List<Item> ReadItemsJSON()
        {
            List<Item> listItems = new List<Item>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriItemsJSON));
            var myQuery = from items in JSONFile["Items"]
                            select items;

            foreach (var item in myQuery)
            {
                listItems.Add(item.ToObject<Item>());
            }

            return listItems;
        }


        // =======================================================================
        // Read Users XML File : read all users from XML file
        // =======================================================================
        List<User> ReadUsersXML()
        {
            List<User> listUsers = new List<User>();

            var XMLFile = XElement.Load(UriUserXML);
            var myQuery = from element in XMLFile.Descendants("User")
                           select new User()
                           {
                               Id = Convert.ToInt32(element.Element("Id").Value),
                               Username = element.Element("Username").Value,
                               Password = element.Element("Password").Value,
                           };

            foreach (var user in myQuery)
            {
                listUsers.Add(user);
            }

            return listUsers;
        }


        // =======================================================================
        // Write Users XML File : write to XML file
        // =======================================================================
        void WriteUserXML(User user)
        {
            List<User> listUsers = ReadUsersXML();
            int userId = listUsers.Max(user => user.Id);    // find the highest id

            XDocument XMLFile = XDocument.Load(UriUserXML);

            XMLFile.Element("Users")
                .Elements("User")
                .Where(item => Convert.ToInt32(item.Element("Id").Value) == userId).FirstOrDefault()
                .AddAfterSelf(new XElement("User", new XElement("Id", userId + 1), new XElement("Username", user.Username), new XElement("Password", user.Password), new XElement("Role", "user")));

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



        /*
         * 
         * 
         * 
         * 
         *                  STATIC METHODS
         * 
         * 
         * 
         * 
         * 
         * 
         */


        // =======================================================================
        // Read all items property Type from JSON file
        // =======================================================================
        public static List<string> ReadTypesJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Types.json";
            List<string> listTypes = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var myQuery = from types in JSONFile["Types"]
                            orderby types["Name"] ascending
                            select types;

            foreach (var type in myQuery)
            {
                listTypes.Add(type.ToObject<Models.ModelDataType>().Name);
            }

            return listTypes;
        }

        // =======================================================================
        // Read all items property Commune from JSON file
        // =======================================================================
        public static List<string> ReadCommunesJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Communes.json";
            List<string> listCommunes = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var myQuery = from communes in JSONFile["Communes"]
                            orderby communes["Name"] ascending
                            select communes;

            foreach (var commune in myQuery)
            {
                listCommunes.Add(commune.ToObject<Models.ModelDataCommune>().Name);
            }

            return listCommunes;
        }









    }
}