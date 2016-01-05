using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spring.Extensions.Configuration.Server.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
    class TestServerStartup
    {
        public IConfiguration Configuration { get; set; }


        public TestServerStartup()
        {
            // These settings match the default java config server
            var appsettings = @"
{
    'spring': {
      'cloud': {
        'config': {
            'uri': 'http://localhost:8888',
            'name': 'foo',
            'environment': 'development'
        }
      }
    }
}";
            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var builder = new ConfigurationBuilder()
                .AddJsonFile(path)
                .AddConfigServer();
            Configuration = builder.Build();

        }
  
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ConfigServerOptions>(Configuration);

            services.AddMvc();
        }
        public void Configure(IApplicationBuilder app)
        {

            app.UseMvc(routes =>
                routes.MapRoute(
                    name: "VerifyAsInjectedOptions",
                    template: "{controller=Home}/{action=VerifyAsInjectedOptions}")
                );
        }
    }
}
