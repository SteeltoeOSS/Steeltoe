// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class AppUrlCalculatorTest
{
    private const string ListenNonSecurePort1 = "3333";
    private const string ListenNonSecurePort2 = "4444";
    private const string ListenNonSecurePort3 = "5555";
    private const string ListenSecurePort1 = "6666";
    private const string ListenSecurePort2 = "7777";
    private const string ManagementPort = "8888";
    private const string OverriddenPort = "9999";

    [Fact]
    public void Selects_default_binding_when_nothing_configured()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://localhost:5000/");
    }

    [Fact]
    public void Prefers_https_binding_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};https://dontcare:{ListenSecurePort2}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{ListenSecurePort1}/");
    }

    [Fact]
    public void Selects_http_binding_when_scheme_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};http://dontcare:{ListenNonSecurePort2}",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "http"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{FakeDomainNameResolver.TestHostName}:{ListenNonSecurePort1}/");
    }

    [Fact]
    public void Selects_https_binding_when_scheme_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1};https://dontcare:{ListenSecurePort2}",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{ListenSecurePort1}/");
    }

    [Fact]
    public void Uses_scheme_and_port_number_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https",
            ["Spring:Boot:Admin:Client:BasePort"] = "7890"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:7890/");
    }

    [Fact]
    public void Uses_port_number_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://dontcare:{ListenNonSecurePort2};https://dontcare:{ListenSecurePort1}",
            ["Spring:Boot:Admin:Client:BasePort"] = OverriddenPort
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{OverriddenPort}/");
    }

    [Fact]
    public void Uses_non_secure_management_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{FakeDomainNameResolver.TestHostName}:{ManagementPort}/");
    }

    [Fact]
    public void Uses_secure_management_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Management:Endpoints:SslEnabled"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{ManagementPort}/");
    }

    [Fact]
    public void Uses_non_secure_management_port_with_configured_scheme()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Spring:Boot:Admin:Client:BaseScheme"] = "https"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{ManagementPort}/");
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

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{FakeDomainNameResolver.TestHostName}:{ManagementPort}/");
    }

    [Fact]
    public void Uses_non_secure_management_port_with_configured_port_number()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Port"] = ManagementPort,
            ["Spring:Boot:Admin:Client:BasePort"] = OverriddenPort
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{FakeDomainNameResolver.TestHostName}:{OverriddenPort}/");
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

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{OverriddenPort}/");
    }

    [Fact]
    public void Uses_hostname_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BaseHost"] = "test.host.com"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://test.host.com:5000/");
    }

    [Fact]
    public void Selects_localhost_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://localhost:{ListenNonSecurePort2};http://10.20.30.40:{ListenNonSecurePort3}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://localhost:{ListenNonSecurePort2}/");
    }

    [Fact]
    public void Selects_IP_address_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://10.20.30.40:{ListenNonSecurePort2};http://localhost:{ListenNonSecurePort3}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://10.20.30.40:{ListenNonSecurePort2}/");
    }

    [Fact]
    public void Uses_hostname_from_InetUtils()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://dontcare:{ListenNonSecurePort2}",
            ["Spring:Boot:Admin:Client:UseNetworkInterfaces"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://{FakeNetworkInterfaceProvider.TestHostName}:{ListenNonSecurePort1}/");
    }

    [Fact]
    public void Uses_IP_address_from_DomainNameResolver()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"https://dontcare:{ListenSecurePort1};https://dontcare:{ListenSecurePort2}",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestIPAddress}:{ListenSecurePort1}/");
    }

    [Fact]
    public void Uses_IP_address_from_InetUtils()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};https://dontcare:{ListenSecurePort1}",
            ["Spring:Boot:Admin:Client:UseNetworkInterfaces"] = "true",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeNetworkInterfaceProvider.TestIPAddress}:{ListenSecurePort1}/");
    }

    [Fact]
    public void Uses_path_when_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BasePath"] = "api"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://localhost:5000/api");
    }

    [Fact]
    public void Prefers_IP_address_over_wildcard_host_when_multiple_urls_configured_with_PreferIPAddress()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1};http://10.20.30.40:{ListenNonSecurePort2};http://localhost:{ListenNonSecurePort3}",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://10.20.30.40:{ListenNonSecurePort2}/");
    }

    [Fact]
    public void Prefers_IP_address_over_https_when_multiple_urls_configured_with_PreferIPAddress()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"https://dontcare:{ListenSecurePort1};http://10.20.30.40:{ListenNonSecurePort1};https://localhost:{ListenSecurePort2}",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://10.20.30.40:{ListenNonSecurePort1}/");
    }

    [Fact]
    public void Prefers_https_over_localhost_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"https://dontcare:{ListenSecurePort1};http://localhost:{ListenNonSecurePort1};http://10.20.30.40:{ListenNonSecurePort2}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"https://{FakeDomainNameResolver.TestHostName}:{ListenSecurePort1}/");
    }

    [Fact]
    public void Prefers_localhost_over_wildcard_host_when_multiple_urls_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://localhost:{ListenNonSecurePort2};http://dontcare:{ListenNonSecurePort1}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be($"http://localhost:{ListenNonSecurePort2}/");
    }

    [Fact]
    public void Unable_when_no_bindings_available()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var fakeServer = (FakeServer)serviceProvider.GetRequiredService<IServer>();
        fakeServer.Features.Get<IServerAddressesFeature>()!.Addresses.Clear();

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().BeNull();
    }

    [Fact]
    public void Unable_when_invalid_host_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BaseHost"] = "host:name"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().BeNull();
    }

    [Fact]
    public void Unable_when_hostname_lookup_fails()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1}"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var fakeDomainNameResolver = (FakeDomainNameResolver)serviceProvider.GetRequiredService<IDomainNameResolver>();
        fakeDomainNameResolver.ReturnsNull = true;

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().BeNull();
    }

    [Fact]
    public void Unable_when_IP_address_lookup_fails()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://dontcare:{ListenNonSecurePort1}",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var fakeDomainNameResolver = (FakeDomainNameResolver)serviceProvider.GetRequiredService<IDomainNameResolver>();
        fakeDomainNameResolver.ReturnsNull = true;

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().BeNull();
    }

    [Fact]
    public void Escapes_special_characters_in_configuration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BasePath"] = "path???/some"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://localhost:5000/path%3F%3F%3F/some");
    }

    [Fact]
    public void Preserves_escaped_special_characters_in_configuration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:BasePath"] = "path%3F%3F%3F/some"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using ServiceProvider serviceProvider = BuildServiceProvider(configurationRoot);

        var calculator = serviceProvider.GetRequiredService<AppUrlCalculator>();
        var options = serviceProvider.GetRequiredService<IOptions<SpringBootAdminClientOptions>>();
        string? url = calculator.AutoDetectAppUrl(options.Value);

        url.Should().Be("http://localhost:5000/path%3F%3F%3F/some");
    }

    private static ServiceProvider BuildServiceProvider(IConfigurationRoot configurationRoot)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddSingleton<IServer, FakeServer>();
        services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        services.AddSingleton<INetworkInterfaceProvider, FakeNetworkInterfaceProvider>();
        services.AddSpringBootAdminClient();
        services.RemoveAll<IHostedService>();

        return services.BuildServiceProvider(true);
    }

    private sealed class FakeNetworkInterfaceProvider : INetworkInterfaceProvider
    {
        public const string TestIPAddress = "10.11.12.13";
        public const string TestHostName = "inet-host-name";

        private static readonly NetworkInterfaceSnapshot Snapshot = new("eth0", "fake-id", true, false, 1, [IPAddress.Parse(TestIPAddress)]);

        public IReadOnlyList<NetworkInterfaceSnapshot> GetAllNetworkInterfaces()
        {
            return [Snapshot];
        }

        public string ResolveHostName(IPAddress address)
        {
            return TestHostName;
        }
    }
}
