
using Microsoft.AspNet.Mvc;
using SimpleCloudFoundry.Model;
using Microsoft.Extensions.OptionsModel;
using Spring.Extensions.Configuration.CloudFoundry;
using SimpleCloudFoundry.ViewModels.Home;
using Spring.Extensions.Configuration.Server;

namespace SimpleCloudFoundry.Controllers
{
    public class HomeController : Controller
    {

        private ConfigServerData ConfigServerData { get; set; }
        private CloudFoundryServicesOptions CloudFoundryServices { get; set; }
        private CloudFoundryApplicationOptions CloudFoundryApplication { get; set; }
        private ConfigServerClientSettingsOptions ConfigServerClientSettingsOptions { get; set; }

        public HomeController(IOptions<ConfigServerData> configServerData, 
            IOptions<CloudFoundryApplicationOptions> appOptions, 
            IOptions<CloudFoundryServicesOptions> servOptions,
            IOptions<ConfigServerClientSettingsOptions> confgServerSettings)
        {
            // The ASP.NET DI mechanism injects the data retrieved from the
            // Spring Cloud Config Server as an IOptions<ConfigServerData>
            // since we added "services.Configure<ConfigServerData>(Configuration);"
            // in the StartUp class
            if (configServerData != null)
                ConfigServerData = configServerData.Value;

            // The ASP.NET DI mechanism injects these as well, see
            // public void ConfigureServices(IServiceCollection services) in Startup class
            if (servOptions != null)
                CloudFoundryServices = servOptions.Value;
            if (appOptions != null)
                CloudFoundryApplication = appOptions.Value;

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
            // TODO:
            return View();
        }
        public IActionResult ConfigServerSettings()
        {
            if (ConfigServerClientSettingsOptions != null)
            {
                ViewData["AccessTokenUri"] = ConfigServerClientSettingsOptions.AccessTokenUri;
                ViewData["ClientId"] = ConfigServerClientSettingsOptions.ClientId;
                ViewData["ClientSecret"] = ConfigServerClientSettingsOptions.ClientSecret;
                ViewData["Enabled"] = ConfigServerClientSettingsOptions.Enabled;
                ViewData["Environment"] = ConfigServerClientSettingsOptions.Environment;
                ViewData["FailFast"] = ConfigServerClientSettingsOptions.FailFast;
                ViewData["Label"] = ConfigServerClientSettingsOptions.Label;
                ViewData["Name"] = ConfigServerClientSettingsOptions.Name;
                ViewData["Password"] = ConfigServerClientSettingsOptions.Password;
                ViewData["Uri"] = ConfigServerClientSettingsOptions.Uri;
                ViewData["Username"] = ConfigServerClientSettingsOptions.Username;
                ViewData["ValidateCertificates"] = ConfigServerClientSettingsOptions.ValidateCertificates;
            }
            else
            {
                ViewData["AccessTokenUri"] = "Not Available";
                ViewData["ClientId"] = "Not Available";
                ViewData["ClientSecret"] = "Not Available";
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
        public IActionResult CloudFoundry()
        {
            return View(new CloudFoundryViewModel(
                CloudFoundryApplication == null ? new CloudFoundryApplicationOptions() : CloudFoundryApplication,
                CloudFoundryServices == null ? new CloudFoundryServicesOptions() : CloudFoundryServices));
        }

    }
}
