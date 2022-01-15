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
        public IActionResult Signin([FromForm] User newUser)
        {
            User user = new User();

            int id = newUser.Id;
            string username = newUser.Username;
            string password = newUser.Password;

            user = new User()
            {
                Id = 0,
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
        // Items JSON File : read all items from JSON file
        // =======================================================================
        List<Item> ReadItemsByUser()
        {
            List<Item> allItems = new List<Item>();

            // Définir ma source de données
            var listUsers = ReadUserXML();
            var listItems = ReadItemsJSON();
            var currentId = User.FindFirstValue("id");  // get the user id from token

            // Création de la requête
            var itemsByUser = from items in listItems
                              where items.UserId == Int32.Parse(currentId)
                              select new Item()
                              {
                                  ItemId = items.ItemId,
                                  UserId = items.UserId,
                                  Name = items.Name
                              };

            // Appel de la requête
            foreach (var items in itemsByUser)
            {
                allItems.Add(items);
            }
            return allItems;
        }


        // =======================================================================
        // Items JSON File : read all items from JSON file
        // =======================================================================
        static List<Item> ReadItemsJSON()
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
            int countUser = ListUsers.Count;

            XDocument XMLFile = XDocument.Load(UriUserXML);

            XMLFile.Element("Users")
                .Elements("User")
                .Where(item => Convert.ToInt32(item.Element("Id").Value) == countUser).FirstOrDefault()
                .AddAfterSelf(new XElement("User", new XElement("Id", countUser + 1), new XElement("Username", user.Username), new XElement("Password", user.Password)));

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