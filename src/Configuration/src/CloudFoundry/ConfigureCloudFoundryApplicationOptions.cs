// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class ConfigureCloudFoundryApplicationOptions : IConfigureOptions<CloudFoundryApplicationOptions>
{
    private const string ConfigurationPrefix = "vcap:application";

    private readonly IConfiguration _configuration;
    private readonly IConfigureOptions<ApplicationInstanceInfo> _configureApplicationInstanceInfo;

    public ConfigureCloudFoundryApplicationOptions(IConfiguration configuration, IConfigureOptions<ApplicationInstanceInfo> configureApplicationInstanceInfo)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureApplicationInstanceInfo);

        _configuration = configuration;
        _configureApplicationInstanceInfo = configureApplicationInstanceInfo;
    }

    public void Configure(CloudFoundryApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IConfigurationSection section = _configuration.GetSection(ConfigurationPrefix);
        section.Bind(options);

        // Bug workaround for https://github.com/dotnet/runtime/issues/107247.
        options.ApplicationName = section["application_name"];

        _configureApplicationInstanceInfo.Configure(options);
    }
}
