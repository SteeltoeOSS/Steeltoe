// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption;

internal sealed partial class DecryptionConfigurationSource : ICompositeConfigurationSource
{
    private readonly ITextDecryptor? _textDecryptor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DecryptionConfigurationSource> _logger;

    public IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();

    public DecryptionConfigurationSource(ITextDecryptor? textDecryptor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _textDecryptor = textDecryptor;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DecryptionConfigurationSource>();
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        LogBuild(_logger, Sources.Count, builder.Properties.Count);
        List<IConfigurationProvider> providers = Sources.Select(source => source.Build(builder)).ToList();
        return new DecryptionConfigurationProvider(providers, _textDecryptor, _loggerFactory);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Build for {SourceCount} sources and {PropertyCount} properties.")]
    private static partial void LogBuild(ILogger<DecryptionConfigurationSource> logger, int sourceCount, int propertyCount);
}
