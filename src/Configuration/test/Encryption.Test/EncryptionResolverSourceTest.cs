// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverSourceTest
{
    private readonly Mock<ITextDecryptor> _decryptorMock = new();

    [Fact]
    public void Constructors_InitializesProperties()
    {
        var memorySource = new MemoryConfigurationSource();

        var sources = new List<IConfigurationSource>
        {
            memorySource
        };

        var factory = new LoggerFactory();

        var encryptionSource = new EncryptionResolverSource(sources, _decryptorMock.Object, factory);
        Assert.Equal(factory, encryptionSource.LoggerFactory);
        Assert.NotNull(encryptionSource.Sources);
        Assert.Single(encryptionSource.Sources);
        Assert.NotSame(sources, encryptionSource.Sources);
        Assert.Contains(memorySource, encryptionSource.Sources);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var memorySource = new MemoryConfigurationSource();

        IList<IConfigurationSource> sources = new List<IConfigurationSource>
        {
            memorySource
        };

        var encryptionSource = new EncryptionResolverSource(sources, _decryptorMock.Object, NullLoggerFactory.Instance);
        IConfigurationProvider provider = encryptionSource.Build(new ConfigurationBuilder());
        Assert.IsType<EncryptionResolverProvider>(provider);
    }
}
