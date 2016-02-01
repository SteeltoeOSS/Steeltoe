using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;
using Simple.Model;
using Spring.Extensions.Configuration.Server;

namespace Simple.Controllers
{
    public class HomeController : Controller
    {
        private ConfigServerData ConfigServerData { get; set; }

        private ConfigServerClientSettingsOptions ConfigServerClientSettingsOptions { get; set; }

        public HomeController(IOptions<ConfigServerData> configServerData, IOptions<ConfigServerClientSettingsOptions> confgServerSettings)
        {
            // The ASP.NET DI mechanism injects the data retrieved from the Spring Cloud Config Server 
            // as an IOptions<ConfigServerData>. This happens because we added the call to:
            // "services.Configure<ConfigServerData>(Configuration);" in the StartUp class
            if (configServerData != null)
                ConfigServerData = configServerData.Value;

            // Inject the settings used in communicating with the Spring Cloud Config Server
            if (confgServerSettings != null)
                ConfigServerClientSettingsOptions = confgServerSettings.Value;
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

        public IActionResult ConfigServerSettings()
        {
            if (ConfigServerClientSettingsOptions != null)
            {
                ViewData["Enabled"] = ConfigServerClientSettingsOptions.Enabled;
                ViewData["Environment"] = ConfigServerClientSettingsOptions.Environment;
                ViewData["FailFast"] = ConfigServerClientSettingsOptions.FailFast;
                ViewData["Label"] = ConfigServerClientSettingsOptions.Label;
                ViewData["Name"] = ConfigServerClientSettingsOptions.Name;
                ViewData["Password"] = ConfigServerClientSettingsOptions.Password;
                ViewData["Uri"] = ConfigServerClientSettingsOptions.Uri;
                ViewData["Username"] = ConfigServerClientSettingsOptions.Username;
                ViewData["ValidateCertificates"] = ConfigServerClientSettingsOptions.ValidateCertificates;
            } else
            {

                ViewData["Enabled"] = "Not Available";
                ViewData["Environment"] = "Not Available";
                ViewData["FailFast"] = "Not Available";
                ViewData["Label"] = "Not Available";
                ViewData["Name"] = "Not Available";
                ViewData["Password"] = "Not Available";
                ViewData["Uri"] = "Not Available";
                ViewData["Username"] = "Not Available";
                ViewData["ValidateCertificates"] = "Not Available";
            }
            return View();
        }
    }
}
