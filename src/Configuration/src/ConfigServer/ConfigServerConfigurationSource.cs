// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Certificates;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerConfigurationSource : IConfigurationSource
{
    internal List<IConfigurationSource> Sources { get; } = [];
    internal Dictionary<string, object> Properties { get; } = [];

    /// <summary>
    /// Gets the default settings the Config Server client uses to contact the Config Server.
    /// </summary>
    internal ConfigServerClientOptions DefaultOptions { get; }

    /// <summary>
    /// Gets the configuration the Config Server client uses to contact the Config Server. Values returned override the default values provided in
    /// <see cref="DefaultOptions" />.
    /// </summary>
    internal IConfiguration? Configuration { get; private set; }

    /// <summary>
    /// Gets the logger factory used by the Config Server client.
    /// </summary>
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultOptions">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="configuration">
    /// configuration used by the Config Server client. Values will override those found in default settings.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientOptions defaultOptions, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(defaultOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Configuration = configuration;
        DefaultOptions = defaultOptions;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultOptions">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultOptions" />.
    /// </param>
    /// <param name="properties">
    /// properties to be used when sources are built.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientOptions defaultOptions, IList<IConfigurationSource> sources,
        IDictionary<string, object>? properties, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultOptions);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Sources = sources.ToList();

        if (properties != null)
        {
            Properties = new Dictionary<string, object>(properties);
        }

        DefaultOptions = defaultOptions;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Returns a <see cref="ConfigServerConfigurationProvider" /> configured using the values from this <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder, unused.
    /// </param>
    /// <returns>
    /// The configuration provider.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (Configuration == null)
        {
            // Create our own builder to build sources
            var configurationBuilder = new ConfigurationBuilder();

            foreach (IConfigurationSource source in Sources)
            {
                configurationBuilder.Add(source);
            }

            // Use properties provided
            foreach (KeyValuePair<string, object> pair in Properties)
            {
                configurationBuilder.Properties.Add(pair);
            }

            // Create configuration
            Configuration = configurationBuilder.Build();
        }

        string? clientCertificatePath = Configuration.GetValue<string>($"{CertificateOptions.ConfigurationKeyPrefix}:ConfigServer:CertificateFilePath");

        if (!string.IsNullOrEmpty(clientCertificatePath) && DefaultOptions.ClientCertificate.Certificate == null)
        {
            var certificateConfigurer = new ConfigureCertificateOptions(Configuration);

            var options = new CertificateOptions();
            certificateConfigurer.Configure("ConfigServer", options);
            DefaultOptions.ClientCertificate.Certificate = options.Certificate;
        }

        return new ConfigServerConfigurationProvider(this, LoggerFactory);
    }
}
