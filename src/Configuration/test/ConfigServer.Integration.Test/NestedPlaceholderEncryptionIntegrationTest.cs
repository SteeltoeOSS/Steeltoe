// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Configuration.ConfigServer.Integration.Test;

public sealed class NestedPlaceholderEncryptionIntegrationTest
{
    [Fact]
    [Trait("Category", "Integration")]
    public void ResolveOuterEncryptionInnerPlaceholder_returnsDecryptedValuesInPlaceholder()
    {
        var settings = new Dictionary<string, string?>
        {
            { "encrypt:enabled", "true" },
            { "encrypt:keyStore:location", "./server.jks" },
            { "encrypt:keyStore:password", "letmein" },
            { "encrypt:keyStore:alias", "mytestkey" },
            { "encrypt:rsa:strong", "false" },
            { "encrypt:rsa:algorithm", "OAEP" },
            { "encrypt:rsa:salt", "deadbeef" },
            {
                "encrypted",
                "{cipher}AQATBPXCmri0MCEoCam0noXJgKGlFfE/chVN7XhH1V23MqJ8sI3lI61PyvsryJP3LlfNn38gUuulMeslAs/gUCoPFPV/zD7M8x527wQUbmWD6bR0ZMJ4hu3DisK6Diw2YAOxXSsm3Zh46cPFQcowfOG1x2OXj+5uL4T+VBGdt3Nr6dHCOumkTJ1KAtaJMfASf3J8G4M27v6m4Y2EdBqP1zWwDhAZ3R0u9uTP9xYUqQiKsUeOixrhOaCvtb1Q+Zg6A41CxM4cjL3Ty6miNYLx3QkxRvfkdo0iqo7jTrWWAT1aeRV6t5U5iMlWnD4eXzad60E3ZSINhvDiB03xPPPuHKC6qUTRJEEbQFegmn/KIPMMn9WaH/JLLZNvQYMuaFszZ84AE3aQcH0be+sNFDSjHNHL"
            },
            { "placeholder", "${encrypted}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot configuration1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<PlaceholderBeforeEncryptionStartup>().UseConfiguration(configuration1);

        using var server = new TestServer(hostBuilder);
        var configuration2 = server.Services.GetRequiredService<IConfiguration>();
        Assert.NotSame(configuration1, configuration2);

        Assert.Equal("encrypt the world", configuration2["encrypted"]);
        Assert.Equal("encrypt the world", configuration2["placeholder"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ResolveOuterPlaceholderInnerEncryption_returnsDecryptedValuesInPlaceholder()
    {
        var settings = new Dictionary<string, string?>
        {
            { "encrypt:enabled", "true" },
            { "encrypt:keyStore:location", "./server.jks" },
            { "encrypt:keyStore:password", "letmein" },
            { "encrypt:keyStore:alias", "mytestkey" },
            { "encrypt:rsa:strong", "false" },
            { "encrypt:rsa:algorithm", "OAEP" },
            { "encrypt:rsa:salt", "deadbeef" },
            {
                "encrypted",
                "{cipher}AQATBPXCmri0MCEoCam0noXJgKGlFfE/chVN7XhH1V23MqJ8sI3lI61PyvsryJP3LlfNn38gUuulMeslAs/gUCoPFPV/zD7M8x527wQUbmWD6bR0ZMJ4hu3DisK6Diw2YAOxXSsm3Zh46cPFQcowfOG1x2OXj+5uL4T+VBGdt3Nr6dHCOumkTJ1KAtaJMfASf3J8G4M27v6m4Y2EdBqP1zWwDhAZ3R0u9uTP9xYUqQiKsUeOixrhOaCvtb1Q+Zg6A41CxM4cjL3Ty6miNYLx3QkxRvfkdo0iqo7jTrWWAT1aeRV6t5U5iMlWnD4eXzad60E3ZSINhvDiB03xPPPuHKC6qUTRJEEbQFegmn/KIPMMn9WaH/JLLZNvQYMuaFszZ84AE3aQcH0be+sNFDSjHNHL"
            },
            { "placeholder", "${encrypted}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot configuration1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<EncryptionBeforePlaceholderStartup>().UseConfiguration(configuration1);

        using var server = new TestServer(hostBuilder);
        var configuration2 = server.Services.GetRequiredService<IConfiguration>();
        Assert.NotSame(configuration1, configuration2);

        Assert.Equal("encrypt the world", configuration2["encrypted"]);
        Assert.Equal("encrypt the world", configuration2["placeholder"]);
    }
}
