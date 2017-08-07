using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        public Startup()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/management',
            'info' : {
                'enabled': false,
                'sensitive': false,
                'id': 'infomanagement'
            }
        }
    },
    'info': {
        'application': {
            'name': 'foobar',
            'version': '1.0.0',
            'date': '5/1/2008',
            'time' : '8:30:52 AM'
        },
        'NET': {
            'type': 'Core',
            'version': '1.1.0',
            'ASPNET' : {
                'type': 'Core',
                'version': '1.1.0'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            Configuration = configurationBuilder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseInfoActuator(Configuration);

            app.UseMvc();
        }
    }
}
