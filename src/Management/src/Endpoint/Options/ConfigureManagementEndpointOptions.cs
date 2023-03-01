using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.Options;
internal class ConfigureManagementEndpointOptions : IConfigureNamedOptions<ManagementEndpointOptions>
{

    private static readonly string ManagementInfoPrefix = "management:endpoints";
    private static readonly string CloudFoundryEnabledPrefix = "management:cloudfoundry:enabled";
    private readonly IConfiguration configuration;

    public ConfigureManagementEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(string name, ManagementEndpointOptions options)
    {
        configuration.GetSection(ManagementInfoPrefix).Bind(options);
        foreach (string converterTypeName in options.CustomJsonConverters ?? Array.Empty<string>())
        {
            var converterType = Type.GetType(converterTypeName);

            if (converterType != null)
            {
                var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                options.SerializerOptions.Converters.Add(converterInstance);
            }
        }
        if (name == EndpointContextNames.ActuatorManagementOptionName)
        {
            options.Path ??= "/actuator";
            options.Exposure = new Exposure(configuration);
        }
        else if (name == EndpointContextNames.CFManagemementOptionName)
        {
            options.Path = "/cloudfoundryapplication";
            options.Enabled = configuration.GetSection(CloudFoundryEnabledPrefix).Value != "false";
            options.Exposure = new Exposure(allowAll: true);
        }

    }

    public void Configure(ManagementEndpointOptions options)
    {
        throw new NotImplementedException();
    }
}
