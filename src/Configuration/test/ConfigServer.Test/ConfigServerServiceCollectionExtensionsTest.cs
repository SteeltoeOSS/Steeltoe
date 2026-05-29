// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerServiceCollectionExtensionsTest
{
    [Fact]
    public void DoesNotAddConfigServerMultipleTimes()
    {
        var builder = new ConfigurationBuilder();
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();
        builder.AddDecryption();
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();
        builder.AddDecryption();
        builder.AddConfigServer();

        builder.EnumerateSources<ConfigServerConfigurationSource>().Should().ContainSingle();

        IConfigurationRoot configurationRoot = builder.Build();

        configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }
}
