// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint.Actuators.All;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class PostConfigureEurekaInstanceOptionsTest
{
    [Fact]
    public async Task Applies_defaults_when_not_configured()
    {
        string? hostName = DnsTools.ResolveHostName();
        string? ipAddress = DnsTools.ResolveHostAddress(hostName!);
        string appName = Assembly.GetEntryAssembly()!.GetName().Name!;

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(null);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.InstanceId.Should().Be($"{hostName}:{appName}:{5000}");
        instanceOptions.AppName.Should().Be(appName);
        instanceOptions.AppGroupName.Should().BeNull();
        instanceOptions.MetadataMap.Should().BeEmpty();
        instanceOptions.HostName.Should().Be(hostName);
        instanceOptions.IPAddress.Should().Be(ipAddress);
        instanceOptions.PreferIPAddress.Should().BeFalse();
        instanceOptions.VipAddress.Should().Be(appName);
        instanceOptions.SecureVipAddress.Should().Be(appName);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(5000);
        instanceOptions.IsSecurePortEnabled.Should().BeFalse();
        instanceOptions.SecurePort.Should().BeNull();
        instanceOptions.RegistrationMethod.Should().BeNull();
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(30);
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(90);
        instanceOptions.StatusPageUrlPath.Should().Be("/info");
        instanceOptions.StatusPageUrl.Should().BeNull();
        instanceOptions.HomePageUrlPath.Should().Be("/");
        instanceOptions.HomePageUrl.Should().BeNull();
        instanceOptions.HealthCheckUrlPath.Should().Be("/health");
        instanceOptions.HealthCheckUrl.Should().BeNull();
        instanceOptions.SecureHealthCheckUrl.Should().BeNull();
        instanceOptions.AutoScalingGroupName.Should().BeNull();
        instanceOptions.DataCenterInfo.Name.Should().Be(DataCenterName.MyOwn);
        instanceOptions.UseNetworkInterfaces.Should().BeFalse();
    }

    [Fact]
    public async Task Preserves_explicit_configuration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:InstanceId"] = "test-instance-id",
            ["eureka:instance:AppName"] = "test-app-name",
            ["eureka:instance:AppGroup"] = "test-app-group-name",
            ["eureka:instance:MetadataMap:test-key"] = "test-metadata-value",
            ["eureka:instance:HostName"] = "test-host-name",
            ["eureka:instance:IPAddress"] = "test-ip-address",
            ["eureka:instance:PreferIPAddress"] = "false",
            ["eureka:instance:VipAddress"] = "test-vip-address",
            ["eureka:instance:SecureVipAddress"] = "test-secure-vip-address",
            ["eureka:instance:Port"] = "5555",
            ["eureka:instance:NonSecurePortEnabled"] = "true",
            ["eureka:instance:SecurePort"] = "9999",
            ["eureka:instance:SecurePortEnabled"] = "true",
            ["eureka:instance:RegistrationMethod"] = "test-registration-method",
            ["eureka:instance:InstanceEnabledOnInit"] = "false",
            ["eureka:instance:LeaseRenewalIntervalInSeconds"] = "15",
            ["eureka:instance:LeaseExpirationDurationInSeconds"] = "45",
            ["eureka:instance:StatusPageUrlPath"] = "test-status-page",
            ["eureka:instance:StatusPageUrl"] = "http://www.domain.org/test-status-page",
            ["eureka:instance:HomePageUrlPath"] = "test-home-page",
            ["eureka:instance:HomePageUrl"] = "http://www.domain.org/test-home-page",
            ["eureka:instance:HealthCheckUrlPath"] = "test-health-page",
            ["eureka:instance:HealthCheckUrl"] = "http://www.domain.org/test-health-page",
            ["eureka:instance:SecureHealthCheckUrl"] = "https://www.domain.org/test-secure-health-page",
            ["eureka:instance:AsgName"] = "test-scaling-group-name",
            ["eureka:instance:DataCenterInfo:Name"] = "netflix",
            ["eureka:instance:UseNetworkInterfaces"] = "true"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.InstanceId.Should().Be("test-instance-id");
        instanceOptions.AppName.Should().Be("test-app-name");
        instanceOptions.AppGroupName.Should().Be("test-app-group-name");
        instanceOptions.MetadataMap.Should().HaveCount(1);
        instanceOptions.MetadataMap.Should().ContainSingle(pair => pair.Key == "test-key" && pair.Value == "test-metadata-value");
        instanceOptions.HostName.Should().Be("test-host-name");
        instanceOptions.IPAddress.Should().Be("test-ip-address");
        instanceOptions.PreferIPAddress.Should().BeFalse();
        instanceOptions.VipAddress.Should().Be("test-vip-address");
        instanceOptions.SecureVipAddress.Should().Be("test-secure-vip-address");
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(5555);
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.SecurePort.Should().Be(9999);
        instanceOptions.RegistrationMethod.Should().Be("test-registration-method");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeFalse();
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(15);
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(45);
        instanceOptions.StatusPageUrlPath.Should().Be("test-status-page");
        instanceOptions.StatusPageUrl.Should().Be("http://www.domain.org/test-status-page");
        instanceOptions.HomePageUrlPath.Should().Be("test-home-page");
        instanceOptions.HomePageUrl.Should().Be("http://www.domain.org/test-home-page");
        instanceOptions.HealthCheckUrlPath.Should().Be("test-health-page");
        instanceOptions.HealthCheckUrl.Should().Be("http://www.domain.org/test-health-page");
        instanceOptions.SecureHealthCheckUrl.Should().Be("https://www.domain.org/test-secure-health-page");
        instanceOptions.AutoScalingGroupName.Should().Be("test-scaling-group-name");
        instanceOptions.DataCenterInfo.Name.Should().Be(DataCenterName.Netflix);
        instanceOptions.UseNetworkInterfaces.Should().BeTrue();
    }

    [Fact]
    public async Task Sets_configuration_from_spring()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:discovery:registrationMethod"] = "route",
            ["spring:application:name"] = "myapp"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.RegistrationMethod.Should().Be("route");
        instanceOptions.AppName.Should().Be("myapp");
        instanceOptions.VipAddress.Should().Be("myapp");
        instanceOptions.SecureVipAddress.Should().Be("myapp");
    }

    [Fact]
    public async Task Does_not_override_explicit_settings_with_spring_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:discovery:registrationMethod"] = "route",
            ["spring:application:name"] = "myapp",
            ["eureka:instance:RegistrationMethod"] = "explicit-registration-method",
            ["eureka:instance:AppName"] = "explicit-app-name"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.RegistrationMethod.Should().Be("explicit-registration-method");
        instanceOptions.AppName.Should().Be("explicit-app-name");
        instanceOptions.VipAddress.Should().Be("explicit-app-name");
        instanceOptions.SecureVipAddress.Should().Be("explicit-app-name");
    }

    [Fact]
    public async Task Does_not_use_network_interfaces_by_default()
    {
        var inetUtilsMock = new Mock<InetUtils>(new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254"));

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(null, services => services.AddSingleton(inetUtilsMock.Object));
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.HostName.Should().NotBe("FromMock");
        instanceOptions.IPAddress.Should().NotBe("254.254.254.254");
    }

    [Fact]
    public async Task Can_use_network_interfaces()
    {
        var inetUtilsMock = new Mock<InetUtils>(new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254"));

        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:UseNetworkInterfaces"] = "true"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings, services => services.AddSingleton(inetUtilsMock.Object));
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.HostName.Should().Be("FromMock");
        instanceOptions.IPAddress.Should().Be("254.254.254.254");
    }

    [Fact]
    public async Task Can_use_network_interfaces_without_reverse_DNS_on_IP()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:UseNetworkInterfaces"] = "true",
            ["spring:cloud:inet:SkipReverseDnsLookup"] = "true"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;
        noSlowReverseDnsQuery.Stop();

        instanceOptions.HostName.Should().NotBeNull();

        // testing with an actual reverse dns query results in around 5000 ms
        noSlowReverseDnsQuery.ElapsedMilliseconds.Should().BeInRange(0, 1500);
    }

    [Fact]
    public async Task Sets_hostname_to_IP_address()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:ipAddress"] = "192.168.0.1",
            ["eureka:instance:preferIpAddress"] = "true"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.IPAddress.Should().Be("192.168.0.1");
        instanceOptions.HostName.Should().Be("192.168.0.1");
    }

    [Fact]
    public async Task Finds_ports_from_urls()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "http://myapp:1233"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(1233);
        instanceOptions.IsSecurePortEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Picks_first_from_multiple_urls()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "https://myapp:1234;http://0.0.0.0:1233;http://::7777;http://*:8888;https://ignored:9999"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(1233);
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.SecurePort.Should().Be(1234);
    }

    [Fact]
    public async Task Handles_plus_in_urls()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "https://+:443;http://+:80"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(80);
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.SecurePort.Should().Be(443);
    }

    [Fact]
    public async Task Adds_random_number_to_instance_ID_when_ports_are_zero()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:HostName"] = "localhost",
            ["eureka:instance:AppName"] = "test-app",
            ["eureka:instance:Port"] = "0",
            ["eureka:instance:NonSecurePortEnabled"] = "true",
            ["eureka:instance:SecurePort"] = "0",
            ["eureka:instance:SecurePortEnabled"] = "true"
        };

        await using ServiceProvider serviceProvider = BuildTestServiceProvider(appSettings);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.InstanceId.Should().NotBeNull();
        string[] parts = instanceOptions.InstanceId!.Split(':');
        parts.Should().HaveCount(3);

        int.TryParse(parts[2], CultureInfo.InvariantCulture, out int number).Should().BeTrue();
        number.Should().BeInRange(90_000, 99_999);
    }

    [Fact]
    public async Task Sets_paths_from_actuators()
    {
        await using ServiceProvider serviceProvider = BuildTestServiceProvider(null, services => services.AddAllActuators());
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.HealthCheckUrlPath.Should().Be("/actuator/health");
        instanceOptions.StatusPageUrlPath.Should().Be("/actuator/info");
    }

    private static ServiceProvider BuildTestServiceProvider(Dictionary<string, string?>? appSettings, Action<IServiceCollection>? configureServices = null)
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (appSettings != null)
        {
            configurationBuilder.AddInMemoryCollection(appSettings);
        }

        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddSingleton(configuration);
        services.AddLogging();
        services.TryAddSingleton<InetUtils>();

        services.AddApplicationInstanceInfo();
        services.AddOptions<EurekaInstanceOptions>().BindConfiguration(EurekaInstanceOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<EurekaInstanceOptions>, PostConfigureEurekaInstanceOptions>();

        return services.BuildServiceProvider(true);
    }
}
