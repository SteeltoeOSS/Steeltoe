// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class ApplicationInfoManagerTest
{
    [Fact]
    public void UpdateInstance_AppliesChanges()
    {
        var optionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>();
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
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
    public void ChangeConfiguration_AppliesChanges()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new EurekaInstanceOptions
        {
            VirtualHostName = "some"
        });

        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;
        using var eventMonitor = new EventMonitor(appManager);

        optionsMonitor.Change(new EurekaInstanceOptions
        {
            VirtualHostName = "other"
        });

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
            VirtualHostName = "some"
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        optionsMonitor.Change(instanceOptions);

        appManager.Instance.IsDirty.Should().BeFalse();

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public void ChangeConfiguration_IgnoresConflict()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            InstanceId = "some"
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        optionsMonitor.Change(new EurekaInstanceOptions
        {
            InstanceId = "other"
        });

        appManager.Instance.InstanceId.Should().Be("some");

        eventMonitor.EventArgs.Should().BeNull();
    }

    [Fact]
    public void ChangeConfiguration_PreservesExistingDirtyState()
    {
        var optionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>();
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        appManager.Instance.IsDirty = true;
        using var eventMonitor = new EventMonitor(appManager);

        optionsMonitor.Change(new EurekaInstanceOptions());

        appManager.Instance.IsDirty.Should().BeTrue();

        eventMonitor.EventArgs.Should().NotBeNull();
    }

    [Fact]
    public void ChangeConfiguration_PreservesMetadataAndStatusSetFromCode()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            MetadataMap =
            {
                ["configKey"] = "configValue"
            }
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        appManager.UpdateInstance(InstanceStatus.OutOfService, null, new Dictionary<string, string?>
        {
            ["appKey"] = "appValue"
        });

        appManager.Instance.IsDirty = false;

        using var eventMonitor = new EventMonitor(appManager);

        optionsMonitor.Change(instanceOptions);

        appManager.Instance.Status.Should().Be(InstanceStatus.OutOfService);
        appManager.Instance.Metadata.Should().ContainKey("appKey");
        appManager.Instance.Metadata.Should().NotContainKey("configKey");

        eventMonitor.EventArgs.Should().BeNull();
    }

    private sealed class EventMonitor : IDisposable
    {
        private readonly EurekaApplicationInfoManager _appManager;

        public InstanceChangedEventArgs? EventArgs { get; private set; }

        public EventMonitor(EurekaApplicationInfoManager appManager)
        {
            _appManager = appManager;
            appManager.InstanceChanged += AppManagerOnInstanceChanged;
        }

        private void AppManagerOnInstanceChanged(object? sender, InstanceChangedEventArgs args)
        {
            EventArgs = args;
        }

        public void Dispose()
        {
            _appManager.InstanceChanged -= AppManagerOnInstanceChanged;
        }
    }
}
