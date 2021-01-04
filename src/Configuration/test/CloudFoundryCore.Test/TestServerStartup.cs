// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class TestServerStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
#if NETCOREAPP3_1 || NET5_0
            app.UseRouting();
#else
            app.UseMvc();
#endif
        }
    }
}
