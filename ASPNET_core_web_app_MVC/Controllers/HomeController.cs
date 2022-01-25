﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Xml.Linq;
using ASPNET_core_web_app_MVC.Authentication;
using ASPNET_core_web_app_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IJwtAuth jwtAuth;
        private string UriUserXML = $@"{Directory.GetCurrentDirectory()}/Data/Users.xml";
        private string UriItemsJSON = $@"{Directory.GetCurrentDirectory()}/Data/Items.json";

        // public HomeController(ILogger<HomeController> logger)
        public HomeController(IJwtAuth jwtAuth, IWebHostEnvironment hostEnvironment)
        {
            // _logger = logger;
            this.jwtAuth = jwtAuth;
            webHostEnvironment = hostEnvironment;
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
            HttpContext.Session.Remove("search");
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

            List<string> listLocalisations = new List<string>();
            listLocalisations = ReadLocalisationsJSON();
            ViewBag.Localisations = listLocalisations;

            return View("Items", listItems);
        }

        // =======================================================================
        // Search items : using <form> instead of @BeginForm() to pass value into
        //                route value with method Get
        // =======================================================================
        [HttpGet("items/search")]
        public IActionResult SearchItems(string search, string type, string localisation, string sort, string direction)
        {
            // Store arguments into session in purpose to use in SortItemViewComponent
            if (search != null)
            {
                HttpContext.Session.SetString("search", search);
            }
            if (type != null)
            {
                HttpContext.Session.SetString("type", type);
            }
            if (localisation != null)
            {
                HttpContext.Session.SetString("localisation", localisation);
            }
            if (sort != null)
            {
                HttpContext.Session.SetString("sort", sort);
            }
            if (direction != null)
            {
                HttpContext.Session.SetString("direction", direction);
            }

            List<Item> listItems = new List<Item>();
            listItems = SearchUserItems(search, type, localisation, sort, direction);

            return Items(listItems);
        }

        [HttpPost("items/ResetSearchItems")]
        public IActionResult ResetSearchItems()
        {
            // delete argument stored in session
            HttpContext.Session.Remove("search");
            HttpContext.Session.Remove("type");
            HttpContext.Session.Remove("localisation");
            HttpContext.Session.Remove("sort");
            HttpContext.Session.Remove("direction");

            return RedirectToAction("Items");
        }


        // =======================================================================
        // Add items
        // =======================================================================
        [HttpGet("items/add")]
        public IActionResult AddItems()
        {
            List<string> listTypes = new List<string>();
            listTypes = ReadTypesJSON();
            ViewBag.Types = listTypes;

            List<string> listLocalisations = new List<string>();
            listLocalisations = ReadLocalisationsJSON();
            ViewBag.Localisations = listLocalisations;

            return View();
        }

        [HttpPost("items/add")]  // define route : https://localhost:<port>/items
        public IActionResult AddItems([FromForm] ItemCredential itemCredential)
        {
            List<Item> ListItems = ReadItemsJSON(); // to find the highest id in ALL items list
            int itemId = ListItems.Max(item => item.ItemId);    // find the highest id
            string uniqueFileName;

            if (itemCredential.Image != null)
            {
                uniqueFileName = UploadedFile(itemCredential.Image);
            }
            else
            {
                uniqueFileName = "no_image.png";
            }

            Item item = new Item()
            {
                ItemId = itemId + 1,
                UserId = Int32.Parse(User.FindFirstValue("id")),
                Name = itemCredential.Name,
                Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                Type = itemCredential.Type,
                Localisation = itemCredential.Localisation,
                Description = itemCredential.Description,
                Image = uniqueFileName,
            };

            WriteItemJSON(item);

            return RedirectToAction("Items");
        }

        // =======================================================================
        // Edit item
        // =======================================================================
        [HttpPost("items/edit/{itemId}")]
        public IActionResult EditItems(int itemId, [FromForm] Item item, [FromForm] IFormFile image)
        {
            Item itemToEdit = FindItemByItemId(itemId);
            string uniqueFileName;

            if (image != null)
            {
                uniqueFileName = UploadedFile(image);
            }
            else
            {
                uniqueFileName = "no_image.png";
            }

            if (itemToEdit.UserId == Int32.Parse(User.FindFirstValue("id")))
            {
                EditItemByItemId(itemId, item, uniqueFileName);
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
        [HttpPost("items/delete/{itemId}")]
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
        // Details item
        // =======================================================================
        [HttpGet("items/details/{itemId}")]
        public IActionResult DetailsItems(int itemId)
        {
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            if (currentUserId == FindItemByItemId(itemId).UserId)   // check on ALL the list of item
            {
                Item item = FindUserItemByItemId(itemId);   // get ONLY user's item
                ViewBag.DetailsItems = item;

                return View();
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
        // Search only User's Items
        // =======================================================================
        List<Item> SearchUserItems(string search, string type, string localisation, string sort, string direction)
        {
            List<Item> listResultItems = new List<Item>();

            // Définir ma source de données
            var listItems = ReadItemsJSON();
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token
            var query = listItems.Where(items => items.UserId == currentUserId);

            // Appel de la requête
            foreach (var item in query)
            {
                listResultItems.Add(item);
            }

            var propertyInfo = typeof(Item);

            // use PREVIOUS list to restrict area sort and filter
            if (direction == "ASC")
            {
                var myQuery = listResultItems.OrderBy(x => propertyInfo.GetProperty(sort).GetValue(x, null)); // get property of Sort;
                listResultItems = new List<Item>(); // clear the PREVIOUS list in order to add new params sort and filter elements

                foreach (var item in myQuery.ToList())
                {
                    listResultItems.Add(item);
                }

            }
            if (direction == "DSC")
            {
                var myQuery = listResultItems.OrderByDescending(x => propertyInfo.GetProperty(sort).GetValue(x, null));
                listResultItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listResultItems.Add(item);
                }

            }
            if (type != null)
            {
                var myQuery = listResultItems.Where(items => items.Type == type);
                listResultItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listResultItems.Add(item);
                }

            }
            if (localisation != null)
            {
                var myQuery = listResultItems.Where(items => items.Localisation == localisation);
                listResultItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listResultItems.Add(item);
                }
            }
            if (search != null)
            {
                var myQuery = listResultItems.Where(items => items.UserId == currentUserId &&
                items.Name.ToLower().Contains(search.ToLower()));
                listResultItems = new List<Item>();

                foreach (var item in myQuery.ToList())
                {
                    listResultItems.Add(item);
                }
            }

            return listResultItems;
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
        // Find ONLY user's item by itemId
        // =======================================================================
        Item FindUserItemByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> listItems = ReadUserItems();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            listItems.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Edit Item
        // =======================================================================
        void EditItemByItemId(int itemId, Item item, string image)
        {
            // delete file associated to item
            var BeforeEditTheItem = FindItemByItemId(itemId);   // Getting image before apply edition on the item (sended from form)
            if (BeforeEditTheItem.Image != null && BeforeEditTheItem.Image != "no_image.png")
            {
                string itemImagePath = Path.Combine(webHostEnvironment.WebRootPath, "images") + "/" + BeforeEditTheItem.Image;
                System.IO.File.Delete(itemImagePath);
            }

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
                    x.Image = image;
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
            // delete file associated to item
            var item = FindItemByItemId(itemId);
            if (item.Image != null && item.Image != "no_image.png") // avoid the unauthorize access exception by checking if image is null
            {
                string itemImagePath = Path.Combine(webHostEnvironment.WebRootPath, "images") + "/" + item.Image;
                System.IO.File.Delete(itemImagePath);
            }

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

        // =======================================================================
        // Upload file
        // =======================================================================
        string UploadedFile(IFormFile image)
        {
            string uniqueFileName = null;

            if (image != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
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
        // Read all items property Localisation from JSON file
        // =======================================================================
        public static List<string> ReadLocalisationsJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Localisations.json";
            List<string> listLocalisation = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var myQuery = from localisation in JSONFile["Localisations"]
                            orderby localisation["Name"] ascending
                            select localisation;

            foreach (var localisation in myQuery)
            {
                listLocalisation.Add(localisation.ToObject<Models.ModelDataLocalisation>().Name);
            }

            return listLocalisation;
        }









    }
}