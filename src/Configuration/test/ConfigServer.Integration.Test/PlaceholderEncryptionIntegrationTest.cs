// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Integration.Test;

public sealed class PlaceholderEncryptionIntegrationTest
{
    [Fact]
    public void PlaceholderInsideDecryptionProvider_ReturnsDecryptedValuesInPlaceholder()
    {
        var settings = new Dictionary<string, string?>
        {
            ["encrypt:enabled"] = "true",
            ["encrypt:keyStore:location"] = "./server.jks",
            ["encrypt:keyStore:password"] = "letmein",
            ["encrypt:keyStore:alias"] = "mytestkey",
            ["encrypt:rsa:strong"] = "false",
            ["encrypt:rsa:algorithm"] = "OAEP",
            ["encrypt:rsa:salt"] = "deadbeef",
            ["encrypted"] =
                "{cipher}AQBoKgZNlxY+EWcGG0CXhyfV4q/u7lJjCBS+9liSKpu/w4gNmJhTYvjDJ3XIExVSVit41po5n91LVI3h777QlY7b0D2zOI0f4YR/9MtAdsq/cgRGZ4uzcv69bmVnQ0yt5ilxV021TH0EsVEmwmgyY+n1mKcD7aXWQwS2lAvJycgVgrDfbj2qz2c7aPn+8mXvG8EAbNmEhCbATCdPDlmBUPjLvuSweDlzlefQJ+jVSxLHfOcQ+g17arhIH1j0nZEAGywoNBGS1xg6DQ+8sW0GiYennTrnKslzMFjPTQ8QJSONzYysdRLGbV2Bi73ifUd+4AnMuSKcIRNiRACRtt+i7ZhrgTWRV1F+F8vfIiqf3SfzHdyclHkoCVkPhNBc9ySq0XRubPtg7UnW2KPZufZ0D7xx",
            ["placeholder"] = "${encrypted}"
        };

        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(settings);
            configurationBuilder.AddPlaceholderResolver();
            configurationBuilder.AddDecryption();
        });

        using IWebHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["encrypted"].Should().Be("encrypt the world");
        configuration["placeholder"].Should().Be("encrypt the world");
    }

    [Fact]
    public void DecryptionInsidePlaceholderProvider_ReturnsDecryptedValuesInPlaceholder()
    {
        var settings = new Dictionary<string, string?>
        {
            ["encrypt:enabled"] = "true",
            ["encrypt:keyStore:location"] = "./server.jks",
            ["encrypt:keyStore:password"] = "letmein",
            ["encrypt:keyStore:alias"] = "mytestkey",
            ["encrypt:rsa:strong"] = "false",
            ["encrypt:rsa:algorithm"] = "OAEP",
            ["encrypt:rsa:salt"] = "deadbeef",
            ["encrypted"] =
                "{cipher}AQBoKgZNlxY+EWcGG0CXhyfV4q/u7lJjCBS+9liSKpu/w4gNmJhTYvjDJ3XIExVSVit41po5n91LVI3h777QlY7b0D2zOI0f4YR/9MtAdsq/cgRGZ4uzcv69bmVnQ0yt5ilxV021TH0EsVEmwmgyY+n1mKcD7aXWQwS2lAvJycgVgrDfbj2qz2c7aPn+8mXvG8EAbNmEhCbATCdPDlmBUPjLvuSweDlzlefQJ+jVSxLHfOcQ+g17arhIH1j0nZEAGywoNBGS1xg6DQ+8sW0GiYennTrnKslzMFjPTQ8QJSONzYysdRLGbV2Bi73ifUd+4AnMuSKcIRNiRACRtt+i7ZhrgTWRV1F+F8vfIiqf3SfzHdyclHkoCVkPhNBc9ySq0XRubPtg7UnW2KPZufZ0D7xx",
            ["placeholder"] = "${encrypted}"
        };

        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(settings);
            configurationBuilder.AddDecryption();
            configurationBuilder.AddPlaceholderResolver();
        });

        using IWebHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["encrypted"].Should().Be("encrypt the world");
        configuration["placeholder"].Should().Be("encrypt the world");
    }
}
