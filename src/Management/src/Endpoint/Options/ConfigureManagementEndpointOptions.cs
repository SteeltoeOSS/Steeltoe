// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.Options;

internal class ConfigureManagementEndpointOptions : IConfigureNamedOptions<ManagementEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints";
    private const string CloudFoundryEnabledPrefix = "management:cloudfoundry:enabled";
    private const string DefaultPath = "/actuator";
    private const string DefaultCFPath = "/cloudfoundryapplication";
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<HttpMiddlewareOptions> _endpoints;

    public ConfigureManagementEndpointOptions(IConfiguration configuration, IEnumerable<HttpMiddlewareOptions> endpointsCollection)
    {
        _configuration = configuration;
        _endpoints = endpointsCollection;
    }

    public virtual void Configure(string name, ManagementEndpointOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        // Regardless of the name, configure the available contexts
        options.EndpointContexts.Add(EndpointContexts.Actuator);

        if (Platform.IsCloudFoundry)
        {
            options.EndpointContexts.Add(EndpointContexts.CloudFoundry);
        }

        foreach (string converterTypeName in options.CustomJsonConverters ?? Array.Empty<string>())
        {
            var converterType = Type.GetType(converterTypeName);

            if (converterType != null)
            {
                var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                options.SerializerOptions.Converters.Add(converterInstance);
            }
        }

        if (name == EndpointContexts.Actuator.ToString())
        {
            options.Path ??= DefaultPath;

            options.Exposure = new Exposure(_configuration);

            options.EndpointOptions = new List<HttpMiddlewareOptions>(_endpoints.Where(e => e is not CloudFoundryEndpointOptions));
        }
        else if (name == EndpointContexts.CloudFoundry.ToString())
        {
            options.Path = DefaultCFPath;
            string cfEnabledConfig = _configuration.GetSection(CloudFoundryEnabledPrefix).Value;

            if (cfEnabledConfig != null)
            {
                options.Enabled = !string.Equals(_configuration.GetSection(CloudFoundryEnabledPrefix).Value, "false", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                options.Enabled ??= true;
            }

            options.Exposure = new Exposure(true);
            options.EndpointOptions = new List<HttpMiddlewareOptions>(_endpoints.Where(e => e is not HypermediaEndpointOptions));
        }
    }

    public void Configure(ManagementEndpointOptions options)
    {
        throw new NotImplementedException();
    }
}
