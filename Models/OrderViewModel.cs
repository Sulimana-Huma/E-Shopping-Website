using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_Shopping_Website.Models
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string ProName { get; set; }
        public int ProId { get; set; }
        public string ProImage { get; set; }
        public decimal ProPrice { get; set; }
        public string ProDes { get; set; }
        public string SellerEmail { get; set; }
        public string SellerContact { get; set; }
        public string SellerCity { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; }

        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public bool HasReview { get; set; } // Indicates whether a review exists for the order

    }

}