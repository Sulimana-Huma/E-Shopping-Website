using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class ReviewViewModel
    {
        public int review_id { get; set; }
        public int product_id { get; set; }
        public int user_id { get; set; }
        public int rating { get; set; }
        public string review_message { get; set; } // Make sure the property name is correct
        public DateTime? review_date { get; set; } // If the 'Review' model contains a 'review_date', use it here. Otherwise, consider removing this field if not available.
        public string ReviewerName { get; set; }

        public virtual tbl_user tbl_user { get; set; }
    }
}