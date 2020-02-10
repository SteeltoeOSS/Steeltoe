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
            StartupHelper.UseOrGeneratePlatformCertificates();
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
