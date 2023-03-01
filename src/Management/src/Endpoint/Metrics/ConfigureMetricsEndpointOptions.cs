using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Metrics;
internal class ConfigureMetricsEndpointOptions : IConfigureOptions<MetricsEndpointOptions>
{
    private readonly IConfiguration configuration;
    internal const string ManagementInfoPrefix = "management:endpoints:metrics";
    internal const string DefaultIngressIgnorePattern =
        "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";

    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";
    public ConfigureMetricsEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(MetricsEndpointOptions options)
    {

        configuration.GetSection(ManagementInfoPrefix).Bind(options);
        if (string.IsNullOrEmpty(options.Id))
        {
            options.Id = "metrics";
        }

        if (string.IsNullOrEmpty(options.IngressIgnorePattern))
        {
            options.IngressIgnorePattern = DefaultIngressIgnorePattern;
        }

        if (string.IsNullOrEmpty(options.EgressIgnorePattern))
        {
            options.EgressIgnorePattern = DefaultEgressIgnorePattern;
        }

    }

}
