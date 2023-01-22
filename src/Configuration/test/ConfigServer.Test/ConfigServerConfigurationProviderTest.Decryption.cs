// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Xunit;
using Xunit.Sdk;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationProviderDecryptionTest
{
    private readonly ConfigServerClientSettings _commonSettings = new()
    {
        Name = "myName"
    };

    [Fact]
    public async Task Deserialize_GoodJsonAsync()
    {
        const string environment = @"
                    {
                        ""name"": ""testname"",
                        ""profiles"": [""Production""],
                        ""label"": ""testlabel"",
                        ""version"": ""testversion"",
                        ""propertySources"": [
                            {
                                ""name"": ""source"",
                                ""source"": {
                                            ""secret1"": ""{cipher}e401ca0578839c9e5207f52d0ae4dc836f8c6530cdc90f14b544180f6fdb9265b80d6ace9fbbab700c7af32141171358""
                                    }
                            }
                        ]
                    }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder)
        {
            BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri)
        };

        ConfigServerClientSettings settings = _commonSettings;
        _commonSettings.EncryptionEnabled = true;
        _commonSettings.EncryptionKey = "12345678901234567890";
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        provider.Load();
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(provider.TryGet("secret1", out string value));
        Assert.Equal("encrypt the world", value);
    }
}
