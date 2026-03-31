// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Certificates;
using Steeltoe.Configuration.CloudFoundry;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigureConfigServerClientOptions : IConfigureOptions<ConfigServerClientOptions>
{
    private const string VcapServicesConfigServerVersion2CredentialsPrefix = "vcap:services:p-config-server:0:credentials";
    private const string VcapServicesConfigServerVersion3CredentialsPrefix = "vcap:services:p.config-server:0:credentials";
    private const string VcapServicesConfigServerCredentialsAltPrefix = "vcap:services:config-server:0:credentials";

    private readonly IConfiguration _configuration;
    private readonly Action<ConfigServerClientOptions>? _configure;

    public ConfigureConfigServerClientOptions(IConfiguration configuration, Action<ConfigServerClientOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
        _configure = configure;
    }

    public void Configure(ConfigServerClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ConfigServerClientOptions.ConfigurationPrefix).Bind(options);
        _configure?.Invoke(options);

        OverrideFromVcapServicesCredentials(options);
        ConfigureClientCertificate(options);

        options.Name ??= GetApplicationName();
        options.Environment ??= "Production";
    }

    private void OverrideFromVcapServicesCredentials(ConfigServerClientOptions options)
    {
        VcapServicesConfigServerCredentialsOptions credentialsOptions = new();
        _configuration.GetSection(VcapServicesConfigServerCredentialsAltPrefix).Bind(credentialsOptions);
        _configuration.GetSection(VcapServicesConfigServerVersion2CredentialsPrefix).Bind(credentialsOptions);
        _configuration.GetSection(VcapServicesConfigServerVersion3CredentialsPrefix).Bind(credentialsOptions);

        options.Uri = credentialsOptions.Uri ?? options.Uri;
        options.ClientId = credentialsOptions.ClientId ?? options.ClientId;
        options.ClientSecret = credentialsOptions.ClientSecret ?? options.ClientSecret;
        options.AccessTokenUri = credentialsOptions.AccessTokenUri ?? options.AccessTokenUri;
    }

    private void ConfigureClientCertificate(ConfigServerClientOptions options)
    {
        if (options.ClientCertificate.Certificate != null)
        {
            return;
        }

        var certificateConfigurer = new ConfigureCertificateOptions(_configuration);

        var certificateOptions = new CertificateOptions();
        certificateConfigurer.Configure("ConfigServer", certificateOptions);

        if (certificateOptions.Certificate == null)
        {
            certificateConfigurer.Configure(certificateOptions);
        }

        options.ClientCertificate = certificateOptions.Clone();
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
