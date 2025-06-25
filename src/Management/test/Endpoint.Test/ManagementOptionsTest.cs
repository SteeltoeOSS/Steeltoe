// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Json;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementOptionsTest
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }.AddJsonIgnoreEmptyCollection();

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.IsCloudFoundryEnabled.Should().BeTrue();
        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("health");
        options.Exposure.Include.Should().Contain("info");
        options.Exposure.Exclude.Should().BeEmpty();
        options.Enabled.Should().BeTrue();
        options.Path.Should().Be("/actuator");
        options.Port.Should().Be(0);
        options.SslEnabled.Should().BeFalse();
        options.UseStatusCodeFromResponse.Should().BeTrue();
        options.SerializerOptions.Should().BeEquivalentTo(DefaultJsonSerializerOptions);
        options.CustomJsonConverters.Should().BeEmpty();

        options.GetBasePath("/cloudfoundryapplication/info").Should().Be("/cloudfoundryapplication");
        options.GetBasePath("/some/info").Should().Be("/actuator");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:CloudFoundry:Enabled"] = "false",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "services",
            ["Management:Endpoints:Actuator:Exposure:Include:1"] = "loggers",
            ["Management:Endpoints:Actuator:Exposure:Exclude:0"] = "env",
            ["Management:Endpoints:Actuator:Exposure:Exclude:1"] = "mappings",
            ["Management:Endpoints:Enabled"] = "false",
            ["Management:Endpoints:Path"] = "/management",
            ["Management:Endpoints:Port"] = "8080",
            ["Management:Endpoints:SslEnabled"] = "true",
            ["Management:Endpoints:UseStatusCodeFromResponse"] = "false",
            ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true",
            ["Management:Endpoints:CustomJsonConverters:0"] = "Steeltoe.Management.Endpoint.Actuators.Info.EpochSecondsDateTimeConverter"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.IsCloudFoundryEnabled.Should().BeFalse();
        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("services");
        options.Exposure.Include.Should().Contain("loggers");
        options.Exposure.Exclude.Should().HaveCount(2);
        options.Exposure.Exclude.Should().Contain("env");
        options.Exposure.Exclude.Should().Contain("mappings");
        options.Enabled.Should().BeFalse();
        options.Path.Should().Be("/management");
        options.Port.Should().Be(8080);
        options.SslEnabled.Should().BeTrue();
        options.UseStatusCodeFromResponse.Should().BeFalse();

        options.SerializerOptions.Should().BeEquivalentTo(new JsonSerializerOptions(DefaultJsonSerializerOptions)
        {
            WriteIndented = true,
            Converters =
            {
                new EpochSecondsDateTimeConverter()
            }
        });

        options.CustomJsonConverters.Should().ContainSingle();
        options.CustomJsonConverters[0].Should().Be("Steeltoe.Management.Endpoint.Actuators.Info.EpochSecondsDateTimeConverter");

        options.GetBasePath("/cloudfoundryapplication/info").Should().Be("/cloudfoundryapplication");
        options.GetBasePath("/some/info").Should().Be("/management");
    }

    [Fact]
    public async Task Can_set_exposure_with_Spring_syntax()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Web:Exposure:Include"] = "*",
            ["Management:Endpoints:Web:Exposure:Exclude"] = " loggers, mappings "
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.Exposure.Include.Should().ContainSingle();
        options.Exposure.Include[0].Should().Be("*");
        options.Exposure.Exclude.Should().HaveCount(2);
        options.Exposure.Exclude[0].Should().Be("loggers");
        options.Exposure.Exclude[1].Should().Be("mappings");
    }

    [Fact]
    public async Task Merges_exposure_with_Spring_syntax_and_removes_duplicates()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Web:Exposure:Include"] = " httpexchanges ",
            ["Management:Endpoints:Web:Exposure:Exclude"] = " loggers, mappings, ,, mappings ",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "heapdump",
            ["Management:Endpoints:Actuator:Exposure:Include:1"] = "env",
            ["Management:Endpoints:Actuator:Exposure:Exclude:0"] = "loggers",
            ["Management:Endpoints:Actuator:Exposure:Exclude:1"] = "beans"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.Exposure.Include.Should().HaveCount(3);
        options.Exposure.Include[0].Should().Be("httpexchanges");
        options.Exposure.Include[1].Should().Be("heapdump");
        options.Exposure.Include[2].Should().Be("env");
        options.Exposure.Exclude.Should().HaveCount(3);
        options.Exposure.Exclude[0].Should().Be("loggers");
        options.Exposure.Exclude[1].Should().Be("mappings");
        options.Exposure.Exclude[2].Should().Be("beans");
    }

    [Fact]
    public async Task Can_clear_default_exposure()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = string.Empty,
            ["Management:Endpoints:Actuator:Exposure:Exclude:0"] = string.Empty
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.Exposure.Include.Should().BeEmpty();
        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_clear_default_exposure_with_Spring_syntax()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Web:Exposure:Include"] = string.Empty,
            ["Management:Endpoints:Web:Exposure:Exclude"] = string.Empty
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.Exposure.Include.Should().BeEmpty();
        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Fact]
    public async Task Exposure_does_not_bind_from_management_configuration_key()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Exposure:Include:0"] = "httpexchanges",
            ["Management:Endpoints:Exposure:Include:1"] = "dbmigrations",
            ["Management:Endpoints:Exposure:Exclude:0"] = "trace",
            ["Management:Endpoints:Exposure:Exclude:1"] = "env"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions options = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("health");
        options.Exposure.Include.Should().Contain("info");

        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Evaluates_actuator_enabled(bool isManagementEnabled, bool? isActuatorEnabled, bool expected)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Enabled"] = isManagementEnabled.ToString(CultureInfo.InvariantCulture),
            ["Management:Endpoints:Info:Enabled"] = isActuatorEnabled?.ToString(CultureInfo.InvariantCulture)
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions managementOptions = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;
        InfoEndpointOptions endpointOptions = serviceProvider.GetRequiredService<IOptions<InfoEndpointOptions>>().Value;

        endpointOptions.IsEnabled(managementOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null, false, false)]
    [InlineData(null, "refresh", false, false)]
    [InlineData(null, "loggers", false, false)]
    [InlineData(null, "refresh,loggers", false, false)]
    [InlineData(null, "*", false, false)]
    [InlineData("refresh", null, true, false)]
    [InlineData("refresh", "refresh", false, false)]
    [InlineData("refresh", "loggers", true, false)]
    [InlineData("refresh", "refresh,loggers", false, false)]
    [InlineData("refresh", "*", false, false)]
    [InlineData("loggers", null, false, true)]
    [InlineData("loggers", "refresh", false, true)]
    [InlineData("loggers", "loggers", false, false)]
    [InlineData("loggers", "refresh,loggers", false, false)]
    [InlineData("loggers", "*", false, false)]
    [InlineData("refresh,loggers", null, true, true)]
    [InlineData("refresh,loggers", "refresh", false, true)]
    [InlineData("refresh,loggers", "loggers", true, false)]
    [InlineData("refresh,loggers", "refresh,loggers", false, false)]
    [InlineData("refresh,loggers", "*", false, false)]
    [InlineData("*", null, true, true)]
    [InlineData("*", "refresh", false, true)]
    [InlineData("*", "loggers", true, false)]
    [InlineData("*", "refresh,loggers", false, false)]
    [InlineData("*", "*", false, false)]
    public async Task Evaluates_actuator_exposure(string? include, string? exclude, bool refreshExposed, bool loggersExposed)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Web:Exposure:Include"] = include,
            ["Management:Endpoints:Web:Exposure:Exclude"] = exclude
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddRefreshActuator();
        services.AddLoggersActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions managementOptions = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;
        RefreshEndpointOptions refreshEndpointOptions = serviceProvider.GetRequiredService<IOptions<RefreshEndpointOptions>>().Value;
        LoggersEndpointOptions loggersEndpointOptions = serviceProvider.GetRequiredService<IOptions<LoggersEndpointOptions>>().Value;

        refreshEndpointOptions.IsExposed(managementOptions).Should().Be(refreshExposed);
        loggersEndpointOptions.IsExposed(managementOptions).Should().Be(loggersExposed);
    }
}
