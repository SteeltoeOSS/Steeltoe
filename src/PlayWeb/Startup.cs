using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Security.Authentication.CloudFoundry;
using Steeltoe.Security.Authentication.MtlsCore;
using Steeltoe.Security.Authentication.MtlsCore.Events;

namespace PlayWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCloudFoundryContainerIdentity(Configuration);
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCloudFoundryIdentityCertificate();
            services.AddAuthorization(cfg => cfg.AddPolicy("sameorg", builder => builder.SameOrg()));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseCloudFoundryContainerIdentity();
            app.UseAuthentication();

            app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}