// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Registry.Test;

public class ConsulServiceRegistrarTest
{
    [Fact]
    public void Constructor_ThrowsOnNulls()
    {
        var registry = new Mock<IConsulServiceRegistry>().Object;
        var options = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration();

        Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(null, options, registration));
        Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(registry, (ConsulDiscoveryOptions)null, registration));
        Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(registry, options, null));
    }

    [Fact]
    public void Register_CallsRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration();

        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Register();
        regMoq.Verify(a => a.Register(registration), Times.Once);
    }

    [Fact]
    public void Deregister_CallsRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration();

        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Deregister();
        regMoq.Verify(a => a.Deregister(registration), Times.Once);
    }

    [Fact]
    public void Register_DoesNotCallRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions
        {
            Register = false
        };
        var registration = new ConsulRegistration();

        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Register();
        regMoq.Verify(a => a.Register(registration), Times.Never);
    }

    [Fact]
    public void Deregister_DoesNotCallRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions
        {
            Deregister = false
        };
        var registration = new ConsulRegistration();

        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Deregister();
        regMoq.Verify(a => a.Deregister(registration), Times.Never);
    }

    [Fact]
    public void Start_DoesNotStart()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions
        {
            Enabled = false
        };
        var registration = new ConsulRegistration();
        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Start();
        Assert.Equal(0, reg.IsRunning);
    }

    [Fact]
    public void Start_CallsRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration();
        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Start();
        Assert.Equal(1, reg.IsRunning);
        regMoq.Verify(a => a.Register(registration), Times.Once);
    }

    [Fact]
    public void Dispose_CallsRegistry()
    {
        var regMoq = new Mock<IConsulServiceRegistry>();
        var options = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration();
        var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
        reg.Start();
        Assert.Equal(1, reg.IsRunning);
        regMoq.Verify(a => a.Register(registration), Times.Once);
        reg.Dispose();
        regMoq.Verify(a => a.Deregister(registration), Times.Once);
        regMoq.Verify(a => a.Dispose(), Times.Once);
        Assert.Equal(0, reg.IsRunning);
    }
}
