// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaApplicationInfoManagerTest
{
    [Fact]
    public void UpdateInstance_AppliesChanges()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            InstanceId = "demo",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;
        InstanceStatus? previousStatus = appManager.Instance.Status;
        using var eventMonitor = new EventMonitor(appManager);

        appManager.UpdateInstance(InstanceStatus.OutOfService, null, null);

        appManager.Instance.Status.Should().Be(InstanceStatus.OutOfService);
        appManager.Instance.IsDirty.Should().BeTrue();

        eventMonitor.EventArgs.Should().NotBeNull();
        eventMonitor.EventArgs!.PreviousInstance.Status.Should().Be(previousStatus);
        eventMonitor.EventArgs.NewInstance.Status.Should().Be(InstanceStatus.OutOfService);
    }

    [Fact]
    public void UpdateInstance_RemovesEmptyMetadataValues()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            InstanceId = "demo",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;
        using var eventMonitor = new EventMonitor(appManager);

        appManager.UpdateInstance(null, null, new Dictionary<string, string?>
        {
            ["key1"] = null,
            ["key2"] = string.Empty,
            ["key3"] = "value"
        });

        appManager.Instance.Metadata.Should().HaveCount(1);
        appManager.Instance.Metadata.Should().ContainSingle(pair => pair.Key == "key3" && pair.Value == "value");
    }

    [Fact]
    public void UpdateInstance_NoChangeWhenEurekaTurnedOff()
    {
        var clientOptions = new EurekaClientOptions
        {
            Enabled = false
        };

        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            InstanceId = "demo",
            AppName = "demo"
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;
        using var eventMonitor = new EventMonitor(appManager);

        appManager.UpdateInstance(InstanceStatus.OutOfService, null, null);

        appManager.Instance.Status.Should().BeNull();

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public void ChangeConfiguration_AppliesChanges()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            VipAddress = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            InstanceId = "demo",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;
        using var eventMonitor = new EventMonitor(appManager);

        instanceOptions.VipAddress = "other";
        instanceOptionsMonitor.Change(instanceOptions);

        appManager.Instance.VipAddress.Should().Be("other");
        appManager.Instance.IsDirty.Should().BeTrue();

        eventMonitor.EventArgs.Should().NotBeNull();
        eventMonitor.EventArgs!.PreviousInstance.VipAddress.Should().Be("some");
        eventMonitor.EventArgs.NewInstance.VipAddress.Should().Be("other");
    }

    [Fact]
    public void ChangeConfiguration_DetectsUnchanged()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            VipAddress = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            InstanceId = "demo",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        instanceOptions.MetadataMap.Add("key1", null);
        instanceOptions.MetadataMap.Add("key2", string.Empty);
        instanceOptionsMonitor.Change(instanceOptions);

        appManager.Instance.IsDirty.Should().BeFalse();

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public void ChangeConfiguration_IgnoresConflictingChanges()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            InstanceId = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        instanceOptions.InstanceId = "other";
        instanceOptions.AppName = "other";

        instanceOptionsMonitor.Change(instanceOptions);

        appManager.Instance.InstanceId.Should().Be("some");
        appManager.Instance.AppName.Should().Be("DEMO");

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public void ChangeConfiguration_AppliesOnlyNonConflictingChanges()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            InstanceId = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        instanceOptions.InstanceId = "new-instance-id";
        instanceOptions.AppName = "new-app-name";
        instanceOptions.HostName = "new-host";
        instanceOptions.IPAddress = "192.168.0.2";

        instanceOptionsMonitor.Change(instanceOptions);

        appManager.Instance.InstanceId.Should().Be("some");
        appManager.Instance.AppName.Should().Be("DEMO");
        appManager.Instance.HostName.Should().Be("new-host");
        appManager.Instance.IPAddress.Should().Be("192.168.0.2");

        eventMonitor.EventArgs.Should().NotBeNull();
        eventMonitor.EventArgs!.PreviousInstance.InstanceId.Should().Be("some");
        eventMonitor.EventArgs.NewInstance.InstanceId.Should().Be("some");
        eventMonitor.EventArgs!.PreviousInstance.AppName.Should().Be("DEMO");
        eventMonitor.EventArgs.NewInstance.AppName.Should().Be("DEMO");
        eventMonitor.EventArgs!.PreviousInstance.HostName.Should().Be("localhost");
        eventMonitor.EventArgs.NewInstance.HostName.Should().Be("new-host");
        eventMonitor.EventArgs!.PreviousInstance.IPAddress.Should().Be("192.168.0.1");
        eventMonitor.EventArgs.NewInstance.IPAddress.Should().Be("192.168.0.2");
    }

    [Fact]
    public void ChangeConfiguration_PreservesExistingDirtyState()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            InstanceId = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            AppName = "demo"
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = true;
        using var eventMonitor = new EventMonitor(appManager);

        instanceOptionsMonitor.Change(new EurekaInstanceOptions());

        appManager.Instance.IsDirty.Should().BeTrue();

        eventMonitor.EventArgs.Should().NotBeNull();
    }

    [Fact]
    public void ChangeConfiguration_PreservesMetadataAndStatusSetFromCode()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            InstanceId = "some",
            IPAddress = "192.168.0.1",
            HostName = "localhost",
            AppName = "demo",
            MetadataMap =
            {
                ["configKey"] = "configValue"
            }
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>();
        TestOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(clientOptionsMonitor, instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        appManager.UpdateInstance(InstanceStatus.OutOfService, null, new Dictionary<string, string?>
        {
            ["appKey"] = "appValue"
        });

        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        instanceOptionsMonitor.Change(instanceOptions);

        appManager.Instance.Status.Should().Be(InstanceStatus.OutOfService);
        appManager.Instance.Metadata.Should().ContainKey("appKey");
        appManager.Instance.Metadata.Should().NotContainKey("configKey");

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public async Task ExplicitlyConfiguredIPAddressIsPreserved()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Instance:hostName"] = "ignored-host-name",
            ["Eureka:Instance:ipAddress"] = "192.168.10.20",
            ["Eureka:Instance:preferIpAddress"] = "true"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();
        var appManager = webApplication.Services.GetRequiredService<EurekaApplicationInfoManager>();

        appManager.Instance.IPAddress.Should().Be("192.168.10.20");
        appManager.Instance.HostName.Should().Be("192.168.10.20");
    }

    private sealed class EventMonitor : IDisposable
    {
        private readonly EurekaApplicationInfoManager _appManager;

        public InstanceChangedEventArgs? EventArgs { get; private set; }

        public EventMonitor(EurekaApplicationInfoManager appManager)
        {
            _appManager = appManager;
            appManager.InstanceChanged += AppInfoManagerOnInstanceChanged;
        }

        private void AppInfoManagerOnInstanceChanged(object? sender, InstanceChangedEventArgs args)
        {
            EventArgs = args;
        }

        public void Dispose()
        {
            _appManager.InstanceChanged -= AppInfoManagerOnInstanceChanged;
        }
    }
}
