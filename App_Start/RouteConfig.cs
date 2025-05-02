using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace E_Shopping_Website
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                
                defaults: new { controller = "User", action = "Index", id = UrlParameter.Optional }
                
                );

            routes.MapRoute(
                name: "AdminLogin",
                url: "Admin/Login",
                defaults: new { controller = "Admin", action = "Login" }
            );
        }
    }
}