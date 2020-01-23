using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cb = new ConfigurationBuilder();
            cb.AddPemFiles(@"c:\temp\container_certchain.txt", @"c:\temp\cert.key");
            var c = cb.Build();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
            services.Configure<CertificateOptions>(c);
            services.AddSingleton<IConfiguration>(c);
            var container = services.BuildServiceProvider();
            var cert = c["certificate"];
            var options = container.GetService<IOptionsSnapshot<CertificateOptions>>();
            var x509 = options.Value.Certificate;
            var bytes = x509.Export(X509ContentType.Cert);
            var b64 = Convert.ToBase64String(bytes);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-Client-Cert", cert);
            await client.GetAsync("http://localhost/");
        }
    }
}
