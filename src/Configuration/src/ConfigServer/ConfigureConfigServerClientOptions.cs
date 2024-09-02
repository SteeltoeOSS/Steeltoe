// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigureConfigServerClientOptions : IConfigureOptions<ConfigServerClientOptions>
{
    private const string VcapServicesConfigserverCredentialsPrefix = "vcap:services:p-config-server:0:credentials";
    private const string VcapServicesConfigserver30CredentialsPrefix = "vcap:services:p.config-server:0:credentials";
    private const string VcapServicesConfigserverCredentialsAltPrefix = "vcap:services:config-server:0:credentials";

    private readonly IConfiguration _configuration;

    public ConfigureConfigServerClientOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(ConfigServerClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ConfigServerClientOptions.ConfigurationPrefix).Bind(options);
        OverrideFromVcapServicesCredentials(options);

        options.Name ??= GetApplicationName();
    }

    private void OverrideFromVcapServicesCredentials(ConfigServerClientOptions options)
    {
        VcapServicesConfigServerCredentialsOptions credentialsOptions = new();
        _configuration.GetSection(VcapServicesConfigserverCredentialsAltPrefix).Bind(credentialsOptions);
        _configuration.GetSection(VcapServicesConfigserver30CredentialsPrefix).Bind(credentialsOptions);
        _configuration.GetSection(VcapServicesConfigserverCredentialsPrefix).Bind(credentialsOptions);

        options.Uri = credentialsOptions.Uri ?? options.Uri;
        options.ClientId = credentialsOptions.ClientId ?? options.ClientId;
        options.ClientSecret = credentialsOptions.ClientSecret ?? options.ClientSecret;
        options.AccessTokenUri = credentialsOptions.AccessTokenUri ?? options.AccessTokenUri;
    }

    private string? GetApplicationName()
    {
        var vcapOptions = new CloudFoundryApplicationOptions();

        var defaultConfigurer = new ConfigureApplicationInstanceInfo(_configuration);
        var cloudFoundryConfigurer = new ConfigureCloudFoundryApplicationOptions(_configuration, defaultConfigurer);
        cloudFoundryConfigurer.Configure(vcapOptions);

        return vcapOptions.ApplicationName;
    }
}
