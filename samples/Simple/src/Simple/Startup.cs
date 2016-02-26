using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spring.Extensions.Configuration.Server;
using Simple.Model;

namespace Simple
{
    //
    // NOTE: This sample application assumes a running Spring Cloud Config Server is started
    //       with repository data for application named: foo, & profile: development
    //
    //       The easiest way to get that to happen is clone the spring-cloud-config
    //       repo and run the config-server.
    //          eg. git clone https://github.com/spring-cloud/spring-cloud-config.git
    //              cd spring-cloud-config\spring-cloud-config-server
    //              mvn spring-boot:run
    //

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()

                // Adds the Spring Cloud Configuration Server as a configuration source.
                // The settings used in contacting the Server will be picked up from
                // appsettings.json, and then overriden from any environment variables. 
                // Defaults will be used for any settings not present in either of those sources.
                // See ConfigServerClientSettings for defaults. 
                .AddConfigServer(env);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Optional for Spring Cloud Configuration Server.  
            // Adds IConfigurationRoot as a service and also configures the 
            // IOption<ConfigServerClientSettingsOptions> 
            // After this call, these components can be injected into other 
            // components(e.g. HomeController) using the standard ASP.NET DI mechanisms.
            services.AddConfigServer(Configuration);


            // Add framework services.
            services.AddMvc();

            // Add the configuration data returned from the Spring Cloud Config Server as IOption<>
            // It can also be injected into other ASP.NET components (eg. HomeController) using 
            // standard DI mechanisms provided by ASP.NET
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
