using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.Options;
internal class ConfigureManagementEndpointOptions : IConfigureNamedOptions<ManagementEndpointOptions>
{

    private static readonly string ManagementInfoPrefix = "management:endpoints";
    private static readonly string CloudFoundryEnabledPrefix = "management:cloudfoundry:enabled";
    private const string DefaultPath = "/actuator";
    private const string DefaultCFPath = "/cloudfoundryapplication";
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IContextName> _contextNames;
    private readonly IEnumerable<IEndpointOptions> _endpoints;

    public ConfigureManagementEndpointOptions(IConfiguration configuration, IEnumerable<IContextName> contextNames, IEnumerable<IEndpointOptions> endpointsCollection)
    {
        _configuration = configuration;
        _contextNames = contextNames;
        _endpoints = endpointsCollection;
    }

    public virtual void Configure(string name, ManagementEndpointOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);
        foreach (string converterTypeName in options.CustomJsonConverters ?? Array.Empty<string>())
        {
            var converterType = Type.GetType(converterTypeName);

            if (converterType != null)
            {
                var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                options.SerializerOptions.Converters.Add(converterInstance);
            }
        }
        foreach (var context in _contextNames)
        {
            options.ContextNames.Add(context.Name);
        }

        if (name == ActuatorContext.Name)
        {
            options.Path ??= DefaultPath;

            options.Exposure = new Exposure(_configuration);

            options.EndpointOptions = new List<IEndpointOptions>( _endpoints.Where(e=> e.GetType() != typeof(CloudFoundryEndpointOptions)));

        }
        else if (name == CFContext.Name)
        {
            options.Path = DefaultCFPath;
            var cfEnabledConfig = _configuration.GetSection(CloudFoundryEnabledPrefix).Value;
            if (cfEnabledConfig != null)
            {
                options.Enabled = !string.Equals(_configuration.GetSection(CloudFoundryEnabledPrefix).Value, "false", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                options.Enabled ??= true;
            }
            options.Exposure = new Exposure(allowAll: true);
            options.EndpointOptions = new List<IEndpointOptions>(_endpoints.Where(e => e.GetType() != typeof(HypermediaEndpointOptions)));
        }

    }

    public void Configure(ManagementEndpointOptions options)
    {
        throw new NotImplementedException();
    }
}
