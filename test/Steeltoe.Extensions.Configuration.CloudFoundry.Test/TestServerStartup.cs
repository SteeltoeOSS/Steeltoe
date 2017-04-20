using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class TestServerStartup
    {
        public List<string> ExpectedAddresses = new List<string>();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            var addresses = ExpectedAddresses.DefaultIfEmpty("http://localhost:5000");
            Assert.Equal(addresses, app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses);
            app.UseMvc();
        }
    }
}
