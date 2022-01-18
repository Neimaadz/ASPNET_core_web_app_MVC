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
        private static string UriUserXML = $@"{Directory.GetCurrentDirectory()}/Data/Users.xml";
        private static string UriItemsJSON = $@"{Directory.GetCurrentDirectory()}/Data/Items.json";

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
        [HttpGet("items")]  // define route : https://localhost:<port>/items
        public IActionResult Items()
        {
            List<Item> allItems = new List<Item>();
            allItems = ReadItemsByUser();

            return View(allItems);
        }


        // =======================================================================
        // Add items
        // =======================================================================
        [HttpGet("additems")]
        public IActionResult AddItems()
        {
            return View();
        }

        [HttpPost("additems")]  // define route : https://localhost:<port>/items
        public IActionResult AddItems([FromForm] ItemCredential itemCredential)
        {
            List<Item> ListItems = ReadUserItemsJSON();
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

            WriteUserItemJSON(item);

            return RedirectToAction("items");
        }

        // =======================================================================
        // Edit item
        // =======================================================================
        [HttpGet("editItems/{itemId}")]
        public IActionResult EditItems(int itemId)
        {
            Item item = FindItemsByItemId(itemId);

            return View(item);
        }

        [HttpPost("editItems/{itemId}")]
        public IActionResult EditItems(int itemId, [FromForm] Item item)
        {
            EditUserItemByItemId(itemId, item);
            return RedirectToAction("items");
        }


        // =======================================================================
        // Delete item
        // =======================================================================
        [HttpPost("deleteItems")]
        public IActionResult DeleteItems(int itemId)
        {
            DeleteUserItemByItemId(itemId);
            return RedirectToAction("items");
        }


        // =======================================================================
        // All users
        // =======================================================================
        [HttpGet("users")]
        public IActionResult Users()
        {
            List<User> allUsers = new List<User>();
            allUsers = ReadUserXML();

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
                allUsers = ReadUserXML();

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
        // Items JSON File : find item by item id
        // =======================================================================
        static Item FindItemsByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> ListItems = ReadUserItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            ListItems.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Items JSON File : read all items from JSON file
        // =======================================================================
        List<Item> ReadItemsByUser()
        {
            List<Item> allItems = new List<Item>();

            // Définir ma source de données
            var listUsers = ReadUserXML();
            var listItems = ReadUserItemsJSON();
            var currentId = User.FindFirstValue("id");  // get the user id from token

            // Création de la requête
            var itemsByUser = from items in listItems
                              where items.UserId == Int32.Parse(currentId)
                              select new Item()
                              {
                                  ItemId = items.ItemId,
                                  UserId = items.UserId,
                                  Name = items.Name,
                                  Date = items.Date,
                                  Type = items.Type,
                                  Localisation = items.Localisation,
                                  Description = items.Description
                              };

            // Appel de la requête
            foreach (var items in itemsByUser)
            {
                allItems.Add(items);
            }
            return allItems;
        }


        // =======================================================================
        // Items JSON File : find item by item id
        // =======================================================================
        static void EditUserItemByItemId(int itemId, Item item)
        {
            List<Item> Items = ReadUserItemsJSON();

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
            var allItems = new { Items };

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);
        }


        // =======================================================================
        // Items JSON File : delete to JSON file
        // =======================================================================
        void DeleteUserItemByItemId(int itemId)
        {
            int index = 0;
            List<Item> Items = ReadUserItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            Items.ForEach(x => { if (x.ItemId.Equals(itemId)) index=Items.IndexOf(x); });
            Items.RemoveAt(index);
            var allItems = new { Items };

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);

        }


        // =======================================================================
        // Items JSON File : write to JSON file
        // =======================================================================
        void WriteUserItemJSON(Item item)
        {
            List<Item> Items = ReadUserItemsJSON();

            Items.Add(item);
            var allItems = new { Items };

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            // Permet d'écrire sur le fichier new.json
            System.IO.File.WriteAllText(UriItemsJSON, json);

        }


        // =======================================================================
        // Items JSON File : read all items from JSON file
        // =======================================================================
        static List<Item> ReadUserItemsJSON()
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
        static List<User> ReadUserXML()
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
        static void WriteUserXML(User user)
        {
            List<User> ListUsers = ReadUserXML();
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
        static bool IsUserAlreadyExist(List<User> users, User user)
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