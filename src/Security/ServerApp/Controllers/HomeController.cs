using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerApp.Models;
using Steeltoe.Common.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ServerApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly X509Certificate2 cert;

        public HomeController(ILogger<HomeController> logger, IOptionsMonitor<CertificateOptions> optionsMonitor)
        {
            _logger = logger;
            cert = optionsMonitor.CurrentValue.Certificate;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(policy: "sameorg")]
        public IActionResult SecurityCheck()
        {
            return Json(true);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult GetInstanceCert()
        {
            return Json(cert.GetRawCertDataString());
        }

        public IActionResult GetInstanceKey()
        {
            var file = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");
            _logger.LogWarning("Attempting to access file {file}", file);
            var stream = new FileStream(file, FileMode.Open);
            return File(stream, "application/octet-stream", Path.GetFileName(file));

        }

        public string PemFromCert(X509Certificate2 certificate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }
    }
}
