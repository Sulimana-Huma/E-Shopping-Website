using E_Shopping_Website.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Security.Claims;
using System.Data.Entity.Validation; 
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text;
using System.Data.Entity; 





namespace E_Shopping_Website.Controllers
{

    public class UserController : Controller
    {
        Humaim_ShoppingEntities2 hs = new Humaim_ShoppingEntities2();

        public ActionResult Index(string searchTerm, int? page)
        {
            int pageSize = 99;
            int pageIndex = page ?? 1;

            
            var categories = hs.tbl_category.Where(x => x.cat_status).OrderBy(x => x.cat_id).ToList();

            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                
                var productIds = hs.tbl_product
                    .Where(p => p.pro_name.Contains(searchTerm))
                    .Select(p => p.pro_fk_category) 
                    .Distinct() 
                    .ToList();

                
                categories = categories.Where(c => productIds.Contains(c.cat_id)).ToList();
            }
            ViewBag.SearchTerm = searchTerm;


            var userId = Session["u_id"]; 
            string userImage = null;
            var userType = "";

            if (userId != null)
            {
                userType = GetUserAccountType(userId.ToString());
                int id = Convert.ToInt32(userId);
                var user = hs.tbl_user.Find(id); 
                if (user != null)
                {
                    userImage = user.u_image; 
                }
            }
            ViewBag.UserType = userType;
            ViewBag.UserID = userId;
            
            ViewBag.UserImage = userImage;

           
            IPagedList<tbl_category> pagedCategories = categories.ToPagedList(pageIndex, pageSize);
            return View(pagedCategories);
        }

        public ActionResult MyOrders()
        {
            int userId = Convert.ToInt32(Session["u_id"]);

            if (Session["u_id"] == null)
            {
                return RedirectToAction("Login");
            }
            else
            {
                var orders = hs.Orders
                                .Where(o => o.buyer_id == userId)
                                .Join(hs.tbl_product,
                                      o => o.product_id,
                                      p => p.pro_id,
                                      (o, p) => new
                                      {
                                          o.order_id,
                                          p.pro_name,
                                          p.pro_image,
                                          p.pro_price,
                                          p.pro_des,
                                          p.pro_fk_user,
                                          o.status,
                                          o.Date
                                      })
                                .Join(hs.tbl_user,
                                      o => o.pro_fk_user,
                                      u => u.u_id,
                                      (o, u) => new OrderViewModel
                                      {
                                          OrderId = o.order_id,
                                          ProName = o.pro_name,
                                          ProImage = o.pro_image,
                                          ProPrice = o.pro_price,
                                          ProDes = o.pro_des,
                                          SellerEmail = u.u_email,
                                          SellerContact = u.u_contact,
                                          SellerCity = u.u_city,
                                          Status = o.status,
                                          Date = o.Date ?? DateTime.MinValue, 
                                          HasReview = hs.Reviews.Any(r => r.order_id == o.order_id)
                                      })
                                 .OrderByDescending(o => o.Date) 
                        .ToList();

                return View(orders);
            }
        }
        private string GetUserAccountType(string userId)
        {
            
            if (int.TryParse(userId, out int userIntId)) 
            {
                using (var db = new Humaim_ShoppingEntities2())
                {
                    
                    var user = db.tbl_user.FirstOrDefault(u => u.u_id == userIntId); 
                    if (user != null)
                    {
                        return user.u_accountType; 
                    }
                }
            }

            return ""; 
        }



        public ActionResult SignIn()
        {
            return RedirectToAction("Login"); 
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(tbl_user avm)
        {
            tbl_user ad = hs.tbl_user.SingleOrDefault(x => x.u_email == avm.u_email && x.u_password == avm.u_password);
            if (ad != null)
            {
                Session["u_id"] = ad.u_id.ToString(); 
                return RedirectToAction("Index", "User"); 
            }
            else
            {
                ViewBag.error = "Invalid username or password"; 
            }
            return View();
        }
        public ActionResult SignUp()
        {
            return View();
        }

        
        private static string generatedOtp;
        private static tbl_user pendingUser;

        
        [HttpPost]
        public ActionResult SignUp(tbl_user uvm, HttpPostedFileBase imgfile, string otp, string register, string verifyOtp)
        {
            if (register != null)
            {
                
                if (string.IsNullOrEmpty(uvm.u_email) || string.IsNullOrEmpty(uvm.u_name) || string.IsNullOrEmpty(uvm.u_password))
                {
                    ViewBag.error = "Please fill out all the details!";
                    return View(uvm);
                }

              
                SendOtp(uvm.u_email);

              
                pendingUser = uvm;

                
                ViewBag.error = "OTP has been sent to your email. Please enter the OTP to complete registration.";
                return View(uvm);
            }

            if (verifyOtp != null)
            {
                
                if (otp != generatedOtp)
                {
                    ViewBag.OtpError = "Invalid OTP. Please try again.";
                    return View(uvm);
                }

                
                string path = UploadimgFile(imgfile);
                if (path.Equals("-1"))
                {
                    ViewBag.error = "Image upload failed. Please ensure it's a valid image file.";
                    return View(uvm);
                }

                if (string.IsNullOrEmpty(path))
                {
                    path = "~/UploadedFiles/default.png";
                }

                try
                {
                    
                    if (hs.tbl_user.Any(x => x.u_email == pendingUser.u_email))
                    {
                        ModelState.AddModelError("u_email", "Email already exists.");
                        return View(uvm);
                    }

                   
                    tbl_user newUser = new tbl_user
                    {
                        u_name = pendingUser.u_name,
                        u_email = pendingUser.u_email,
                        u_password = pendingUser.u_password,
                        u_image = path,
                        u_contact = pendingUser.u_contact,
                        u_city = pendingUser.u_city,
                        u_accountType = pendingUser.u_accountType
                    };

                    hs.tbl_user.Add(newUser);
                    hs.SaveChanges();

                    
                    pendingUser = null;

                    return RedirectToAction("SignIn"); 
                }
                catch (Exception ex)
                {
                    ViewBag.error = "An error occurred: " + ex.Message;
                    return View(uvm);
                }
            }

            return View();
        }
       
        private void SendOtp(string email)
        {
            Random rand = new Random();
            generatedOtp = rand.Next(100000, 999999).ToString();

            string from = "humasulimana@gmail.com";
            string pass = "vozy wvnr rbrf dwfy"; 
            string subject = "Email Verification OTP";
            string messageBody = "Your OTP for email verification is: " + generatedOtp;

            MailMessage message = new MailMessage();
            message.To.Add(email);
            message.From = new MailAddress(from);
            message.Subject = subject;
            message.Body = messageBody;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential(from, pass);
            smtp.EnableSsl = true;

            try
            {
                smtp.Send(message);
                ViewBag.error = "OTP has been sent to your email. Please verify it.";
            }
            catch (Exception ex)
            {
                ViewBag.error = "Failed to send OTP: " + ex.Message;
            }
        }
        public ActionResult Ads(int? id, int? page)
        {
            var category = hs.tbl_category.FirstOrDefault(x => x.cat_id == id);
            
            var list = hs.tbl_product.Where(x => x.pro_fk_category == id)
                                     .OrderByDescending(x => x.pro_id)
                                     .ToList();

            
            int pageSize = 12;  
            int pageIndex = page.HasValue ? Convert.ToInt32(page) : 1;

            
            IPagedList<tbl_product> pagedList = list.ToPagedList(pageIndex, pageSize);

            ViewBag.CategoryId = id; 
            ViewBag.ShowVideo = id ?? 0;
            ViewBag.CategoryName = category?.cat_name;
            
            return View(pagedList);
        }

        [HttpPost]
        public ActionResult Ads(int? id, int? page, string search)
        {
            int pagesize = 12; 
            int pageindex = page.HasValue ? Convert.ToInt32(page) : 1;

            var list = hs.tbl_product
                         .Where(x => (id == null || x.pro_fk_category == id) && x.pro_name.Contains(search))
                         .OrderByDescending(x => x.pro_id)
                         .ToList();

            IPagedList<tbl_product> stu = list.ToPagedList(pageindex, pagesize);
            ViewBag.CurrentCategoryId = id; 
                                           

            return View(stu);
        }

        public string UploadimgFile(HttpPostedFileBase imgfile)
        {
           
            if (imgfile == null || imgfile.ContentLength == 0)
            {
                return string.Empty; 
            }

            try
            {
                
                string extension = Path.GetExtension(imgfile.FileName);

                
                if (extension.ToLower() != ".jpg" && extension.ToLower() != ".png" && extension.ToLower() != ".jpeg")
                {
                    return "-1"; 
                }

                
                string filename = Path.GetFileNameWithoutExtension(imgfile.FileName) + "_" + DateTime.Now.Ticks + extension;
                string path = Path.Combine(Server.MapPath("~/UploadedFiles"), filename);

                
                if (!Directory.Exists(Server.MapPath("~/UploadedFiles")))
                {
                    Directory.CreateDirectory(Server.MapPath("~/UploadedFiles"));
                }

               
                imgfile.SaveAs(path);

                
                return "~/UploadedFiles/" + filename;
            }
            catch (Exception)
            {
                return "-1"; 
            }
        }
        [HttpGet]
        public ActionResult CreateAd()
        {
            if (Session["u_id"] == null) 
            {
                ViewBag.Message = "Please log in first to post an ad.";
                return RedirectToAction("Login");
            }
            List<tbl_category> li = hs.tbl_category.Where(x => x.cat_status).ToList();
            ViewBag.categorylist = new SelectList(li, "cat_id", "cat_name");
            return View();
        }

        [HttpPost]
        public ActionResult CreateAd(tbl_product pvm, HttpPostedFileBase imgfile)
        {
            string path = UploadimgFile(imgfile);
            if (path.Equals("-1"))
            {
                ViewBag.error = "Image could not be uploaded";
            }
            else
            {
                tbl_product p = new tbl_product
                {
                    pro_name = pvm.pro_name,
                    pro_price = pvm.pro_price,
                    pro_image = path,
                    pro_fk_category = pvm.pro_fk_category,
                    pro_des = pvm.pro_des,
                    pro_fk_user = Convert.ToInt32(Session["u_id"].ToString()),
                    total_availables = pvm.total_availables
                };
                hs.tbl_product.Add(p);
                hs.SaveChanges();
                return RedirectToAction("Index"); 
            }
            return View(); 
        }
        public ActionResult ViewAd(int id)
        {
            
            var adDetails = hs.tbl_product
     .Where(p => p.pro_id == id)
     .Select(p => new Adviewmodel
     {
         pro_id = p.pro_id,
         pro_name = p.pro_name,
         pro_image = p.pro_image,
         pro_des = p.pro_des,
         pro_price = p.pro_price,
         total_availables = p.total_availables, 
         cat_name = p.tbl_category.cat_name,
         u_name = p.tbl_user.u_name,
         u_image = p.tbl_user.u_image,
         u_email = p.tbl_user.u_email,  
         u_contact = p.tbl_user.u_contact,  
         pro_fk_user = p.pro_fk_user
     })
     .FirstOrDefault();


            
            var reviews = hs.Reviews
     .Where(r => r.product_id == id) 
     .Include(r => r.Order)
     .ToList() 
     .Select(r => new ReviewViewModel
     {
         review_id = r.review_id,
         product_id = r.product_id,
         user_id = r.buyer_id, 
         review_message = r.review_message,
         rating = r.rating,
         ReviewerName = r.tbl_user.u_name 
     })
     .ToList();



            
            var viewModel = new Adviewmodel
            {
                pro_id = adDetails.pro_id,
                pro_name = adDetails.pro_name,
                pro_image = adDetails.pro_image,
                pro_des = adDetails.pro_des,
                pro_price = adDetails.pro_price,
                total_availables = adDetails.total_availables,
                cat_name = adDetails.cat_name,
                u_name = adDetails.u_name,
                u_image = adDetails.u_image,
                u_email = adDetails.u_email,
                u_contact = adDetails.u_contact,
                pro_fk_user = adDetails.pro_fk_user,
                Reviews = reviews 
            };

            return View(viewModel);
        }

        public ActionResult Logout()
        {
            Session.RemoveAll();
            Session.Abandon();
            return RedirectToAction("Index");
        }
        public ActionResult AdsByCategory(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

          
            var products = hs.tbl_product.Where(x => x.pro_fk_category == id).ToList();

            if (products == null || products.Count == 0)
            {
                ViewBag.Message = "No products found in this category.";
            }

            ViewBag.CategoryName = hs.tbl_category.Where(x => x.cat_id == id).Select(x => x.cat_name).FirstOrDefault();

            return View(products);
        }


        [HttpPost]
        public ActionResult DeleteAd(int id)
        {
            
            var ad = hs.tbl_product.Find(id);
            if (ad != null)
            {
                
                hs.tbl_product.Remove(ad);
                hs.SaveChanges();

                
                return RedirectToAction("Index");
            }

            
            return HttpNotFound();
        }
        [HttpGet]
        public ActionResult MyPosts(int? page)
        {
            string userId = Session["u_id"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            if (int.TryParse(userId, out int userIdInt))
            {
                var posts = hs.tbl_product.Where(p => p.pro_fk_user == userIdInt).ToList();

                int pageSize = 12;
                int pageNumber = (page ?? 1);
                var pagedList = posts.ToPagedList(pageNumber, pageSize);

                return View(pagedList);
            }
            else
            {
                return HttpNotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public ActionResult DeletePost(int productId)
        {
            Console.WriteLine($"DeletePost called with productId: {productId}");

            try
            {
               
                var orders = hs.Orders.Where(o => o.product_id == productId).ToList();
                hs.Orders.RemoveRange(orders); 

                
                var product = hs.tbl_product.Find(productId);
                if (product != null)
                {
                    hs.tbl_product.Remove(product);
                    hs.SaveChanges(); 
                }
            }
            catch (DbUpdateException ex) 
            {
               
                var innerException = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(innerException);
            }
            catch (Exception ex)
            {
                
                Console.WriteLine(ex.Message); 
            }

            return RedirectToAction("MyPosts"); 
        }
        [HttpPost]
        public ActionResult PlaceOrder(int id)
        {
            if (Session["u_id"] == null)
            {
                
                return Json(new { success = false, message = "You need to sign in or sign up to place an order." });
            }

            var product = hs.tbl_product.Find(id);
            if (product == null || product.total_availables <= 0)
            {
                return Json(new { success = false, message = "Product is not available." });
            }

            
            int buyerId = Convert.ToInt32(Session["u_id"]);

           
            var order = new Order
            {
                seller_id = product.pro_fk_user.GetValueOrDefault(), 
                buyer_id = buyerId, 
                product_id = product.pro_id,
                category_id = product.pro_fk_category.GetValueOrDefault(),
                status = "Pending",
                Date = DateTime.Now
            };

            hs.Orders.Add(order);
            product.total_availables -= 1;

           
            if (product.total_availables == 0)
            {
                var saledOut = new Saled_out
                {
                    product_id = product.pro_id,
                    sold_at = DateTime.Now,
                    seller_id = product.pro_fk_user.GetValueOrDefault(),
                   

                };
                hs.Saled_out.Add(saledOut);
            }

            hs.SaveChanges();

            var buyer = hs.tbl_user.Find(buyerId);
            var seller = hs.tbl_user.Find(product.pro_fk_user);

          
            BuyerEmail(buyer.u_email, product, order);
            SellerEmail(seller.u_email, product, order);

            return Json(new { success = true, message = "Order placed successfully." });
        }

        private void SellerEmail(string sellerEmail, tbl_product product, Order order)
        {
           
            string buyerName = hs.tbl_user.Find(order.buyer_id)?.u_name ?? "Unknown";
            string buyerEmail = hs.tbl_user.Find(order.buyer_id)?.u_email ?? "Not provided";
            string buyerContact = hs.tbl_user.Find(order.buyer_id)?.u_contact ?? "Not provided";
            string buyerCity = hs.tbl_user.Find(order.buyer_id)?.u_city ?? "Not provided";

          
            string subject = "Order Notification";
            string body = $@"
Dear Seller,

You have received a new order today: {DateTime.Now:dd-MM-yyyy}.

Here are the details of the order:
- Product Name: {product.pro_name}
- Product Description: {product.pro_des}
- Product Price: {product.pro_price:C}
- Buyer Name: {buyerName}
- Buyer Email: {buyerEmail}
- Buyer Contact: {buyerContact}
- Buyer City: {buyerCity}
- Order ID: {order.order_id}

Please prepare to fulfill this order.

Regards,
E-Shopping Website";

           
            string from = "humasulimana@gmail.com"; 
            string pass = "vozy wvnr rbrf dwfy";   

            try
            {
                
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    mail.To.Add(sellerEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                   
                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(from, pass);
                        smtp.EnableSsl = true;

                        
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }
        private void BuyerEmail(string buyerEmail, tbl_product product, Order order)
        {
           
            string sellerName = hs.tbl_user.Find(product.pro_fk_user)?.u_name ?? "Unknown";
            string sellerEmail = hs.tbl_user.Find(product.pro_fk_user)?.u_email ?? "Not provided";
            string sellerContact = hs.tbl_user.Find(product.pro_fk_user)?.u_contact ?? "Not provided";
            string sellerCity = hs.tbl_user.Find(product.pro_fk_user)?.u_city ?? "Not provided";

            
            string subject = "Order Confirmation";
            string body = $@"
Dear Customer,

Your order has been placed successfully today: {DateTime.Now:dd-MM-yyyy}.

Here are the details of your order:
- Seller Name: {sellerName}
- Seller Email: {sellerEmail}
- Seller Contact: {sellerContact}
- Seller City: {sellerCity}
- Order ID: {order.order_id}
- Product ID: {product.pro_id}
- Product Name: {product.pro_name}
- Product Description: {product.pro_des}

Thank you for shopping with us!

Regards,
E-Shopping Website";

           
            string from = "humasulimana@gmail.com"; 
            string pass = "vozy wvnr rbrf dwfy";    

            try
            {
                
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    mail.To.Add(buyerEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    
                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(from, pass);
                        smtp.EnableSsl = true;

                       
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }
        public ActionResult OrdersReceived()
        {
            string sellerIdString = Session["u_id"] as string;
            

            

            if (string.IsNullOrEmpty(sellerIdString) || !int.TryParse(sellerIdString, out int sellerId))
            {
               
                return RedirectToAction("Login", "User");
            }

            using (var context = new Humaim_ShoppingEntities2()) 
            {
               
                var orders = (from order in context.Orders
                              join product in context.tbl_product on order.product_id equals product.pro_id
                              join buyer in context.tbl_user on order.buyer_id equals buyer.u_id
                              where order.seller_id == sellerId
                              orderby order.Date descending
                              select new OrdersReceivedViewModel
                              {
                                  ProductName = product.pro_name,
                                  ProductDescription = product.pro_des,
                                  
                                  ProductPrice = (product.pro_price > 0) ? product.pro_price : 0, 

                                  ProductImage = product.pro_image,
                                  BuyerName = buyer.u_name,
                                  BuyerEmail = buyer.u_email,
                                  BuyerContact = buyer.u_contact,
                                  BuyerImage = buyer.u_image,
                                  OrderDate = order.Date ?? DateTime.Now,
                                  OrderId = order.order_id,
                                  ProductId = product.pro_id
                              }).ToList();


                foreach (var order in orders)
                {
                    var review = context.Reviews
                        .FirstOrDefault(r => r.product_id == order.ProductId && r.order_id == order.OrderId);

                    
                    order.ReviewStatus = review != null ? "See Review" : "Not Reviewed";
                }


               
                return View(orders);
            }
        }

        public ActionResult ProductsReview(int orderId)
        {
            
            var order = hs.Orders
            .Where(o => o.order_id == orderId)
            .Select(o => new OrderViewModel
            {
                ProId = o.product_id,
                ProName = o.tbl_product.pro_name, 
                ProDes = o.tbl_product.pro_des,    
                ProPrice = o.tbl_product.pro_price,  
                ProImage = o.tbl_product.pro_image, 
                SellerId = o.seller_id,
                SellerName = o.tbl_user.u_name,
                BuyerId = o.buyer_id,
                BuyerName = o.tbl_user1.u_name,
                OrderId = o.order_id
            })
            .FirstOrDefault();


            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateInput(false)]
        [AllowAnonymous]
        public ActionResult SubmitReview(int seller_id, int buyer_id, int product_id, int order_id, string reviewMessage, int rating)
        {
            if (string.IsNullOrEmpty(reviewMessage))
            {
                TempData["ErrorMessage"] = "Review cannot be empty.";
                return RedirectToAction("ProductsReview", new { orderId = order_id });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Rating must be between 1 and 5.";
                return RedirectToAction("ProductsReview", new { orderId = order_id });
            }

            
            reviewMessage = RemoveHtmlTags(reviewMessage);

           
            var review = new Review
            {
                seller_id = seller_id,
                buyer_id = buyer_id,
                product_id = product_id,
                review_message = reviewMessage,
                rating = rating,  
                order_id = order_id
            };

            hs.Reviews.Add(review);
            hs.SaveChanges();

            TempData["SuccessMessage"] = "Review submitted successfully.";
            return RedirectToAction("MyOrders");  
        }

        private string RemoveHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

           
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }

        [HttpGet]
        public ActionResult SeeReview(int orderId)
        {
            if (Session["u_id"] == null)
            {
                return View("Login");
            }
            else
            {
                var reviewData = hs.Reviews
                    .Where(r => r.order_id == orderId)
                    .Join(hs.Orders,
                        r => r.order_id,
                        o => o.order_id,
                        (r, o) => new { r, o })
                    .Join(hs.tbl_product,
                        ro => ro.o.product_id,
                        p => p.pro_id,
                        (ro, p) => new { ro, p })
                    .Join(hs.tbl_user,
                        rop => rop.ro.o.seller_id,
                        s => s.u_id,
                        (rop, s) => new { rop, Seller = s })
                    .Join(hs.tbl_user,
                        rops => rops.rop.ro.o.buyer_id,
                        b => b.u_id,
                        (rops, Buyer) => new SeeReviewViewModel
                        {
                            SellerName = rops.Seller.u_name,
                            SellerContact = rops.Seller.u_contact,
                            BuyerName = Buyer.u_name,
                            BuyerContact = Buyer.u_contact,
                            ProductName = rops.rop.p.pro_name,
                            ProductDescription = rops.rop.p.pro_des,
                            ProductPrice = rops.rop.p.pro_price.ToString(),
                            ProductImage = rops.rop.p.pro_image,
                            ReviewMessage = rops.rop.ro.r.review_message,
                            Rating = rops.rop.ro.r.rating 
                        })
                    .FirstOrDefault();

                if (reviewData == null)
                {
                    TempData["ErrorMessage"] = "Review not found for this order.";
                    return RedirectToAction("MyOrders");
                }

                return View(reviewData);
            }


        }
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
           
            var user = hs.tbl_user.FirstOrDefault(u => u.u_email == email);

            if (user != null)
            {
                
                string resetLink = Url.Action("ResettingPassword", "User", new { email = email }, protocol: Request.Url.Scheme);

                
                SendPasswordResetEmail(email, resetLink); 

                ViewBag.Message = "Password reset instructions have been sent to your email.";
            }
            else
            {
                ViewBag.ErrorMessage = "The email address is not registered.";
            }

            return View();
        }



        private void SendResetEmail(tbl_user user)
        {
            
            string resetLink = Url.Action("ResettingPassword", "Account", new { token = user.ResetToken }, Request.Url.Scheme);

          
            var smtpClient = new SmtpClient("smtp.yourserver.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("your-email@example.com"),
                Subject = "Password Reset Request",
                Body = $"Click the following link to reset your password: {resetLink}",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(user.u_email);
            smtpClient.Send(mailMessage);
        }
        [HttpGet]
        public ActionResult ResettingPassword(string email)
        {
           
            var user = hs.tbl_user.FirstOrDefault(u => u.u_email == email);

            if (user != null)
            {
                return View(); 
            }
            else
            {
                
                ViewBag.ErrorMessage = "Invalid email address.";
                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult ResettingPassword(string email, string newPassword, string confirmPassword)
        {
            
            var user = hs.tbl_user.FirstOrDefault(u => u.u_email == email);

            if (user != null)
            {
                if (newPassword == confirmPassword)
                {
                   
                    user.u_password = newPassword;  
                    hs.SaveChanges();  

                    ViewBag.SuccessMessage = "Your password has been updated successfully.";
                    return RedirectToAction("Login"); 
                }
                else
                {
                    
                    ViewBag.ErrorMessage = "Passwords do not match.";
                    return View();
                }
            }
            else
            {
                
                ViewBag.ErrorMessage = "Invalid email address.";
                return View("Error");
            }
        }


      
        private void SendPasswordResetEmail(string email, string resetLink)
        {
            string subject = "Password Reset Instructions";
            string body = $@"
Dear User,

To reset your password, please click on the following link:
{resetLink}

If you did not request a password reset, please ignore this email.

Best regards,
E-Shopping Website";

            
            string from = "humasulimana@gmail.com";  
            string pass = "vozy wvnr rbrf dwfy"; 

            try
            {
              
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    mail.To.Add(email);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                   
                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(from, pass);
                        smtp.EnableSsl = true;

                       
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }

    }

}

