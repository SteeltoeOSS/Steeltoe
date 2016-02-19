using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simple4.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult ConfigServerSettings()
        {
            var config = ServerConfig.Configuration;
            var section = config.GetSection("spring:cloud:config");

            if (section != null)
            {
                ViewBag.Enabled = section["enabled"];
                ViewBag.Environment = section["env"];
                ViewBag.FailFast = section["failFast"];
                ViewBag.Label = section["label"];
                ViewBag.Name = section["name"];
                ViewBag.Password = section["password"];
                ViewBag.Uri = section["uri"];
                ViewBag.Username = section["username"];
                ViewBag.ValidateCertificates = section["validate_certificates"];
            }
            else
            {

                ViewBag.Enabled = "Not Available";
                ViewBag.Environment = "Not Available";
                ViewBag.FailFast = "Not Available";
                ViewBag.Label = "Not Available";
                ViewBag.Name = "Not Available";
                ViewBag.Password = "Not Available";
                ViewBag.Uri = "Not Available";
                ViewBag.Username = "Not Available";
                ViewBag.ValidateCertificates = "Not Available";
            }
            return View();
        }
        public ActionResult ConfigServerData()
        {

            var config = ServerConfig.Configuration;
            if (config != null)
            {
                ViewBag.Bar = config["bar"] ?? "Not returned";
                ViewBag.Foo = config["foo"] ?? "Not returned";

                ViewBag.Info_Url = config["info:url"] ?? "Not returned";
                ViewBag.Info_Description = config["info:description"] ?? "Not returned";

            }

            return View();
        }
    }
}