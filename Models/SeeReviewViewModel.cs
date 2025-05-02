using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class SeeReviewViewModel
    {
        public string SellerName { get; set; }
        public string SellerContact { get; set; }
        public string BuyerName { get; set; }
        public string BuyerContact { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ProductImage { get; set; }

        public string ProductPrice { get; set; }
        public string ReviewMessage { get; set; }
        public int Rating { get; set; }  // Rating (from 1 to 5)
    }
}