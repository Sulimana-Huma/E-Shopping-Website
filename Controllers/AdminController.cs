using E_Shopping_Website.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Collections.Generic;
using System.Data.Entity;  

namespace E_Shopping_Website.Controllers
{
    public class AdminController : Controller
    {
        private Humaim_ShoppingEntities2 hs = new Humaim_ShoppingEntities2();

        
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        
        [HttpPost]
        public ActionResult Login(tbl_admin avm)
        {
            
            var ad = hs.tbl_admin.SingleOrDefault(x => x.ad_username == avm.ad_username && x.ad_password == avm.ad_password);
            if (ad != null)
            {
                if (ad.IsAccessGranted==true)
                {
                    // If access is granted, proceed with the login
                    Session["ad_id"] = ad.ad_id.ToString();
                    return RedirectToAction("ViewCategoryy");
                }
                else
                {
                    // If access is not granted, show an error message
                    ViewBag.error = "Access Denied. Please contact the administrator.";
                }
            }
            else
            {
                ViewBag.error = "Invalid Username or password"; 
            }
            return View(); 
        }


        [HttpPost]
        public ActionResult AddAdmin(string Username, string Password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                TempData["Error"] = "Username and Password are required!";
                return RedirectToAction("ViewAdmins");
            }

          
            if (hs.tbl_admin.Any(a => a.ad_username == Username))
            {
                TempData["Error"] = "This username already exists!";
                return RedirectToAction("ViewAdmins");
            }

            
            var newAdmin = new tbl_admin
            {
                ad_username = Username,
                ad_password = Password
            };

            hs.tbl_admin.Add(newAdmin);
            hs.SaveChanges();

            TempData["Success"] = "New admin added successfully!";
            return RedirectToAction("ViewAdmins");
        }


        public ActionResult ViewAdmins()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login");
            }

            int currentAdminId = Convert.ToInt32(Session["ad_id"]);

            var admins = hs.tbl_admin
                           .Where(a => a.ad_id != currentAdminId)
                           .Select(a => new AdminViewModel
                           {
                               AdminId = a.ad_id,
                               Username = a.ad_username,
                               Password = a.ad_password,
                               Categories = hs.tbl_category
                                              .Where(c => c.cat_fk_ad == a.ad_id)
                                              .Select(c => c.cat_name)
                                              .ToList(),
                               // Check if nullable bool is null and assign a default value
                               IsAccessGranted = a.IsAccessGranted.HasValue ? a.IsAccessGranted.Value : false
                           })
                           .ToList();

            ViewBag.CurrentAdminId = currentAdminId;
            return View(admins);
        }





        [HttpPost]
        public ActionResult DeleteAdmin(int adminId)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login"); 
            }

            var admin = hs.tbl_admin.SingleOrDefault(a => a.ad_id == adminId);
            if (admin != null)
            {
                hs.tbl_admin.Remove(admin);
                hs.SaveChanges();
            }

            return RedirectToAction("ViewAdmins"); 
        }

       
        [HttpPost]
        public ActionResult UpdateAdmin(int adminId, string username, string password)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login");
            }

            var admin = hs.tbl_admin.SingleOrDefault(a => a.ad_id == adminId);
            if (admin != null)
            {
                admin.ad_username = username;
                admin.ad_password = password;
                hs.SaveChanges(); 
            }

            return RedirectToAction("ViewAdmins"); 
        }










       
        public ActionResult ViewCategoryy(int? page)
        {

            
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.ad_id = Convert.ToInt32(Session["ad_id"]);
          

            
            var categories = hs.tbl_category
                                .Where(c => c.cat_status == true)
                                .OrderBy(c => c.cat_id)
                                .ToList();

           
            int pageNumber = (page ?? 1);
            int pageSize = 12; 

            var pagedCategories = categories.ToPagedList(pageNumber, pageSize);

           
            return View(pagedCategories);
        }


    
       
        private string SaveImage(HttpPostedFileBase image)
        {
            if (image != null && image.ContentLength > 0)
            {
                var fileName = Path.GetFileName(image.FileName);
                var path = Path.Combine(Server.MapPath("~/Content/SubCategoryImages"), fileName);
                image.SaveAs(path);
                return "~/Content/SubCategoryImages/" + fileName;
            }
            return null;
        }


       
        [HttpGet]
        public ActionResult Create()
        {
           
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login"); 
            }
            return View(); 
        }

       
        [HttpPost]
        public ActionResult Create(tbl_category cvm, HttpPostedFileBase imgfile)
        {
            if (ModelState.IsValid) 
            {
                string path = UploadingFile(imgfile); 
                if (path.Equals("-1"))
                {
                    return View(cvm);
                }

                try
                {
                    
                    tbl_category cat = new tbl_category
                    {
                        cat_name = cvm.cat_name,
                        cat_image = path,
                        cat_fk_ad = Convert.ToInt32(Session["ad_id"]),
                        cat_status = true
                    };

                    hs.tbl_category.Add(cat); 
                    hs.SaveChanges(); 
                    return RedirectToAction("ViewCategoryy"); 
                }
                catch (Exception ex)
                {
                    ViewBag.error = "Error saving category: " + ex.Message;
                }
            }
            return View(cvm); 
        }

       
        public string UploadingFile(HttpPostedFileBase file)
        {
            string path = "-1";
            if (file != null && file.ContentLength > 0) 
            {
                string extension = Path.GetExtension(file.FileName);
                if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png") 
                {
                    try
                    {
                        string random = Guid.NewGuid().ToString();
                        string fileName = random + Path.GetFileName(file.FileName);
                        string fullPath = Path.Combine(Server.MapPath("~/Content/upload"), fileName);
                        file.SaveAs(fullPath);
                        path = "~/Content/upload/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        path = "-1";
                        ViewBag.error = "Error while uploading file: " + ex.Message; 
                    }
                }
                else
                {
                    ViewBag.error = "Only jpg, jpeg or png formats are acceptable.";
                }
            }
            else
            {
                ViewBag.error = "Please select a file."; 
                path = "-1";
            }
            return path;
        }

        
        [HttpPost]
        public ActionResult DeleteProduct(int id, int categoryId)
        {
            
            var product = hs.tbl_product.Find(id);
            if (product != null)
            {
                hs.tbl_product.Remove(product); 
                hs.SaveChanges(); 
            }

          
            return RedirectToAction("AdsByCategory", new { categoryId = categoryId });
        }






        public ActionResult AdsByCategory(int categoryId, int? page)
        {
            
            var products = hs.tbl_product.Where(p => p.pro_fk_category == categoryId).ToList();

           
            int pageSize = 12; 
            int pageNumber = (page ?? 1);

            var pagedProducts = products.ToPagedList(pageNumber, pageSize);

            ViewBag.categoryId = categoryId; 
            return View(pagedProducts); 
        }




        public ActionResult UserDetails(string search, int page = 1, int pageSize = 24)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login"); 
            }
            var users = hs.tbl_user.AsQueryable();

            
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.u_name.Contains(search));
            }

            
            users = users.OrderBy(u => u.u_id);

           
            var totalUsers = users.Count();

           
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.CurrentPage = page;

            return View(pagedUsers);
        }


        [HttpPost]
        public ActionResult DeleteUser(int id)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login"); 
            }
           
            var user = hs.tbl_user.Find(id);

            if (user != null)
            {
                
                var products = hs.tbl_product.Where(p => p.pro_fk_user == user.u_id).ToList();
                foreach (var product in products)
                {
                    hs.tbl_product.Remove(product);
                }

              
                hs.tbl_user.Remove(user);
                hs.SaveChanges();

                
               
                return RedirectToAction("UserDetails","Admin");
            }

            return HttpNotFound("User not found.");
        }


       
        [HttpPost]
        public ActionResult Logout()
        {
            Session["ad_id"] = null;
            return RedirectToAction("Login"); 
        }


        
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login");
            }

          
            var category = hs.tbl_category.Find(id);
            if (category == null)
            {
                return HttpNotFound("Category not found."); 
            }

            try
            {
               
                hs.tbl_category.Remove(category);
                hs.SaveChanges();

                return RedirectToAction("ViewCategoryy"); 
            }
            catch (Exception ex)
            {
                ViewBag.error = "Error deleting category: " + ex.Message; 
                return View("ViewCategoryy");
            }
        }

        public ActionResult ViewDatabase()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Login"); 
            }
            return View();
        }


        [HttpGet]
        public ActionResult GetTableData(string table)
        {
            var data = "";

           
            switch (table)
            {
                case "Products":
                    var products = hs.tbl_product.ToList();
                    data = RenderProductTable(products);
                    break;
                case "Category":
                    var categories = hs.tbl_category.ToList();
                    data = RenderCategoryTable(categories);
                    break;
                case "User":
                    var users = hs.tbl_user.ToList();
                    data = RenderUserTable(users);
                    break;
                case "SaledOut":
                    var saledOut = hs.Saled_out
    .Include(s => s.tbl_product)   // Ensure the product is included
    .Include(s => s.tbl_user)      // Ensure the user is included
    .Where(s => s.product_id != null && s.seller_id != null)  // Ensure no null references
    .ToList();


                    data = RenderSaledOutTable(saledOut); 
                    break;

                case "Sellers":
                    var sellers = hs.tbl_user.Where(u => u.u_accountType == "Seller Account").ToList(); 
                    data = RenderSellersTable(sellers); 
                    break;

                case "Buyers":
                    var buyers = hs.tbl_user.Where(u => u.u_accountType == "Buyer Account").ToList(); 
                    data = RenderBuyersTable(buyers); 
                    break;
                   

                /*  case "Orders":
                      var orders = hs.Orders
                          .Include(o => o.tbl_user)    
                          .Include(o => o.tbl_user1)    
                          .Include(o => o.tbl_product)  
                          .Include(o => o.tbl_category)  
                          .ToList();

                      data = RenderOrdersTable(orders);
                      break;*/


                case "Admin":
                    var admins = hs.tbl_admin.ToList();
                    data = RenderAdminTable(admins);
                    break;
                default:
                    return Content("Invalid table name");
            }

            return Content(data);
        }

        private string RenderBuyersTable(IEnumerable<tbl_user> buyers)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>User ID</th><th>Name</th><th>Email</th><th>Contact</th><th>City</th></tr></thead><tbody>";

            foreach (var buyer in buyers)
            {
                htmlTable += $"<tr><td>{buyer.u_id}</td><td>{buyer.u_name}</td><td>{buyer.u_email}</td><td>{buyer.u_contact}</td><td>{buyer.u_city}</td></tr>";
            }

            htmlTable += "</tbody></table>";
            return htmlTable;
        }

        private string RenderSellersTable(IEnumerable<tbl_user> sellers)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>User ID</th><th>Name</th><th>Email</th><th>Contact</th><th>City</th></tr></thead><tbody>";

            foreach (var seller in sellers)
            {
                htmlTable += $"<tr><td>{seller.u_id}</td><td>{seller.u_name}</td><td>{seller.u_email}</td><td>{seller.u_contact}</td><td>{seller.u_city}</td></tr>";
            }

            htmlTable += "</tbody></table>";
            return htmlTable;
        }

        private string RenderProductTable(IEnumerable<tbl_product> products)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>Product ID</th><th>Product Name</th><th>Description</th><th>Price</th><th>Category</th><th>User</th></tr></thead><tbody>";
            foreach (var product in products)
            {
                htmlTable += $"<tr><td>{product.pro_id}</td><td>{product.pro_name}</td><td>{product.pro_des}</td><td>{product.pro_price}</td><td>{product.tbl_category.cat_name}</td><td>{product.tbl_user.u_name}</td></tr>";
            }
            htmlTable += "</tbody></table>";
            return htmlTable;
        }

        private string RenderCategoryTable(IEnumerable<tbl_category> categories)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>Category ID</th><th>Category Name</th><th>Admin</th><th>Status</th></tr></thead><tbody>";
            foreach (var category in categories)
            {
                htmlTable += $"<tr><td>{category.cat_id}</td><td>{category.cat_name}</td><td>{category.tbl_admin.ad_username}</td><td>{category.cat_status}</td></tr>";
            }
            htmlTable += "</tbody></table>";
            return htmlTable;
        }

        private string RenderUserTable(IEnumerable<tbl_user> users)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>User ID</th><th>User Name</th><th>Email</th><th>Account Type</th></tr></thead><tbody>";
            foreach (var user in users)
            {
                htmlTable += $"<tr><td>{user.u_id}</td><td>{user.u_name}</td><td>{user.u_email}</td><td>{user.u_accountType}</td></tr>";
            }
            htmlTable += "</tbody></table>";
            return htmlTable;
        }

        private string RenderSaledOutTable(IEnumerable<Saled_out> saledOut)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>Sale ID</th><th>Product Name</th><th>Seller</th><th>Sold At</th></tr></thead><tbody>";

            foreach (var sale in saledOut)
            {
                // Access the user information for the seller
                string sellerName = sale.tbl_user?.u_name ?? "Unknown";  // Seller Name

                // Access the user information for the buyer, if available

                // Formatting the sold date, ensuring it's not null
                string soldAt = sale.sold_at?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "Not Available";  // Display a default message if null

                htmlTable += $"<tr><td>{sale.SALED_id}</td><td>{sale.tbl_product?.pro_name ?? "No Product"}</td><td>{sellerName}</td><td>{soldAt}</td></tr>";
            }

            htmlTable += "</tbody></table>";
            return htmlTable;
        }

       


        /*private string RenderOrdersTable(IEnumerable<Order> orders)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>Order ID</th><th>Seller</th><th>Buyer</th><th>Product</th><th>Category</th><th>Status</th><th>Date</th></tr></thead><tbody>";
            foreach (var order in orders)
            {
                htmlTable += $"<tr><td>{order.order_id}</td><td>{order.tbl_user.u_name}</td><td>{order.tbl_user1.u_name}</td><td>{order.tbl_product.pro_name}</td><td>{order.tbl_category.cat_name}</td><td>{order.status}</td><td>{order.Date}</td></tr>";
            }
            htmlTable += "</tbody></table>";
            return htmlTable;
        }*/

        private string RenderAdminTable(IEnumerable<tbl_admin> admins)
        {
            string htmlTable = "<table class='table table-bordered'><thead><tr><th>Admin ID</th><th>Username</th></tr></thead><tbody>";
            foreach (var admin in admins)
            {
                htmlTable += $"<tr><td>{admin.ad_id}</td><td>{admin.ad_username}</td></tr>";
            }
            htmlTable += "</tbody></table>";
            return htmlTable;
        }

       


        [HttpPost]
        public  ActionResult ToggleAccess(int adminId)
        {
            try
            {
                using (var hs = new Humaim_ShoppingEntities2()) // Your DbContext
                {
                    // Fetch the admin record by ID
                    var admin = hs.tbl_admin.FirstOrDefault(a => a.ad_id == adminId);

                    if (admin != null)
                    {
                        // Toggle the access (change true to false or vice versa)
                        admin.IsAccessGranted = !admin.IsAccessGranted;

                        // Save changes to the database
                        hs.SaveChanges();

                        // Return success with the new access status
                        
                    }
                   
                }
            }
            catch (Exception ex)
            {
                // Log the error for server-side debugging
                Console.WriteLine("Error in ToggleAccess: " + ex.Message);
            }
            return RedirectToAction("ViewAdmins");
        }



    }
}
