using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Build;
using Steeltoe.Common.Hosting;
using Steeltoe.Security.Authentication.CloudFoundry;
using System;
using System.IO;

namespace ServerApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var task = new GenerateCertificates();
            task.Execute();

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

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    //webBuilder.ConfigureKestrel(o =>
                    //{
                    //    o.ConfigureHttpsDefaults(o => o.ClientCertificateMode = ClientCertificateMode.RequireCertificate);
                    //});
                })
                .UseCloudHosting(8080, 8081);
    }
}
