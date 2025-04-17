// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class AppUrlCalculatorTest
{
    private const string ListenNonSecurePort1 = "4444";
    private const string ListenNonSecurePort2 = "5555";
    private const string ListenSecurePort1 = "6666";
    private const string ListenSecurePort2 = "7777";
    private const string ManagementPort = "8888";
    private const string OverriddenPort = "9999";

    private static readonly Lazy<string> LazySystemHostName = new(() =>
#pragma warning disable S4040 // Strings should be normalized to uppercase
            DnsTools.ResolveHostName()?.ToLowerInvariant() ?? throw new InvalidOperationException("Hostname is unavailable for running tests."),
#pragma warning restore S4040 // Strings should be normalized to uppercase
        LazyThreadSafetyMode.None);

    [Fact]
    public void Selects_default_binding_when_nothing_configured()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:5000");
    }

    [Fact]
    public void Prefers_https_binding_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};https://dontcare:{ListenSecurePort2}"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{ListenSecurePort1}");
    }

    [Fact]
    public void Selects_http_binding_when_scheme_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};http://dontcare:{ListenNonSecurePort2}",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "http"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:{ListenNonSecurePort1}");
    }

    [Fact]
    public void Selects_https_binding_when_scheme_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};https://dontcare:{ListenSecurePort2}",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{ListenSecurePort1}");
    }

    [Fact]
    public void Uses_scheme_and_port_number_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https",
            ["Spring:Boot:Admin:Client:BasePort"] = "7890"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:7890");
    }

    [Fact]
    public void Uses_port_number_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://dontcare:{ListenNonSecurePort2};https://dontcare:{ListenSecurePort1}",
            ["Spring:Boot:Admin:Client:BasePort"] = OverriddenPort
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{OverriddenPort}");
    }

    [Fact]
    public void Uses_non_secure_management_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:{ManagementPort}");
    }

    [Fact]
    public void Uses_secure_management_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Management:Endpoints:SslEnabled"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{ManagementPort}");
    }

    [Fact]
    public void Uses_non_secure_management_port_with_configured_scheme()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{ManagementPort}");
    }

    [Fact]
    public void Uses_secure_management_port_with_configured_scheme()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Management:Endpoints:SslEnabled"] = "true",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "http"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:{ManagementPort}");
    }

    [Fact]
    public void Uses_non_secure_management_port_with_configured_port_number()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Spring:Boot:Admin:Client:BasePort"] = OverriddenPort
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:{OverriddenPort}");
    }

    [Fact]
    public void Uses_secure_management_port_with_configured_port_number()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Management:Endpoints:SslEnabled"] = "true",
            ["Spring:Boot:Admin:Client:BasePort"] = OverriddenPort
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{LazySystemHostName.Value}:{OverriddenPort}");
    }

    [Fact]
    public void Uses_hostname_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BaseHost"] = "test.host.com"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://test.host.com:5000");
    }

    [Fact]
    public void Uses_hostname_from_InetUtils()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:UseNetworkInterfaces"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://inet-host-name:5000");
    }

    [Fact]
    public void Uses_IP_address_from_InetUtils()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:UseNetworkInterfaces"] = "true",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://10.11.12.13:5000");
    }

    [Fact]
    public void Uses_path_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BasePath"] = "api"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{LazySystemHostName.Value}:5000/api");
    }

    [Fact]
    public void Unable_when_no_bindings_available()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configuration);

        var fakeServer = (FakeServer)serviceProvider.GetRequiredService<IServer>();
        fakeServer.Features.Get<IServerAddressesFeature>()!.Addresses.Clear();

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().BeNull();
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton(configuration);
        services.AddOptions();
        services.AddSingleton<IConfigureOptions<SpringBootAdminClientOptions>, ConfigureSpringBootAdminClientOptions>();
        services.AddSingleton<IConfigureOptions<ManagementOptions>, ConfigureManagementOptions>();
        services.AddLogging();
        services.AddSingleton<IServer, FakeServer>();
        services.AddSingleton<InetUtils, FakeInetUtils>();
        services.AddSingleton<AppUrlCalculator>();

        return services.BuildServiceProvider(true);
    }

    private sealed class FakeInetUtils(IOptionsMonitor<InetOptions> optionsMonitor, ILogger<InetUtils> logger)
        : InetUtils(optionsMonitor, logger)
    {
        public override HostInfo FindFirstNonLoopbackHostInfo()
        {
            return new HostInfo("inet-host-name", "10.11.12.13");
        }
    }
}
