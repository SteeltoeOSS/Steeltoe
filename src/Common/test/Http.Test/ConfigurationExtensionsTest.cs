// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Http.Test;

public sealed class ConfigurationExtensionsTest
{
    [Fact]
    public void Detects_addresses_from_UseUrls()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.WebHost.UseUrls("http://localhost:8888", "https://localhost:9999");

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://localhost:8888",
            "https://localhost:9999"
        ]);
    }

    [Fact]
    public void Detects_URLS_in_configuration()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["urls"] = "http://localhost:8888;https://127.0.0.1:9999"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://localhost:8888",
            "https://127.0.0.1:9999"
        ]);
    }

    [Fact]
    public void Detects_PORTS_in_configuration()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["http_ports"] = "5555",
            ["https_ports"] = "6666"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://*:5555",
            "https://*:6666"
        ]);
    }

    [Fact]
    public void Ignores_PORTS_when_URLS_present_in_configuration()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["urls"] = "http://[::1]:8888;https://192.168.1.1:9999",
            ["http_ports"] = "5555",
            ["https_ports"] = "6666"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://[::1]:8888",
            "https://192.168.1.1:9999"
        ]);
    }

    [Fact]
    public void Detects_URLS_in_Kestrel_configuration()
    {
        const string appSettings = """
            {
              "Kestrel": {
                "Endpoints": {
                  "PrimaryEndpoint": {
                    "Url": "http://localhost:5555"
                  },
                  "SecondaryEndpoint": {
                    "Url": "https://+:6666"
                  },
                  "TertiaryEndpoint": {
                    "Url": "https://api.domain.org:7777"
                  }
                }
              }
            }
            """;

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://localhost:5555",
            "https://+:6666",
            "https://api.domain.org:7777"
        ]);
    }

    [Fact]
    public void Ignore_URLS_when_Kestrel_present_in_configuration()
    {
        const string appSettings = """
            {
              "Urls": "https://localhost:9999",
              "Kestrel": {
                "Endpoints": {
                  "PrimaryEndpoint": {
                    "Url": "http://localhost:5555"
                  }
                }
              }
            }
            """;

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo(["http://localhost:5555"]);
    }

    [Fact]
    public void Detects_URLS_in_environment_variable()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_URLS", "http://+:8888;https://some.domain.org:9999");

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://+:8888",
            "https://some.domain.org:9999"
        ]);
    }

    [Fact]
    public void Detects_PORTS_in_environment_variables()
    {
        using var httpScope = new EnvironmentVariableScope("ASPNETCORE_HTTP_PORTS", "6666");
        using var httpsScope = new EnvironmentVariableScope("ASPNETCORE_HTTPS_PORTS", "7777");

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://*:6666",
            "https://*:7777"
        ]);
    }

    [Fact]
    public void Detects_URLS_in_command_line_argument()
    {
        string[] args =
        [
            "--urls",
            "http://*:7777;http://localhost:8888;https://*:9999"
        ];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://*:7777",
            "http://localhost:8888",
            "https://*:9999"
        ]);
    }

    [Fact]
    public void Detects_PORTS_in_command_line_arguments()
    {
        string[] args =
        [
            "--http_ports",
            "6666",
            "--https_ports",
            "7777"
        ];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo([
            "http://*:6666",
            "https://*:7777"
        ]);
    }

    [Fact]
    public void Detects_nothing_configured()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo(["http://localhost:5000"]);
    }

    [Fact]
    public void Does_not_detect_addresses_from_WebApplication_Urls()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        using WebApplication app = builder.Build();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        ICollection<string> addresses = configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo(["http://localhost:5000"]);
    }

    [Fact]
    public void Does_not_detect_address_in_Kestrel_action()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 8888);
            options.ListenLocalhost(9999, listenOptions => listenOptions.UseHttps());
        });

        ICollection<string> addresses = builder.Configuration.GetListenAddresses();

        addresses.Should().BeEquivalentTo(["http://localhost:5000"]);
    }
}
