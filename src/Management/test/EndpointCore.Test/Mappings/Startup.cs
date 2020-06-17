// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMappingsActuator(Configuration);
            services
                .AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMappingsActuator();

#if NETCOREAPP3_1
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
#else
            app.UseMvc();
#endif
        }
    }
}
