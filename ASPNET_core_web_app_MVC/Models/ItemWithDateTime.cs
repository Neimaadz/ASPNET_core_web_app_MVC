using System;

namespace ASPNET_core_web_app_MVC.Models
{
    public class ItemWithDateTime
    {
        public int ItemId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Localisation { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }
}
