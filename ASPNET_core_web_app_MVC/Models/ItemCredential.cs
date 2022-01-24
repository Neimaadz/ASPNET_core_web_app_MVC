using System;
using Microsoft.AspNetCore.Http;

namespace ASPNET_core_web_app_MVC.Models
{
    public class ItemCredential
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string Localisation { get; set; }
        public string Description { get; set; }
        public IFormFile Image { get; set; }    // for sending file
    }
}
