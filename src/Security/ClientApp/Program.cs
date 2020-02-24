﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.CloudFoundry;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ClientApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (!Platform.IsCloudFoundry)
            {
                Console.WriteLine("Not running on the platform... using local certs");
                Environment.SetEnvironmentVariable("CF_INSTANCE_CERT", Path.Combine("Cert", "CF_INSTANCE_CERT.pem"));
                Environment.SetEnvironmentVariable("CF_INSTANCE_KEY", Path.Combine("Cert", "CF_INSTANCE_KEY.pem"));
            }
            else
            {
                Console.WriteLine("CF_INSTANCE_CERT: {0}", Environment.GetEnvironmentVariable("CF_INSTANCE_CERT"));
                Console.WriteLine("CF_INSTANCE_KEY: {0}", Environment.GetEnvironmentVariable("CF_INSTANCE_KEY"));
            }

            var config = new ConfigurationBuilder().AddCloudFoundryContainerIdentity().Build();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
            services.Configure<CertificateOptions>(config);
            services.AddSingleton<IConfiguration>(config);

            var container = services.BuildServiceProvider();

           // var cert = config["certificate"];
            var options = container.GetService<IOptionsSnapshot<CertificateOptions>>();
            var x509 = options.Value.Certificate;
            var bytes = x509.Export(X509ContentType.Cert);
            var b64 = Convert.ToBase64String(bytes);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-Client-Cert", b64);
            var exit = new ConsoleKeyInfo();
            while (exit.Key != ConsoleKey.Escape)
            {
                var response = await client.GetAsync("https://localhost:8081/Home/SecurityCheck");
                Console.WriteLine($"Response code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                exit = Console.ReadKey();
            }
        }
    }
}
