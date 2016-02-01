
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spring.Extensions.Configuration.CloudFoundry;
using Spring.Extensions.Configuration.Server;
using SimpleCloudFoundry.Model;
using System;

namespace SimpleCloudFoundry
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, ILoggerFactory logFactory)
        {
            logFactory.AddConsole(minLevel: LogLevel.Debug);
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()

                // 
                // Adds CloudFoundry environment variables as a configuration source
                // e.g. VCAP_APPLICATION -> vcap:application:application_name, etc.
                // e.g. VCAP_SERVICES -> vcap:services:p-mysql:name, etc.
                //
                .AddCloudFoundry()

                //
                // Adds the Spring Cloud Configuration Server as a configuration source.
                // The settings used in contacting the Server will be picked up from
                // appsettings.json, and then overriden from any environment variables, and then
                // overriden from the CloudFoundry environment variable settings. 
                // Defaults will be used for any settings not present in any of the earlier added 
                // sources.  See ConfigServerClientSettings for defaults. 
                //
                .AddConfigServer(env, logFactory);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddOptions();
            services.AddMvc();

            // Add the Configuration data from VCAP_APPLICATION and VCAP_SERVICES
            // as IOptions that can be injected into other ASP.NET components (e.g. HomeController)
            services.Configure<CloudFoundryApplicationOptions>(Configuration);
            services.Configure<CloudFoundryServicesOptions>(Configuration);

            // Add the Spring Cloud Config Server client settings as IOption<>
            // Then it can be injected into other ASP.NET components (eg. HomeControllqer) using 
            // standard DI mechanisms provided by ASP.NET
            services.Configure<ConfigServerClientSettingsOptions>(Configuration);

            // Add the configuration data returned from the Spring Cloud Config Server
            // as IOptions that can be injected into other ASP.NET components (eg. HomeController)
            services.Configure<ConfigServerData>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIISPlatformHandler();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
