// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureEncryptionResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}somecipher" },
            { "key3", "{cipher}{key:keyalias}somekeyaliascipher" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForConfigureEncryptionResolver>().UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        var config2 = server.Services.GetRequiredService<IConfiguration>();
        Assert.NotSame(config1, config2);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("DECRYPTED", config2["key2"]);
        Assert.Equal("DECRYPTEDWITHALIAS", config2["key3"]);
    }
}
