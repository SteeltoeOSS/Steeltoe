// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Tracing;

internal sealed class ConfigureTracingOptions : IConfigureOptions<TracingOptions>
{
    private const string DefaultIngressIgnorePattern = "/actuator/.*|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";
    private const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions|/eureka/*";

    private readonly IConfiguration _configuration;
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    public ConfigureTracingOptions(IConfiguration configuration, IApplicationInstanceInfo applicationInstanceInfo)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);

        _configuration = configuration;
        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public void Configure(TracingOptions options)
    {
        _configuration.GetSection("management:tracing").Bind(options);

        options.Name ??= _applicationInstanceInfo.ApplicationName;
        options.IngressIgnorePattern ??= DefaultIngressIgnorePattern;
        options.EgressIgnorePattern ??= DefaultEgressIgnorePattern;
    }
}
