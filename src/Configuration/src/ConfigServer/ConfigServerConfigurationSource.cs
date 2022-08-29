// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Options;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public sealed class ConfigServerConfigurationSource : IConfigurationSource
{
    internal IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();

    internal IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the default settings the Config Server client uses to contact the Config Server.
    /// </summary>
    internal ConfigServerClientSettings DefaultSettings { get; }

    /// <summary>
    /// Gets the configuration the Config Server client uses to contact the Config Server. Values returned override the default values provided in
    /// <see cref="DefaultSettings" />.
    /// </summary>
    internal IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Gets the logger factory used by the Config Server client.
    /// </summary>
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="configuration">
    /// configuration used by the Config Server client. Values will override those found in default settings.
    /// </param>
    public ConfigServerConfigurationSource(IConfiguration configuration)
        : this(configuration, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="configuration">
    /// configuration used by the Config Server client. Values will override those found in default settings.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(IConfiguration configuration, ILoggerFactory loggerFactory)
        : this(new ConfigServerClientSettings(), configuration, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultSettings">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="configuration">
    /// configuration used by the Config Server client. Values will override those found in default settings.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IConfiguration configuration)
        : this(defaultSettings, configuration, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultSettings">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="configuration">
    /// configuration used by the Config Server client. Values will override those found in default settings.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(defaultSettings);
        ArgumentGuard.NotNull(loggerFactory);

        Configuration = configuration;
        DefaultSettings = defaultSettings;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultSettings" />.
    /// </param>
    /// <param name="properties">
    /// properties to be used when sources are built.
    /// </param>
    public ConfigServerConfigurationSource(IList<IConfigurationSource> sources, IDictionary<string, object> properties)
        : this(sources, properties, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultSettings" />.
    /// </param>
    /// <param name="properties">
    /// properties to be used when sources are built.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(IList<IConfigurationSource> sources, IDictionary<string, object> properties, ILoggerFactory loggerFactory)
        : this(new ConfigServerClientSettings(), sources, properties, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultSettings">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultSettings" />.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IList<IConfigurationSource> sources)
        : this(defaultSettings, sources, null, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultSettings">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultSettings" />.
    /// </param>
    /// <param name="properties">
    /// properties to be used when sources are built.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IList<IConfigurationSource> sources,
        IDictionary<string, object> properties)
        : this(defaultSettings, sources, properties, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultSettings">
    /// the default settings used by the Config Server client.
    /// </param>
    /// <param name="sources">
    /// configuration sources used by the Config Server client. The <see cref="Configuration" /> will be built from these sources and the values will
    /// override those found in <see cref="DefaultSettings" />.
    /// </param>
    /// <param name="properties">
    /// properties to be used when sources are built.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IList<IConfigurationSource> sources,
        IDictionary<string, object> properties, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(defaultSettings);
        ArgumentGuard.NotNull(sources);
        ArgumentGuard.NotNull(loggerFactory);

        Sources = new List<IConfigurationSource>(sources);

        if (properties != null)
        {
            Properties = new Dictionary<string, object>(properties);
        }

        DefaultSettings = defaultSettings;
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

        IConfigurationSource certificateSource = Sources.FirstOrDefault(cSource => cSource is ICertificateSource);

        if (certificateSource != null && DefaultSettings.ClientCertificate == null)
        {
            var certificateConfigurer =
                (IConfigureNamedOptions<CertificateOptions>)Activator.CreateInstance(((ICertificateSource)certificateSource).OptionsConfigurer, Configuration)!;

            var options = new CertificateOptions();
            certificateConfigurer.Configure(options);
            DefaultSettings.ClientCertificate = options.Certificate;
        }

        return new ConfigServerConfigurationProvider(this);
    }
}
