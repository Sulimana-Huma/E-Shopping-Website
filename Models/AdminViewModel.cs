using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class AdminViewModel
    {
        public int AdminId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> Categories { get; set; }

        public bool IsAccessGranted { get; set; }
    }

}