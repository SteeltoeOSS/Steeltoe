using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;
using Simple.Model;

namespace Simple.Controllers
{
    public class HomeController : Controller
    {
        private ConfigServerData ConfigServerData { get; set; }

        public HomeController(IOptions<ConfigServerData> configServerData)
        {
            // The ASP.NET DI mechanism injects the data retrieved from the Spring Cloud Config Server 
            // as an IOptions<ConfigServerData>. This happens because we added the call to:
            // "services.Configure<ConfigServerData>(Configuration);" in the StartUp class
            if (configServerData != null)
                ConfigServerData = configServerData.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult ConfigServer()
        {
            // ConfigServerData property is set to a ConfigServerData POCO that has been
            // initialized with the configuration data returned from the Spring Cloud Config Server
            if (ConfigServerData != null)
            {
                ViewData["Bar"] = ConfigServerData.Bar ?? "Not returned";
                ViewData["Foo"] = ConfigServerData.Foo ?? "Not returned";

                ViewData["Info.Url"] =  "Not returned";
                ViewData["Info.Description"] = "Not returned";

                if (ConfigServerData.Info != null)
                {
                    ViewData["Info.Url"] = ConfigServerData.Info.Url ?? "Not returned";
                    ViewData["Info.Description"] = ConfigServerData.Info.Description ?? "Not returned";
                } 
            }
            else {
                ViewData["Bar"] = "Not Available";
                ViewData["Foo"] = "Not Available";
                ViewData["Info.Url"] = "Not Available";
                ViewData["Info.Description"] = "Not Available";
            }

            return View();
        }
    }
}
