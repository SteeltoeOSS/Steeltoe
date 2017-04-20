using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public static class CloudFoundryHostBuilderExtensions
    {
        public static IWebHostBuilder UseCloudFoundryHosting(this IWebHostBuilder webHostBuilder)
        {
            if(webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            List<string> urls = new List<string>();

            string portStr = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(portStr))
            {
                int port;
                if (int.TryParse(portStr, out port))
                {
                    urls.Add($"http://*:{port}");
                }
            }

            if (urls.Count > 0)
            {
                webHostBuilder.UseUrls(urls.ToArray());
            }

            return webHostBuilder;
        }
    }
}
