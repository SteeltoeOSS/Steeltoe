
using Microsoft.AspNet.Mvc;
using SimpleCloudFoundry.Model;
using Microsoft.Extensions.OptionsModel;
using Spring.Extensions.Configuration.CloudFoundry;
using SimpleCloudFoundry.ViewModels.Home;

namespace SimpleCloudFoundry.Controllers
{
    public class HomeController : Controller
    {

        private ConfigServerData ConfigServerData { get; set; }
        private CloudFoundryServicesOptions CloudFoundryServices { get; set; }
        private CloudFoundryApplicationOptions CloudFoundryApplication { get; set; }


        public HomeController(IOptions<ConfigServerData> configServerData, 
            IOptions<CloudFoundryApplicationOptions> appOptions, 
            IOptions<CloudFoundryServicesOptions> servOptions)
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
        public IActionResult CloudFoundry()
        {
            return View(new CloudFoundryViewModel(
                CloudFoundryApplication == null ? new CloudFoundryApplicationOptions() : CloudFoundryApplication,
                CloudFoundryServices == null ? new CloudFoundryServicesOptions() : CloudFoundryServices));
        }

    }
}
