// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest
{
    internal class TestServerStartup
    {
        public TestServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ConfigServerDataAsOptions>(Configuration);
            services.AddConfigServerHealthContributor();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetServices<IConfiguration>();
#if NETCOREAPP3_0
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
#else
            app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "VerifyAsInjectedOptions",
                        template: "{controller=Home}/{action=VerifyAsInjectedOptions}");
                    routes.MapRoute(
                        name: "Health",
                        template: "{controller=Home}/{action=Health}");
                });
#endif
        }
    }
}
