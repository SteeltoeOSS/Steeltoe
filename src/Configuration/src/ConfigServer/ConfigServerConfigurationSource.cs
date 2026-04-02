// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerConfigurationSource : IConfigurationSource
{
    private readonly ILoggerFactory _loggerFactory;

    internal List<IConfigurationSource> Sources { get; }
    internal Dictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets the initial options the client uses to contact Config Server.
    /// </summary>
    public ConfigServerClientOptions DefaultOptions { get; }

    /// <summary>
    /// Gets the configuration the client uses to contact Config Server. Entries overrule <see cref="DefaultOptions" />.
    /// </summary>
    public IConfiguration? Configuration { get; private set; }

    /// <summary>
    /// Gets an optional delegate that further configures options from code, after settings from <see cref="Configuration" /> have been applied.
    /// </summary>
    public Action<ConfigServerClientOptions>? Configure { get; }

    /// <summary>
    /// Gets an optional factory to create the HTTP client handler, used to mock HTTP requests to Config Server in tests. When provided, the caller is
    /// responsible for handler disposal.
    /// </summary>
    public Func<HttpClientHandler>? CreateHttpClientHandler { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource" /> class.
    /// </summary>
    /// <param name="defaultOptions">
    /// The initial options the client uses to contact Config Server.
    /// </param>
    /// <param name="sources">
    /// Configuration sources the client uses to contact Config Server. The <see cref="Configuration" /> will be built from these, whose entries overrule
    /// <paramref name="defaultOptions" />.
    /// </param>
    /// <param name="properties">
    /// Configuration properties the client uses to contact Config Server. The <see cref="Configuration" /> will be built from these, whose entries overrule
    /// <paramref name="defaultOptions" />.
    /// </param>
    /// <param name="configure">
    /// An optional delegate that further configures options from code, after settings from the built <see cref="Configuration" /> have been applied.
    /// </param>
    /// <param name="createHttpClientHandler">
    /// An optional factory to create the HTTP client handler, used to mock HTTP requests to Config Server in tests. When provided, the caller is responsible
    /// for handler disposal.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationSource(ConfigServerClientOptions defaultOptions, IList<IConfigurationSource> sources,
        IDictionary<string, object>? properties, Action<ConfigServerClientOptions>? configure, Func<HttpClientHandler>? createHttpClientHandler,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultOptions);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Sources = sources.ToList();
        Properties = properties != null ? new Dictionary<string, object>(properties) : [];

        DefaultOptions = defaultOptions;
        Configure = configure;
        CreateHttpClientHandler = createHttpClientHandler;
        _loggerFactory = loggerFactory;
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
        var configurationBuilder = new ConfigurationBuilder();

        foreach (IConfigurationSource source in Sources)
        {
            configurationBuilder.Add(source);
        }

        foreach (KeyValuePair<string, object> pair in Properties)
        {
            configurationBuilder.Properties.Add(pair);
        }

        Configuration = configurationBuilder.Build();
        return new ConfigServerConfigurationProvider(this, _loggerFactory);
    }
}
