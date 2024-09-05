// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.CloudFoundry.Test;

public sealed class EurekaServiceUriStateManagerTest
{
    [Fact]
    public void Throws_on_invalid_URI()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "bad//uri"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);

        Action action = () => _ = manager.GetSnapshot();

        action.Should().ThrowExactly<UriFormatException>();
    }

    [Fact]
    public void Enumerates_known_servers_once()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        snapshot.GetNextServiceUri().Should().Be("https://one");
        snapshot.GetNextServiceUri().Should().Be("https://two");
        snapshot.GetNextServiceUri().Should().Be("https://three");

        Action action = () => _ = snapshot.GetNextServiceUri();
        action.Should().ThrowExactly<EurekaTransportException>().WithMessage("Failed to execute request on all known Eureka servers.");
    }

    [Fact]
    public void Puts_last_known_working_server_at_top()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkWorkingServiceUri(new Uri("https://three"));

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        snapshot.GetNextServiceUri().Should().Be("https://three");
    }

    [Fact]
    public void Excludes_broken_servers()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkFailingServiceUri(new Uri("https://two"));

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(2);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://three");
    }

    [Fact]
    public void Clears_last_known_working_server_when_removed_from_configuration()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkWorkingServiceUri(new Uri("https://two"));

        options.EurekaServerServiceUrls = "https://one,https://three";

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(2);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://three");
    }

    [Fact]
    public void Clears_last_known_working_server_when_marked_as_failing()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkWorkingServiceUri(new Uri("https://two"));
        manager.MarkFailingServiceUri(new Uri("https://two"));

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(2);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://three");
    }

    [Fact]
    public void Removes_failing_server_when_marked_as_working()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkFailingServiceUri(new Uri("https://two"));
        manager.MarkWorkingServiceUri(new Uri("https://two"));

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();
        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(3);
        serviceUris[0].Should().Be("https://two");
        serviceUris[1].Should().Be("https://one");
        serviceUris[2].Should().Be("https://three");
    }

    [Fact]
    public void Marking_server_as_failing_does_not_affect_snapshot()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        manager.MarkFailingServiceUri(new Uri("https://two"));

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(3);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://two");
        serviceUris[2].Should().Be("https://three");
    }

    [Fact]
    public void Marking_server_as_working_does_not_affect_snapshot()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        manager.MarkWorkingServiceUri(new Uri("https://two"));

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(3);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://two");
        serviceUris[2].Should().Be("https://three");
    }

    [Fact]
    public void Failing_servers_are_cleared_when_threshold_exceeded()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one,https://two,https://three,https://four"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);
        manager.MarkFailingServiceUri(new Uri("https://one"));
        manager.MarkFailingServiceUri(new Uri("https://two"));
        manager.MarkFailingServiceUri(new Uri("https://three"));

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(4);
        serviceUris[0].Should().Be("https://one");
        serviceUris[1].Should().Be("https://two");
        serviceUris[2].Should().Be("https://three");
        serviceUris[3].Should().Be("https://four");
    }

    [Fact]
    public void Normalizes_configured_URIs()
    {
        var options = new EurekaClientOptions
        {
            EurekaServerServiceUrls = "https://one , https://two , , https://three"
        };

        var manager = new EurekaServiceUriStateManager(TestOptionsMonitor.Create(options), NullLogger<EurekaServiceUriStateManager>.Instance);

        EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot = manager.GetSnapshot();

        List<Uri> serviceUris = GetAllServiceUris(snapshot);

        serviceUris.Should().HaveCount(3);
        serviceUris[0].ToString().Should().Be("https://one/");
        serviceUris[1].ToString().Should().Be("https://two/");
        serviceUris[2].ToString().Should().Be("https://three/");
    }

    private static List<Uri> GetAllServiceUris(EurekaServiceUriStateManager.ServiceUrisSnapshot snapshot)
    {
        List<Uri> serviceUris = [];

        while (true)
        {
            try
            {
                Uri nextUri = snapshot.GetNextServiceUri();
                serviceUris.Add(nextUri);
            }
            catch (EurekaTransportException)
            {
                return serviceUris;
            }
        }
    }
}
