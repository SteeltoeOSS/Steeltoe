// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net.Http.Json;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationProviderTest
{
    private readonly ConfigServerClientSettings _commonSettings = new()
    {
        Name = "myName"
    };

    [Fact]
    public void SettingsConstructor__ThrowsIfSettingsNull()
    {
        const ConfigServerClientSettings settings = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance));
        Assert.Contains(nameof(settings), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsConstructor__ThrowsIfHttpClientNull()
    {
        var settings = new ConfigServerClientSettings();
        const HttpClient httpClient = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient, NullLoggerFactory.Instance));
        Assert.Contains(nameof(httpClient), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsConstructor__WithLoggerFactorySucceeds()
    {
        var logFactory = new LoggerFactory();
        var settings = new ConfigServerClientSettings();

        var provider = new ConfigServerConfigurationProvider(settings, logFactory);
        Assert.NotNull(provider.Logger);
    }

    [Fact]
    public void DefaultConstructor_InitializedWithDefaultSettings()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        TestHelper.VerifyDefaults(provider.Settings);
    }

    [Fact]
    public void SourceConstructor_WithDefaults_InitializesWithDefaultSettings()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var settings = new ConfigServerClientSettings();
        var source = new ConfigServerConfigurationSource(settings, configuration, NullLoggerFactory.Instance);
        var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        TestHelper.VerifyDefaults(provider.Settings);
    }

    [Fact]
    public void GetConfigServerUri_NoBaseUri_Throws()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        Assert.Throws<ArgumentNullException>(() => provider.GetConfigServerUri(null, null));
    }

    [Fact]
    public void GetConfigServerUri_NoLabel()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], null);
        Assert.Equal($"{settings.RawUris[0]}{settings.Name}/{settings.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabel()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], settings.Label);
        Assert.Equal($"{settings.RawUris[0]}{settings.Name}/{settings.Environment}/{settings.Label}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabelContainingSlash()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel/version"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], settings.Label);
        Assert.Equal($"{settings.RawUris[0]}{settings.Name}/{settings.Environment}/myLabel(_)version", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "http://localhost:9999/myPath/path/",
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], null);
        Assert.Equal($"http://localhost:9999/myPath/path/{settings.Name}/{settings.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo_NoEndingSlash()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "http://localhost:9999/myPath/path",
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], null);
        Assert.Equal($"http://localhost:9999/myPath/path/{settings.Name}/{settings.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_NoEndingSlash()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "http://localhost:9999",
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], null);
        Assert.Equal($"http://localhost:9999/{settings.Name}/{settings.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithEndingSlash()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "http://localhost:9999/",
            Name = "myName",
            Environment = "Production"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string path = provider.GetConfigServerUri(settings.RawUris[0], null);
        Assert.Equal($"http://localhost:9999/{settings.Name}/{settings.Environment}", path);
    }

    [Fact]
    public async Task Deserialize_GoodJsonAsync()
    {
        var environment = new ConfigEnvironment
        {
            Name = "testname",
            Label = "testlabel",
            Profiles = new List<string>
            {
                "Production"
            },
            Version = "testversion",
            State = "teststate",
            PropertySources = new List<PropertySource>
            {
                new()
                {
                    Name = "source",
                    Source = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 10 }
                    }
                }
            }
        };

        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);
        var content = JsonContent.Create(environment);

        var env = await content.ReadFromJsonAsync<ConfigEnvironment>(provider.SerializerOptions);
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
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertArray_NotArrayValue()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertArrayKey("foobar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void ConvertArray_NotArrayValue2()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertArrayKey("foobar[bar]");
        Assert.Equal("foobar[bar]", result);
    }

    [Fact]
    public void ConvertArray_WithArrayValue()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertArrayKey("foobar[1234]");
        Assert.Equal("foobar:1234", result);
    }

    [Fact]
    public void ConvertArray_WithArrayArrayValue()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertArrayKey("foobar[1234][5678]");
        Assert.Equal("foobar:1234:5678", result);
    }

    [Fact]
    public void ConvertArray_WithArrayArrayNotAtEnd()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertArrayKey("foobar[1234][5678]barbar");
        Assert.Equal("foobar[1234][5678]barbar", result);
    }

    [Fact]
    public void ConvertKey_WithArrayArrayValue()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertKey("a.b.foobar[1234][5678].barfoo.boo[123]");
        Assert.Equal("a:b:foobar:1234:5678:barfoo:boo:123", result);
    }

    [Fact]
    public void ConvertKey_WithEscapedDot()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string result = provider.ConvertKey(@"a.b\.foobar");
        Assert.Equal("a:b.foobar", result);
    }

    [Fact]
    public async Task RemoteLoadAsync_InvalidUri()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<UriFormatException>(async () => await provider.RemoteLoadAsync(new[]
        {
            "foobar\\foobar\\"
        }, null, CancellationToken.None));
    }

    [Fact]
    public async Task RemoteLoadAsync_HostTimesOut()
    {
        var messageHandler = new SlowHttpMessageHandler(1.Seconds(), new HttpResponseMessage());

        var httpClient = new HttpClient(messageHandler)
        {
            Timeout = 100.Milliseconds()
        };

        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, httpClient, NullLoggerFactory.Instance);

        Func<Task> action = async () => await provider.RemoteLoadAsync(new[]
        {
            "http://localhost:9999/app/profile"
        }, null, CancellationToken.None);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerException<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            500
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment("testing");

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(async () => await provider.RemoteLoadAsync(settings.GetUris(), null, CancellationToken.None));

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            204
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        ConfigEnvironment result = await provider.RemoteLoadAsync(settings.RawUris, null, CancellationToken.None);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithPollingTimer()
    {
        // Arrange
        const string environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [

                    ]
                }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        TestConfigServerStartup.ReturnStatus = Enumerable.Repeat(200, 100).ToArray();
        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            PollingInterval = TimeSpan.FromMilliseconds(300),
            Label = "label,testlabel"
        };

        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);
        Assert.True(TestConfigServerStartup.InitialRequestLatch.Wait(TimeSpan.FromSeconds(60)));
        Assert.True(TestConfigServerStartup.RequestCount >= 1);
        await Task.Delay(1000);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(TestConfigServerStartup.RequestCount >= 2);
        Assert.False(provider.GetReloadToken().HasChanged);
    }

    [Fact]
    public async Task DoLoad_MultipleLabels_ChecksAllLabels()
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
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        TestConfigServerStartup.ReturnStatus = new[]
        {
            404,
            200
        };

        TestConfigServerStartup.Label = "testlabel";
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.Label = "label,testlabel";
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await provider.DoLoadAsync(true, CancellationToken.None);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal(2, TestConfigServerStartup.RequestCount);
        Assert.Equal($"/{settings.Name}/{settings.Environment}/testlabel", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGood()
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
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        ConfigEnvironment env = await provider.RemoteLoadAsync(settings.GetUris(), null, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
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
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsGreaterThanEqualBadRequest_StopsChecking()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            500,
            200
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.Uri = "http://localhost:8888, http://localhost:8888";
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            404,
            200
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.Uri = "http://localhost:8888, http://localhost:8888";
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            404
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(26, provider.Properties.Count);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            404
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.FailFast = true;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
    {
        ConfigServerClientSettings settings = _commonSettings;
        settings.FailFast = true;
        settings.Uri = "http://localhost:8888,http://localhost:8888";
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            404,
            200
        };

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            500
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.FailFast = true;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            500,
            500,
            500
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        settings.FailFast = true;
        settings.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = new[]
        {
            500,
            500,
            500,
            500,
            500,
            500
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            FailFast = true,
            RetryEnabled = true,
            RetryInitialInterval = 10,
            Timeout = 10
        };

        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.Equal(6, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ChangesDataDictionary()
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
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, CancellationToken.None);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{settings.Name}/{settings.Environment}", TestConfigServerStartup.LastRequest.Path.Value);

        Assert.True(provider.TryGet("key1", out string value));
        Assert.Equal("value1", value);
        Assert.True(provider.TryGet("key2", out value));
        Assert.Equal("10", value);
    }

    [Fact]
    public void ReLoad_DataDictionary_With_New_Configurations()
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
                                            ""featureToggles.ShowModule[0]"": ""FT1"",
                                            ""featureToggles.ShowModule[1]"": ""FT2"",
                                            ""featureToggles.ShowModule[2]"": ""FT3"",
                                            ""enableSettings"":""true""
                                    }
                            }
                        ]
                    }";

        IHostEnvironment hostEnvironment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostEnvironment.EnvironmentName);

        using var server = new TestServer(builder);
        server.BaseAddress = new Uri(ConfigServerClientSettings.DefaultUri);

        ConfigServerClientSettings settings = _commonSettings;
        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        provider.Load();
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out string value));
        Assert.Equal("FT1", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:1", out value));
        Assert.Equal("FT2", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:2", out value));
        Assert.Equal("FT3", value);
        Assert.True(provider.TryGet("enableSettings", out value));
        Assert.Equal("true", value);

        TestConfigServerStartup.Reset();

        TestConfigServerStartup.Response = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [
                        {
                            ""name"": ""source"",
                            ""source"": {
                                ""featureToggles.ShowModule[0]"": ""none""
                            }
                        }
                    ]
                }";

        provider.Load();
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out string val));
        Assert.Equal("none", val);
        Assert.False(provider.TryGet("featureToggles:ShowModule:1", out _));
        Assert.False(provider.TryGet("featureToggles:ShowModule:2", out _));
        Assert.False(provider.TryGet("enableSettings", out _));
    }

    [Fact]
    public void AddConfigServerClientSettings_ChangesDataDictionary()
    {
        var settings = new ConfigServerClientSettings
        {
            AccessTokenUri = "https://foo.bar/",
            ClientId = "client_id",
            ClientSecret = "client_secret",
            Enabled = true,
            Environment = "environment",
            FailFast = false,
            Label = "label",
            Name = "name",
            Password = "password",
            Uri = "https://foo.bar/",
            Username = "username",
            ValidateCertificates = false,
            Token = "vaulttoken",
            TokenRenewRate = 1,
            TokenTtl = 2,
            RetryMultiplier = 1.1
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);
        CultureInfo initialCulture = GetAndSetCurrentCulture(new CultureInfo("ru-RU"));

        try
        {
            provider.AddConfigServerClientSettings();

            Assert.True(provider.TryGet("spring:cloud:config:access_token_uri", out string value));
            Assert.Equal("https://foo.bar/", value);
            Assert.True(provider.TryGet("spring:cloud:config:client_id", out value));
            Assert.Equal("client_id", value);
            Assert.True(provider.TryGet("spring:cloud:config:client_secret", out value));
            Assert.Equal("client_secret", value);
            Assert.True(provider.TryGet("spring:cloud:config:env", out value));
            Assert.Equal("environment", value);
            Assert.True(provider.TryGet("spring:cloud:config:label", out value));
            Assert.Equal("label", value);
            Assert.True(provider.TryGet("spring:cloud:config:name", out value));
            Assert.Equal("name", value);
            Assert.True(provider.TryGet("spring:cloud:config:password", out value));
            Assert.Equal("password", value);
            Assert.True(provider.TryGet("spring:cloud:config:uri", out value));
            Assert.Equal("https://foo.bar/", value);
            Assert.True(provider.TryGet("spring:cloud:config:username", out value));
            Assert.Equal("username", value);

            Assert.True(provider.TryGet("spring:cloud:config:enabled", out value));
            Assert.Equal("True", value);
            Assert.True(provider.TryGet("spring:cloud:config:failFast", out value));
            Assert.Equal("False", value);
            Assert.True(provider.TryGet("spring:cloud:config:validate_certificates", out value));
            Assert.Equal("False", value);
            Assert.True(provider.TryGet("spring:cloud:config:token", out value));
            Assert.Equal("vaulttoken", value);
            Assert.True(provider.TryGet("spring:cloud:config:timeout", out value));
            Assert.Equal("60000", value);
            Assert.True(provider.TryGet("spring:cloud:config:tokenRenewRate", out value));
            Assert.Equal("1", value);
            Assert.True(provider.TryGet("spring:cloud:config:tokenTtl", out value));
            Assert.Equal("2", value);
            Assert.True(provider.TryGet("spring:cloud:config:discovery:enabled", out value));
            Assert.Equal("False", value);
            Assert.True(provider.TryGet("spring:cloud:config:discovery:serviceId", out value));
            Assert.Equal(ConfigServerClientSettings.DefaultConfigserverServiceId, value);
            Assert.True(provider.TryGet("spring:cloud:config:retry:multiplier", out value));
            Assert.Equal("1.1", value);
        }
        finally
        {
            GetAndSetCurrentCulture(initialCulture);
        }
    }

    [Fact]
    public void GetLabels_Null()
    {
        var settings = new ConfigServerClientSettings();
        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }

    [Fact]
    public void GetLabels_Empty()
    {
        var settings = new ConfigServerClientSettings
        {
            Label = string.Empty
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }

    [Fact]
    public void GetLabels_SingleString()
    {
        var settings = new ConfigServerClientSettings
        {
            Label = "foobar"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("foobar", result[0]);
    }

    [Fact]
    public void GetLabels_MultiString()
    {
        var settings = new ConfigServerClientSettings
        {
            Label = "1,2,3,"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

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
        var settings = new ConfigServerClientSettings
        {
            Label = "1,,2,3,"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public async Task GetRequestMessage_AddsBasicAuthIfPassword()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "http://user:password@localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        var requestUri = new Uri(provider.GetConfigServerUri(settings.RawUris[0], null));
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, "user", "password", CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(provider.GetEncoded("user", "password"), request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetRequestMessage_AddsVaultToken_IfNeeded()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken"
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);

        var requestUri = new Uri(provider.GetConfigServerUri(settings.RawUris[0], null));
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, null, null, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.True(request.Headers.Contains(ConfigServerConfigurationProvider.TokenHeader));
        IEnumerable<string> headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TokenHeader);
        Assert.Contains("MyVaultToken", headerValues);
    }

    [Fact]
    public void GetHttpClient_AddsHeaders_IfConfigured()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "foo",
            Environment = "development",
            Headers =
            {
                { "foo", "bar" },
                { "bar", "foo" }
            }
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);
        HttpClient httpClient = provider.HttpClient;

        Assert.Equal("bar", httpClient.DefaultRequestHeaders.GetValues("foo").SingleOrDefault());
        Assert.Equal("foo", httpClient.DefaultRequestHeaders.GetValues("bar").SingleOrDefault());
    }

    [Fact]
    public void IsDiscoveryFirstEnabled_ReturnsExpected()
    {
        var settings = new ConfigServerClientSettings
        {
            Name = "foo",
            Environment = "development",
            DiscoveryEnabled = true
        };

        var provider = new ConfigServerConfigurationProvider(settings, NullLoggerFactory.Instance);
        Assert.True(provider.IsDiscoveryFirstEnabled());

        var values = new Dictionary<string, string>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        settings = new ConfigServerClientSettings
        {
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(settings, configuration, NullLoggerFactory.Instance);
        provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        Assert.True(provider.IsDiscoveryFirstEnabled());
    }

    [Fact]
    public void UpdateSettingsFromDiscovery_UpdatesSettingsCorrectly()
    {
        var values = new Dictionary<string, string>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var settings = new ConfigServerClientSettings
        {
            Uri = "http://localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(settings, configuration, NullLoggerFactory.Instance);
        var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        provider.UpdateSettingsFromDiscovery(new List<IServiceInstance>(), settings);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
        Assert.Equal("http://localhost:8888/", settings.Uri);

        var metadata1 = new Dictionary<string, string>
        {
            { "password", "firstPassword" }
        };

        var metadata2 = new Dictionary<string, string>
        {
            { "password", "secondPassword" },
            { "user", "secondUser" },
            { "configPath", "configPath" }
        };

        var instances = new List<IServiceInstance>
        {
            new TestServiceInfo(new Uri("https://foo.bar:8888/"), metadata1),
            new TestServiceInfo(new Uri("https://foo.bar.baz:9999/"), metadata2)
        };

        provider.UpdateSettingsFromDiscovery(instances, settings);
        Assert.Equal("secondUser", settings.Username);
        Assert.Equal("secondPassword", settings.Password);
        Assert.Equal("https://foo.bar:8888/,https://foo.bar.baz:9999/configPath", settings.Uri);
    }

    [Fact]
    public async Task DiscoverServerInstances_FailsFast()
    {
        var values = new Dictionary<string, string>
        {
            { "spring:cloud:config:discovery:enabled", "True" },
            { "spring:cloud:config:failFast", "True" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var settings = new ConfigServerClientSettings
        {
            Name = "foo",
            Environment = "development",
            Timeout = 10
        };

        var source = new ConfigServerConfigurationSource(settings, configuration, NullLoggerFactory.Instance);
        var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        var exception = await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.StartsWith("Could not locate Config Server via discovery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reload_And_Bind_Without_Throwing_Exception()
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
                                ""name"": ""my-app"",
                                ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                            }
                        }
                    ]
                }";

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        IHostEnvironment hostingEnvironment = HostingHelpers.GetHostingEnvironment();
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(hostingEnvironment.EnvironmentName);

        ConfigServerClientSettings settings = _commonSettings;

        using var server = new TestServer(hostBuilder);
        server.BaseAddress = new Uri(settings.Uri);

        using HttpClient client = server.CreateClient();
        var provider = new ConfigServerConfigurationProvider(settings, client, NullLoggerFactory.Instance);

        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.Add(new TestConfigServerConfigurationSource(provider));

        IConfigurationRoot configuration = configurationBuilder.Build();

        TestOptions options = null;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        void ReloadLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                configuration.Reload();
            }
        }

        _ = Task.Run(ReloadLoop, cts.Token);

        while (!cts.IsCancellationRequested)
        {
            options = configuration.Get<TestOptions>();
        }

        _ = nameof(TestOptions.Name);
        _ = nameof(TestOptions.Version);

        Assert.Equal("my-app", options.Name);
        Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
    }

    private static CultureInfo GetAndSetCurrentCulture(CultureInfo newCulture)
    {
        var oldCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = newCulture;
        return oldCulture;
    }

    private sealed class SlowHttpMessageHandler : HttpMessageHandler
    {
        private readonly TimeSpan _sleepTime;
        private readonly HttpResponseMessage _responseMessage;

        public SlowHttpMessageHandler(TimeSpan sleepTime, HttpResponseMessage responseMessage)
        {
            _sleepTime = sleepTime;
            _responseMessage = responseMessage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_sleepTime, cancellationToken);
            return _responseMessage;
        }
    }

    private sealed class TestServiceInfo : IServiceInstance
    {
        public string ServiceId => throw new NotImplementedException();

        public string Host => throw new NotImplementedException();

        public int Port => throw new NotImplementedException();

        public bool IsSecure => throw new NotImplementedException();

        public Uri Uri { get; }

        public IDictionary<string, string> Metadata { get; }

        public TestServiceInfo(Uri uri, IDictionary<string, string> metadata)
        {
            Uri = uri;
            Metadata = metadata;
        }
    }

    private sealed class TestOptions
    {
#pragma warning disable S3459 // Unassigned members should be removed
        public string Name { get; set; }

        public string Version { get; set; }
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
