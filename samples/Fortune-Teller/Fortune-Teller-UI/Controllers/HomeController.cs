using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Fortune_Teller_UI.Services;

namespace Fortune_Teller_UI.Controllers
{
    [Route("/")]
    public class HomeController : Controller
    {
        IFortuneService _fortunes;

        public HomeController(IFortuneService fortunes)
        {
            _fortunes = fortunes;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("random")]
        public async Task<string> Random()
        {
            return await _fortunes.RandomFortuneAsync();
        }

    }
}
