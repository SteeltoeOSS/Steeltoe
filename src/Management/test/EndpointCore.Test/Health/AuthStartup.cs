// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class AuthStartup
    {
        public AuthStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddHealthActuator(_configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<AuthenticatedTestMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map<HealthEndpointCore>();
            });
        }
    }
}
