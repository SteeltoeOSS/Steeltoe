using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
 
    public class HomeController : Controller
    {
        private ConfigServerOptions _options;
        public HomeController(IOptions<ConfigServerOptions> options)
        {
            _options = options.Value;
        }
        [HttpGet]
        public string VerifyAsInjectedOptions()
        {
            return _options.Bar + _options.Foo + _options.Info.Description + _options.Info.Url;
        }

    }
}
