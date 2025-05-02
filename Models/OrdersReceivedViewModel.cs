using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class OrdersReceivedViewModel
    {
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int ProductPrice { get; set; }
        public string ProductImage { get; set; }
        public string BuyerName { get; set; }
        public string BuyerEmail { get; set; }
        public string BuyerContact { get; set; }
        public string BuyerImage { get; set; }

        public DateTime OrderDate { get; set; }

        public int OrderId { get; set; }  // To hold the Order ID
        public int ProductId { get; set; }  // To hold the Product ID
        public string ReviewStatus { get; set; }
    }
}