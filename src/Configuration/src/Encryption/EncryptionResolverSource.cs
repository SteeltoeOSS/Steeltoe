// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.Encryption;

/// <summary>
/// Configuration source used in creating a <see cref="EncryptionResolverProvider" /> that resolves encryptions. An encryption takes the form of:
/// <code><![CDATA[
/// ${some:config:reference?default_if_not_present}
/// ]]></code>
/// </summary>
internal sealed class EncryptionResolverSource : IConfigurationSource
{
    private readonly IConfigurationRoot? _configuration;
    private readonly ITextDecryptor _textDecryptor;

    internal IList<IConfigurationSource>? Sources { get; }
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionResolverSource" /> class.
    /// </summary>
    /// <param name="sources">
    /// The configuration sources to use.
    /// </param>
    /// <param name="textDecryptor">
    /// The decryptor to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public EncryptionResolverSource(IList<IConfigurationSource> sources, ITextDecryptor textDecryptor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(textDecryptor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _textDecryptor = textDecryptor;
        Sources = sources.ToList();
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionResolverSource" /> class.
    /// </summary>
    /// <param name="root">
    /// The root configuration to use.
    /// </param>
    /// <param name="textDecryptor">
    /// Decryptor to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public EncryptionResolverSource(IConfigurationRoot root, ITextDecryptor textDecryptor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(textDecryptor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _configuration = root;
        _textDecryptor = textDecryptor;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Builds a <see cref="EncryptionResolverProvider" /> from the specified builder.
    /// </summary>
    /// <param name="builder">
    /// Used to build providers from sources, in case a list of sources was provided instead of a configuration root.
    /// </param>
    /// <returns>
    /// The encryption resolver provider.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (_configuration != null)
        {
            var configurationView = new ConfigurationView(_configuration.Providers.ToArray());
            return new EncryptionResolverProvider(configurationView, _textDecryptor, LoggerFactory);
        }

        var providers = new List<IConfigurationProvider>();

        if (Sources != null)
        {
            foreach (IConfigurationSource source in Sources)
            {
                IConfigurationProvider provider = source.Build(builder);
                providers.Add(provider);
            }
        }

        return new EncryptionResolverProvider(providers, _textDecryptor, LoggerFactory);
    }
}
