using System;
using System.Collections.Generic;
using System.Globalization;
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
        private string[] allowedFile = new string[] { ".png", ".jpg", ".jpeg" };

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

            List<Item> items = ReadUserItems(); // read only user's items

            return Items(items);
        }

        public IActionResult Items(List<Item> items)
        {
            List<string> types = ReadTypesJSON();
            ViewBag.Types = types;

            List<string> localisations = ReadLocalisationsJSON();
            ViewBag.Localisations = localisations;

            return View("Items", items);
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

            List<Item> searchedItems = SearchUserItems(search, type, localisation, sort, direction);

            return Items(searchedItems);
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
            List<string> types = ReadTypesJSON();
            ViewBag.Types = types;

            List<string> localisations = ReadLocalisationsJSON();
            ViewBag.Localisations = localisations;

            return View();
        }

        [HttpPost("items/add")]  // define route : https://localhost:<port>/items
        public IActionResult AddItems([FromForm] Item item, [FromForm] IFormFile image)
        {
            List<Item> items = ReadItemsJSON(); // to find the highest id in ALL items list
            int itemId = items.Max(item => item.ItemId);    // find the highest id
            string uniqueFileName;

            if (image != null)
            {
                string fileExtension = Path.GetExtension(image.FileName);

                if (allowedFile.Contains(fileExtension))
                {
                    uniqueFileName = UploadFile(image);
                }
                else
                {
                    ErrorViewModel error = new ErrorViewModel();
                    error.StatusCode = StatusCodes.Status403Forbidden;
                    error.Message = "Invalid file !";

                    return View("Error", error);
                }
            }
            else
            {
                uniqueFileName = "no_image.png";
            }

            Item newItem = new Item()
            {
                ItemId = itemId + 1,
                UserId = Int32.Parse(User.FindFirstValue("id")),
                Name = item.Name,
                Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                Type = item.Type,
                Localisation = item.Localisation,
                Description = item.Description,
                Image = uniqueFileName,
            };

            WriteItemJSON(newItem);

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
                string fileExtension = Path.GetExtension(image.FileName);

                if (allowedFile.Contains(fileExtension))
                {
                    uniqueFileName = UploadFile(image);
                }
                else
                {
                    ErrorViewModel error = new ErrorViewModel();
                    error.StatusCode = StatusCodes.Status403Forbidden;
                    error.Message = "Invalid file !";

                    return View("Error", error);
                }
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

            if (currentUserId == FindItemByItemId(itemId).UserId || User.IsInRole("admin"))   // check on ALL the list of item
            {
                if (User.IsInRole("admin")) // ADMIN have authorization
                {
                    Item item = FindItemByItemId(itemId);
                    ViewBag.DetailsItems = item;
                }
                else
                {
                    Item item = FindUserItemByItemId(itemId);   // get ONLY user's item
                    ViewBag.DetailsItems = item;
                }

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
            List<Tuple<User, List<Item>>> searchedUsersItems = new List<Tuple<User, List<Item>>>();
            return View(searchedUsersItems);
        }

        public IActionResult Users(List<Tuple<User, List<Item>>> searchedUsersItems)
        {
            return View("Users", searchedUsersItems);
        }

        [Authorize(Roles = "admin")]    // use role to authorize
        [HttpPost("users")]
        public IActionResult SearchUsers([FromForm] string username)
        {
            List<Tuple<User, List<Item>>> searchedUsersItems = SearchUsersItemsByName(username);

            return Users(searchedUsersItems);
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
                List<User> users = ReadUsersXML();

                if (!IsUserAlreadyExist(users, user))
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

        // =======================================================================
        // Export my items to file JSON 
        // =======================================================================
        [HttpGet("exportJson")]
        public IActionResult ExportJSON()
        {
            // Création du fichier à download contenant nos Items en JSON
            string UriMyItemsJSON = $@"{Directory.GetCurrentDirectory()}/Data/myItemsJSON.json";
            List<Item> Items = ReadUserItems();
            var allItems = new { Items };

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            System.IO.File.WriteAllText(UriMyItemsJSON, json);

            // Préparation pour le download
            var net = new System.Net.WebClient();
            var data = net.DownloadData(UriMyItemsJSON);
            var content = new System.IO.MemoryStream(data);
            var contentType = "APPLICATION/octet-stream";
            var fileName = "myItems.json";

            System.IO.File.Delete(UriMyItemsJSON);
            return File(content, contentType, fileName);
        }

        // =======================================================================
        // Export my items to file XML 
        // =======================================================================
        [HttpGet("exportXml")]
        public IActionResult ExportXML()
        {
            // Création du fichier à download contenant nos Items en JSON
            string UriMyItemsJSON = $@"{Directory.GetCurrentDirectory()}/Data/myItemsJSON.json";
            List<Item> Items = ReadUserItems();
            var allItems = new { Items };

            string json = JsonConvert.SerializeObject(allItems, Formatting.Indented);
            System.IO.File.WriteAllText(UriMyItemsJSON, json);

            // Convertir JSON en XML
            string UriMyItemsXML = $@"{Directory.GetCurrentDirectory()}/Data/myItemsXML.xml";
            var myJObject = JObject.Parse(json);    // on parse pour obtenir un JObject
            var xml = new XDocument(
                new XElement("Items",                  // XML Noeud Parent/Root
                    myJObject["Items"].Select(c =>       // On selectionne dans notre Objet JSON
                        new XElement("item",           // XML Noeuds enfants
                            new XElement("ItemId", (int)c["ItemId"]),
                            new XElement("UserId", (int)c["UserId"]),
                            new XElement("Name", (string)c["Name"]),
                            new XElement("Date", (string)c["Date"]),
                            new XElement("Type", (string)c["Type"]),
                            new XElement("Localisation", (string)c["Localisation"]),
                            new XElement("Description", (string)c["Description"]),
                            new XElement("Image", (string)c["Image"])
                        )
                    )
                )
            );
            xml.Save(UriMyItemsXML);    // save le fichier XML pour qu'on puisse le download

            // Préparation pour le download
            var net = new System.Net.WebClient();
            var data = net.DownloadData(UriMyItemsXML);
            var content = new System.IO.MemoryStream(data);
            var contentType = "APPLICATION/octet-stream";
            var fileName = "myItems.xml";

            System.IO.File.Delete(UriMyItemsJSON);
            System.IO.File.Delete(UriMyItemsXML);
            return File(content, contentType, fileName);
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
        // Search Users and items associated : search users and user's items by username
        // =======================================================================
        List<Tuple<User, List<Item>>> SearchUsersItemsByName(string username)
        {
            List<Tuple<User, List<Item>>> searchedUsersItems = new List<Tuple<User, List<Item>>>();

            List<User> users = ReadUsersXML();
            List<Item> items = ReadItemsJSON();

            var myQuery = from user in users
                          where (user.Username).ToLower().Contains(username.ToLower())
                          join item in items on user.Id equals item.UserId
                          select new
                          {
                              user = user,
                              item = item
                          }
                          into responseQuery // résultat de la requête dans responseQuery
                          group responseQuery by responseQuery.user;

            foreach (var userQuery in myQuery)
            {
                List<Item> itemsResult = new List<Item>();

                foreach (var item in userQuery)
                {
                    itemsResult.Add(item.item);
                }
                searchedUsersItems.Add(new Tuple<User, List<Item>>(userQuery.Key, itemsResult));
            }

            return searchedUsersItems;
        }


        // =======================================================================
        // Search/filter/sort only User's Items
        // =======================================================================
        List<Item> SearchUserItems(string search, string type, string localisation, string sort, string direction)
        {
            List<ItemWithDateTime> searchedItems = new List<ItemWithDateTime>();
            var propertyInfo = typeof(ItemWithDateTime);     // get the type

            // Définir ma source de données
            var items = MapperItemToItemWithDateTimeType(ReadItemsJSON());
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token
            var query = items.Where(items => items.UserId == currentUserId);

            // Appel de la requête
            foreach (var item in query)
            {
                searchedItems.Add(item);
            }

            // use PREVIOUS list to restrict area sort and filter
            if (direction == "ASC")
            {
                var myQuery = searchedItems.OrderBy(x => propertyInfo.GetProperty(sort).GetValue(x, null)); // get the value of the property equal to "sort" ;
                searchedItems = new List<ItemWithDateTime>(); // clear the PREVIOUS list in order to add new params sort and filter elements

                foreach (var item in myQuery.ToList())
                {
                    searchedItems.Add(item);
                }
            }
            if (direction == "DSC")
            {
                var myQuery = searchedItems.OrderByDescending(x => propertyInfo.GetProperty(sort).GetValue(x, null));
                searchedItems = new List<ItemWithDateTime>();

                foreach (var item in myQuery.ToList())
                {
                    searchedItems.Add(item);
                }
            }
            if (type != null)
            {
                var myQuery = searchedItems.Where(items => items.Type == type);
                searchedItems = new List<ItemWithDateTime>();

                foreach (var item in myQuery.ToList())
                {
                    searchedItems.Add(item);
                }
            }
            if (localisation != null)
            {
                var myQuery = searchedItems.Where(items => items.Localisation == localisation);
                searchedItems = new List<ItemWithDateTime>();

                foreach (var item in myQuery.ToList())
                {
                    searchedItems.Add(item);
                }
            }
            if (search != null)
            {
                var myQuery = searchedItems.Where(items => items.UserId == currentUserId &&
                items.Name.ToLower().Contains(search.ToLower()));
                searchedItems = new List<ItemWithDateTime>();

                foreach (var item in myQuery.ToList())
                {
                    searchedItems.Add(item);
                }
            }

            return MapperItemWithDateTimeTypeToItem(searchedItems);
        }


        // =======================================================================
        // Find Item by itemId
        // =======================================================================
        Item FindItemByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> items = ReadItemsJSON();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            items.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Find ONLY user's item by itemId
        // =======================================================================
        Item FindUserItemByItemId(int itemId)
        {
            Item item = new Item();
            List<Item> userItems = ReadUserItems();

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            userItems.ForEach(x => { if (x.ItemId.Equals(itemId)) item = x; });
            return item;
        }


        // =======================================================================
        // Edit Item
        // =======================================================================
        void EditItemByItemId(int itemId, Item currentItem, string image)
        {
            // delete file associated to item
            var itemFound = FindItemByItemId(itemId);   // Getting image before apply edition on the item (sended from form)
            if (itemFound.Image != null && itemFound.Image != "no_image.png")
            {
                string itemImagePath = Path.Combine(webHostEnvironment.WebRootPath, "images") + "/" + itemFound.Image;
                System.IO.File.Delete(itemImagePath);
            }

            var Items = ReadItemsJSON(); // name is important to write with this specific name in JSON
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            //Items.ForEach(x => {
            //    if (x.ItemId.Equals(itemId) && x.UserId.Equals(currentUserId))
            //    {
            //        x.Name = currentItem.Name;
            //        x.Type = currentItem.Type;
            //        x.Localisation = currentItem.Localisation;
            //        x.Description = currentItem.Description;
            //        x.Image = image;
            //    }
            //});

            var myQuery = from item in Items
                          where item.ItemId.Equals(itemId) && item.UserId.Equals(currentUserId)
                          select item;

            foreach (var item in myQuery)
            {
                item.Name = currentItem.Name;
                item.Type = currentItem.Type;
                item.Localisation = currentItem.Localisation;
                item.Description = currentItem.Description;
                item.Image = image;
            }

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
            var itemFound = FindItemByItemId(itemId);
            if (itemFound.Image != null && itemFound.Image != "no_image.png") // avoid the unauthorize access exception by checking if image is null
            {
                string itemImagePath = Path.Combine(webHostEnvironment.WebRootPath, "images") + "/" + itemFound.Image;
                System.IO.File.Delete(itemImagePath);
            }

            var Items = ReadItemsJSON(); // name is important to write with this specific name in JSON
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            // Utilisation du ForEach() au lieu de LinQ car qu'une seule unique ID
            //int index = 0;
            //Items.ForEach(x => { if (x.ItemId.Equals(itemId) && x.UserId.Equals(currentUserId)) index=Items.IndexOf(x); });
            //Items.RemoveAt(index);

            Item itemToDelete = new Item();
            var myQuery = from item in Items
                          where item.ItemId.Equals(itemId) && item.UserId.Equals(currentUserId)
                          select item;

            foreach (var item in myQuery)
            {
                itemToDelete = item;
            }
            Items.Remove(itemToDelete);

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
            List<Item> userItems = new List<Item>();

            // Définir ma source de données
            var items = ReadItemsJSON();
            var currentUserId = Int32.Parse(User.FindFirstValue("id"));  // get the user id from token

            var myQuery = items.Where(items => items.UserId == currentUserId);

            // Appel de la requête
            foreach (var item in myQuery)
            {
                userItems.Add(item);
            }

            return userItems;
        }


        // =======================================================================
        // Read All items from JSON File
        // =======================================================================
        List<Item> ReadItemsJSON()
        {
            List<Item> items = new List<Item>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriItemsJSON));
            var myQuery = from item in JSONFile["Items"]
                            select item.ToObject<Item>();

            foreach (var item in myQuery)
            {
                items.Add(item);
            }

            return items;
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
        // Read Users XML File : read all users from XML file
        // =======================================================================
        List<User> ReadUsersXML()
        {
            List<User> users = new List<User>();

            var XMLFile = XElement.Load(UriUserXML);
            var myQuery = from element in XMLFile.Descendants("User")
                           select new User()
                           {
                               Id = Convert.ToInt32(element.Element("Id").Value),
                               Username = element.Element("Username").Value,
                               Password = element.Element("Password").Value,
                               Role = element.Element("Role").Value
                           };

            foreach (var user in myQuery)
            {
                users.Add(user);
            }

            return users;
        }


        // =======================================================================
        // Write Users XML File : write to XML file
        // =======================================================================
        void WriteUserXML(User user)
        {
            List<User> users = ReadUsersXML();
            int userId = users.Max(user => user.Id);    // find the highest id

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
        string UploadFile(IFormFile image)
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

        // =======================================================================
        // Mapper Item to Item with DateTime type
        // =======================================================================
        List<ItemWithDateTime> MapperItemToItemWithDateTimeType(List<Item> items)
        {
            List<ItemWithDateTime> itemsWithDateTime = new List<ItemWithDateTime>();

            foreach (var item in items)
            {
                ItemWithDateTime itemWithDateTime = new ItemWithDateTime
                {
                    ItemId = item.ItemId,
                    UserId = item.UserId,
                    Name = item.Name,
                    Date = DateTime.ParseExact(item.Date, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                    Type = item.Type,
                    Localisation = item.Localisation,
                    Description = item.Description,
                    Image = item.Image,
                };
                itemsWithDateTime.Add(itemWithDateTime);
            }

            return itemsWithDateTime;
        }

        // =======================================================================
        // Mapper Item with DateTime type to Item
        // =======================================================================
        List<Item> MapperItemWithDateTimeTypeToItem(List<ItemWithDateTime> itemsWithDateTime)
        {
            List<Item> items = new List<Item>();

            foreach (var itemWithDateTime in itemsWithDateTime)
            {
                Item item = new Item
                {
                    ItemId = itemWithDateTime.ItemId,
                    UserId = itemWithDateTime.UserId,
                    Name = itemWithDateTime.Name,
                    Date = itemWithDateTime.Date.ToString("dd/MM/yyyy HH:mm"),
                    Type = itemWithDateTime.Type,
                    Localisation = itemWithDateTime.Localisation,
                    Description = itemWithDateTime.Description,
                    Image = itemWithDateTime.Image,
                };
                items.Add(item);
            }

            return items;
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
            List<string> types = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var myQuery = from type in JSONFile["Types"]
                            orderby type["Name"] ascending
                            select type;

            foreach (var type in myQuery)
            {
                types.Add(type.ToObject<Models.ModelDataType>().Name);
            }

            return types;
        }

        // =======================================================================
        // Read all items property Localisation from JSON file
        // =======================================================================
        public static List<string> ReadLocalisationsJSON()
        {
            string UriJSON = $@"{Directory.GetCurrentDirectory()}/Data/Localisations.json";
            List<string> localisations = new List<string>();

            var JSONFile = JObject.Parse(System.IO.File.ReadAllText(UriJSON));
            var myQuery = from localisation in JSONFile["Localisations"]
                            orderby localisation["Name"] ascending
                            select localisation;

            foreach (var localisation in myQuery)
            {
                localisations.Add(localisation.ToObject<Models.ModelDataLocalisation>().Name);
            }

            return localisations;
        }









    }
}