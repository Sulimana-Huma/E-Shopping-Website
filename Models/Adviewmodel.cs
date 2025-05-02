using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class Adviewmodel
    {
        public int pro_id { get; set; }
        public string pro_name { get; set; }
        public string pro_image { get; set; }
        public string pro_des { get; set; }
        public Nullable<int> pro_price { get; set; }
        public int total_availables { get; set; }
        public string u_email { get; set; }

        public int cat_id { get; set; }
        public string cat_name { get; set; }

        public Nullable<int> pro_fk_user { get; set; }
        public Nullable<int> pro_fk_category { get; set; }

        public string u_name { get; set; }

        public string u_image { get; set; }
        public string u_contact { get; set; }

        public List<ReviewViewModel> Reviews { get; set; }
    }


    // Sample code to fetch user details including email
    

}