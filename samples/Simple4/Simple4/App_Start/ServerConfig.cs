using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Spring.Extensions.Configuration.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.FileProviders;

namespace Simple4
{

    public class ServerConfig
    {

       
        public static IConfigurationRoot Configuration { get; set; }

        public static void RegisterConfig(string environment)
        {
            var env = new HostingEnvironment(environment);

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

            // Save configuration for future access
            Configuration = builder.Build();


        }
    }
    public class HostingEnvironment : IHostingEnvironment
    {
        public HostingEnvironment(string env)
        {
            EnvironmentName = env;
        }
        public string EnvironmentName { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        public string WebRootPath { get; set; }

    }
}