// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationProviderTest
{
    private readonly ConfigServerClientOptions _commonOptions = new()
    {
        Name = "myName"
    };

    [Fact]
    public void DefaultConstructor_InitializedWithDefaultSettings()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(provider.ClientOptions, expectedAppName);
    }

    [Fact]
    public void SourceConstructor_WithDefaults_InitializesWithDefaultSettings()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var options = new ConfigServerClientOptions();
        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(provider.ClientOptions, expectedAppName);
    }

    [Fact]
    public void SourceConstructor_WithTimeoutConfigured_InitializesHttpClientWithConfiguredTimeout()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:cloud:config:timeout", "30000" }
        }).Build();

        var options = new ConfigServerClientOptions();
        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromMilliseconds(30000), httpClient.Timeout);
    }

    [Fact]
    public void GetConfigServerUri_NoBaseUri_Throws()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Assert.Throws<ArgumentNullException>(() => provider.BuildConfigServerUri(null!, null));
    }

    [Fact]
    public void GetConfigServerUri_NoLabel()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, null).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabel()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, options.Label).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}/{options.Label}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabelContainingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel/version"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, options.Label).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}/myLabel(_)version", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/myPath/path/",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo_NoEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/myPath/path",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_NoEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public async Task Deserialize_GoodJsonAsync()
    {
        var environment = new ConfigEnvironment
        {
            Name = "testname",
            Label = "testlabel",
            Profiles =
            {
                "Production"
            },
            Version = "testversion",
            State = "teststate",
            PropertySources =
            {
                new PropertySource
                {
                    Name = "source",
                    Source =
                    {
                        { "key1", "value1" },
                        { "key2", 10 }
                    }
                }
            }
        };

        var content = JsonContent.Create(environment);

        var env = await content.ReadFromJsonAsync<ConfigEnvironment>(ConfigServerConfigurationProvider.SerializerOptions);
        Assert.NotNull(env);
        Assert.Equal("testname", env.Name);
        Assert.NotNull(env.Profiles);
        Assert.Single(env.Profiles);
        Assert.Equal("testlabel", env.Label);
        Assert.Equal("testversion", env.Version);
        Assert.Equal("teststate", env.State);
        Assert.NotNull(env.PropertySources);
        Assert.Single(env.PropertySources);
        Assert.Equal("source", env.PropertySources[0].Name);
        Assert.NotNull(env.PropertySources[0].Source);
        Assert.Equal(2, env.PropertySources[0].Source.Count);
        Assert.Equal("value1", env.PropertySources[0].Source["key1"].ToString());
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString()!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RemoteLoadAsync_InvalidUri()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<UriFormatException>(async () => await provider.RemoteLoadAsync(new[]
        {
            "foobar\\foobar\\"
        }, null, CancellationToken.None));
    }

    [Fact]
    public async Task RemoteLoadAsync_HostTimesOut()
    {
        var options = new ConfigServerClientOptions
        {
            Timeout = 100
        };

        var httpClientHandler = new SlowHttpClientHandler(1.Seconds(), new HttpResponseMessage());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        Func<Task> action = async () => await provider.RemoteLoadAsync(["http://localhost:9999/app/profile"], null, CancellationToken.None);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerException<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [500];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(async () => await provider.RemoteLoadAsync(options.GetUris(), null, CancellationToken.None));

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [204];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(options.GetUris(), null, CancellationToken.None);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithPollingTimer()
    {
        // Arrange
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        TestConfigServerStartup.ReturnStatus = Enumerable.Repeat(200, 100).ToArray();
        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            PollingInterval = TimeSpan.FromMilliseconds(300),
            Label = "label,testlabel"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);
        Assert.True(TestConfigServerStartup.InitialRequestLatch.Wait(TimeSpan.FromSeconds(60)));
        Assert.True(TestConfigServerStartup.RequestCount >= 1);
        await Task.Delay(1000);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(TestConfigServerStartup.RequestCount >= 2);
        Assert.False(provider.GetReloadToken().HasChanged);
    }

    [Fact]
    public async Task Create_FailFastEnabledAndExceptionThrownDuringPolling_DoesNotCrash()
    {
        // Arrange
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        // Initial requests succeed, but later requests return 400 status code so that an exception is thrown during polling
        TestConfigServerStartup.ReturnStatus = Enumerable.Repeat(200, 2).Concat(Enumerable.Repeat(400, 100)).ToArray();
        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            PollingInterval = TimeSpan.FromMilliseconds(300),
            FailFast = true,
            Label = "testlabel"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());

        // Act
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        // Assert
        Assert.True(TestConfigServerStartup.InitialRequestLatch.Wait(TimeSpan.FromSeconds(60)));
        Assert.True(TestConfigServerStartup.RequestCount >= 1);
        await Task.Delay(1000);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(TestConfigServerStartup.RequestCount >= 2);
        Assert.False(provider.GetReloadToken().HasChanged);
    }

    [Fact]
    public void Create_WithNonZeroPollingIntervalAndClientDisabled_PollingDisabled()
    {
        // Arrange
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        TestConfigServerStartup.ReturnStatus = Enumerable.Repeat(200, 100).ToArray();
        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Enabled = false,
            PollingInterval = TimeSpan.FromMilliseconds(300),
            Label = "label,testlabel"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());

        // Act
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        // Assert
        Assert.False(TestConfigServerStartup.InitialRequestLatch.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DoLoad_MultipleLabels_ChecksAllLabels()
    {
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.Label = "label,testlabel";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.DoLoadAsync(true, CancellationToken.None);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal(2, TestConfigServerStartup.RequestCount);
        Assert.Equal($"/{options.Name}/{options.Environment}/testlabel", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGood()
    {
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? env = await provider.RemoteLoadAsync(options.GetUris(), null, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(env);
        Assert.Equal("testname", env.Name);
        Assert.NotNull(env.Profiles);
        Assert.Single(env.Profiles);
        Assert.Equal("testlabel", env.Label);
        Assert.Equal("testversion", env.Version);
        Assert.NotNull(env.PropertySources);
        Assert.Single(env.PropertySources);
        Assert.Equal("source", env.PropertySources[0].Name);
        Assert.NotNull(env.PropertySources[0].Source);
        Assert.Equal(2, env.PropertySources[0].Source.Count);
        Assert.Equal("value1", env.PropertySources[0].Source["key1"].ToString());
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString()!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsGreaterThanEqualBadRequest_StopsChecking()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            200
        ];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.Uri = "http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.Uri = "http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [404];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(27, provider.Properties.Count);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [404];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.FailFast = true;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = _commonOptions;
        options.FailFast = true;
        options.Uri = "http://localhost:8888,http://localhost:8888";
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [500];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.FailFast = true;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            500,
            500
        ];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        options.FailFast = true;
        options.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            500,
            500,
            500,
            500,
            500
        ];

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            FailFast = true,
            Retry =
            {
                Enabled = true,
                InitialInterval = 10
            },
            Timeout = 10
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(6, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ChangesDataDictionary()
    {
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);

        Assert.True(provider.TryGet("key1", out string? value));
        Assert.Equal("value1", value);
        Assert.True(provider.TryGet("key2", out value));
        Assert.Equal("10", value);
    }

    [Fact]
    public void ReLoad_DataDictionary_With_New_Configurations()
    {
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "featureToggles.ShowModule[0]": "FT1",
                    "featureToggles.ShowModule[1]": "FT2",
                    "featureToggles.ShowModule[2]": "FT3",
                    "enableSettings": "true"
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = _commonOptions;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        provider.Load();
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out string? value));
        Assert.Equal("FT1", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:1", out value));
        Assert.Equal("FT2", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:2", out value));
        Assert.Equal("FT3", value);
        Assert.True(provider.TryGet("enableSettings", out value));
        Assert.Equal("true", value);

        TestConfigServerStartup.Reset();

        TestConfigServerStartup.Response = """
        {
          "name": "testname",
          "profiles": [
            "Production"
          ],
          "label": "testlabel",
          "version": "testversion",
          "propertySources": [
            {
              "name": "source",
              "source": {
                "featureToggles.ShowModule[0]": "none"
              }
            }
          ]
        }
        """;

        provider.Load();
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out value));
        Assert.Equal("none", value);
        Assert.False(provider.TryGet("featureToggles:ShowModule:1", out _));
        Assert.False(provider.TryGet("featureToggles:ShowModule:2", out _));
        Assert.False(provider.TryGet("enableSettings", out _));
    }

    [Fact]
    public void AddConfigServerClientSettings_ChangesDataDictionary()
    {
        var options = new ConfigServerClientOptions
        {
            Enabled = false,
            FailFast = true,
            Environment = "environment",
            Label = "label",
            Name = "name",
            Uri = "https://foo.bar/",
            Username = "username",
            Password = "password",
            Token = "vaultToken",
            Timeout = 75_000,
            PollingInterval = TimeSpan.FromSeconds(35.5),
            ValidateCertificates = false,
            AccessTokenUri = "https://token.server.com/",
            ClientSecret = "client_secret",
            ClientId = "client_id",
            TokenTtl = 2,
            TokenRenewRate = 1,
            DisableTokenRenewal = true,
            Retry =
            {
                Enabled = true,
                InitialInterval = 8,
                MaxInterval = 16,
                Multiplier = 1.1,
                MaxAttempts = 7
            },
            Discovery =
            {
                Enabled = true,
                ServiceId = "my-config-server"
            },
            Health =
            {
                Enabled = false,
                TimeToLive = 9
            },
            Headers =
            {
                ["headerName1"] = "headerValue1",
                ["headerName2"] = "headerValue2"
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        provider.AddConfigServerClientOptions();

        AssertDataValue("spring:cloud:config:enabled", "False");
        AssertDataValue("spring:cloud:config:failFast", "True");
        AssertDataValue("spring:cloud:config:env", "environment");
        AssertDataValue("spring:cloud:config:label", "label");
        AssertDataValue("spring:cloud:config:name", "name");
        AssertDataValue("spring:cloud:config:uri", "https://foo.bar/");
        AssertDataValue("spring:cloud:config:username", "username");
        AssertDataValue("spring:cloud:config:password", "password");
        AssertDataValue("spring:cloud:config:token", "vaultToken");
        AssertDataValue("spring:cloud:config:timeout", "75000");
        AssertDataValue("spring:cloud:config:pollingInterval", "00:00:35.5000000");
        AssertDataValue("spring:cloud:config:validateCertificates", "False");
        AssertDataValue("spring:cloud:config:accessTokenUri", "https://token.server.com/");
        AssertDataValue("spring:cloud:config:clientSecret", "client_secret");
        AssertDataValue("spring:cloud:config:clientId", "client_id");
        AssertDataValue("spring:cloud:config:tokenTtl", "2");
        AssertDataValue("spring:cloud:config:tokenRenewRate", "1");
        AssertDataValue("spring:cloud:config:disableTokenRenewal", "True");
        AssertDataValue("spring:cloud:config:retry:enabled", "True");
        AssertDataValue("spring:cloud:config:retry:initialInterval", "8");
        AssertDataValue("spring:cloud:config:retry:maxInterval", "16");
        AssertDataValue("spring:cloud:config:retry:multiplier", "1.1");
        AssertDataValue("spring:cloud:config:retry:maxAttempts", "7");
        AssertDataValue("spring:cloud:config:discovery:enabled", "True");
        AssertDataValue("spring:cloud:config:discovery:serviceId", "my-config-server");
        AssertDataValue("spring:cloud:config:health:enabled", "False");
        AssertDataValue("spring:cloud:config:health:timeToLive", "9");
        AssertDataValue("spring:cloud:config:headers:headerName1", "headerValue1");
        AssertDataValue("spring:cloud:config:headers:headerName2", "headerValue2");

        void AssertDataValue(string key, string expected)
        {
            provider.TryGet(key, out string? value).Should().BeTrue();
            value.Should().Be(expected);
        }
    }

    [Fact]
    public void GetLabels_Null()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }

    [Fact]
    public void GetLabels_Empty()
    {
        var options = new ConfigServerClientOptions
        {
            Label = string.Empty
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }

    [Fact]
    public void GetLabels_SingleString()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "foobar"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("foobar", result[0]);
    }

    [Fact]
    public void GetLabels_MultiString()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "1,2,3,"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public void GetLabels_MultiStringHoles()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "1,,2,3,"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public async Task GetRequestMessage_AddsBasicAuthIfUserNameAndPasswordInURL()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://user:password@localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetRequestMessage_AddsBasicAuthIfUserNameAndPasswordInSettings()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:8888/",
            Name = "foo",
            Environment = "development",
            Username = "user",
            Password = "password"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetRequestMessage_BasicAuthInSettingsOverridesUserNameAndPasswordInURL()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://ignored-1:ignored-2@localhost:8888/",
            Name = "foo",
            Environment = "development",
            Username = "user",
            Password = "password"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetRequestMessage_AddsVaultToken_IfNeeded()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(options.Uri!, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.True(request.Headers.Contains(ConfigServerConfigurationProvider.TokenHeader));
        IEnumerable<string> headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TokenHeader);
        Assert.Contains("MyVaultToken", headerValues);
    }

    [Fact]
    public async Task RefreshVaultToken_Succeeds()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken"
        };

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8888/vault/v1/auth/token/renew-self").WithHeaders("X-Vault-Token", "MyVaultToken")
            .WithContent("{\"increment\":300}").Respond(HttpStatusCode.NoContent);

        using var provider = new ConfigServerConfigurationProvider(options, null, handler, NullLoggerFactory.Instance);
        await provider.RefreshVaultTokenAsync(default);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RefreshVaultToken_With_AccessTokenUri_Succeeds()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken",
            AccessTokenUri = "https://auth.server.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "https://auth.server.com/").WithHeaders("Authorization", "Basic dGVzdC1jbGllbnQtaWQ6dGVzdC1jbGllbnQtc2VjcmV0")
            .WithFormData("grant_type=client_credentials").Respond("application/json", "{ \"access_token\": \"secret\" }");

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8888/vault/v1/auth/token/renew-self").WithHeaders("X-Vault-Token", "MyVaultToken")
            .WithHeaders("Authorization", "Bearer secret").WithContent("{\"increment\":300}").Respond(HttpStatusCode.NoContent);

        using var provider = new ConfigServerConfigurationProvider(options, null, handler, NullLoggerFactory.Instance);
        await provider.RefreshVaultTokenAsync(default);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void GetHttpClient_AddsHeaders_IfConfigured()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Headers =
            {
                { "foo", "bar" },
                { "bar", "foo" }
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        Assert.NotNull(httpClient);
        Assert.Equal("bar", httpClient.DefaultRequestHeaders.GetValues("foo").SingleOrDefault());
        Assert.Equal("foo", httpClient.DefaultRequestHeaders.GetValues("bar").SingleOrDefault());
    }

    [Fact]
    public void IsDiscoveryFirstEnabled_ReturnsExpected()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Discovery =
            {
                Enabled = true
            }
        };

        using (var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance))
        {
            Assert.True(provider.IsDiscoveryFirstEnabled());
        }

        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);

        using (var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance))
        {
            Assert.True(provider.IsDiscoveryFirstEnabled());
        }
    }

    [Fact]
    public void UpdateSettingsFromDiscovery_UpdatesSettingsCorrectly()
    {
        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        provider.UpdateSettingsFromDiscovery(new List<IServiceInstance>(), options);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Equal("http://localhost:8888/", options.Uri);

        var metadata1 = new Dictionary<string, string?>
        {
            { "password", "firstPassword" }
        };

        var metadata2 = new Dictionary<string, string?>
        {
            { "password", "secondPassword" },
            { "user", "secondUser" },
            { "configPath", "configPath" }
        };

        List<IServiceInstance> instances =
        [
            new TestServiceInstance("i1", new Uri("https://foo.bar:8888/"), metadata1),
            new TestServiceInstance("i2", new Uri("https://foo.bar.baz:9999/"), metadata2)
        ];

        provider.UpdateSettingsFromDiscovery(instances, options);
        Assert.Equal("secondUser", options.Username);
        Assert.Equal("secondPassword", options.Password);
        Assert.Equal("https://foo.bar:8888/,https://foo.bar.baz:9999/configPath", options.Uri);
    }

    [Fact]
    public async Task DiscoverServerInstances_FailsFast()
    {
        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" },
            { "spring:cloud:config:failFast", "True" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Timeout = 10
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        var exception = await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.StartsWith("Could not locate Config Server via discovery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reload_And_Bind_Without_Throwing_Exception()
    {
        const string environment = """
            {
              "name": "testname",
              "profiles": [
                "Production"
              ],
              "label": "testlabel",
              "version": "testversion",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "name": "my-app",
                    "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create().UseStartup<TestConfigServerStartup>();

        ConfigServerClientOptions clientOptions = _commonOptions;

        using var server = new TestServer(hostBuilder);
        server.BaseAddress = new Uri(clientOptions.Uri!);

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(clientOptions, null, httpClientHandler, NullLoggerFactory.Instance);

        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.Add(new TestConfigServerConfigurationSource(provider));

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        TestOptions? testOptions = null;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        void ReloadLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                configurationRoot.Reload();
            }
        }

        _ = Task.Run(ReloadLoop, cts.Token);

        while (!cts.IsCancellationRequested)
        {
            testOptions = configurationRoot.Get<TestOptions>();
        }

        Assert.NotNull(testOptions);
        Assert.Equal("my-app", testOptions.Name);
        Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", testOptions.Version);
    }

    private static string GetEncodedUserPassword(string user, string password)
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
    }

    private sealed class SlowHttpClientHandler(TimeSpan sleepTime, HttpResponseMessage responseMessage) : HttpClientHandler
    {
        private readonly TimeSpan _sleepTime = sleepTime;
        private readonly HttpResponseMessage _responseMessage = responseMessage;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_sleepTime, cancellationToken);
            return _responseMessage;
        }
    }

    private sealed class TestServiceInstance(string serviceId, Uri uri, IReadOnlyDictionary<string, string?> metadata) : IServiceInstance
    {
        public string ServiceId { get; } = serviceId;
        public string Host { get; } = uri.Host;
        public int Port { get; } = uri.Port;
        public bool IsSecure { get; } = uri.Scheme == Uri.UriSchemeHttps;
        public Uri Uri { get; } = uri;
        public IReadOnlyDictionary<string, string?> Metadata { get; } = metadata;
    }

    private sealed class TestOptions
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public string? Name { get; set; }
        public string? Version { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
