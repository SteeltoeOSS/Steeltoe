// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Configuration.ConfigServer.Discovery.Test;

public sealed class ConfigServerDiscoveryServiceTest
{
    [Fact]
    public async Task ConfigServerDiscoveryService_FindsDiscoveryClients()
    {
        using ConfigurationRoot configurationRoot =
            new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().HaveCount(3);
        service.DiscoveryClients.OfType<ConfigurationDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<ConsulDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<EurekaDiscoveryClient>().Should().ContainSingle();

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_returns_instances_found_by_discovery_clients()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["discovery:services:0:serviceId"] = "configserver",
            ["discovery:services:0:host"] = "config.example.com",
            ["discovery:services:0:port"] = "8888",
            ["discovery:services:1:serviceId"] = "other-service",
            ["discovery:services:1:host"] = "other.example.com",
            ["discovery:services:1:port"] = "9999"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        IServiceInstance instance = result.Should().ContainSingle().Subject;
        instance.ServiceId.Should().Be("configserver");
        instance.Host.Should().Be("config.example.com");
        instance.Port.Should().Be(8888);

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_returns_nothing_when_Config_Server_is_not_discovered()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["discovery:services:0:serviceId"] = "other-service",
            ["discovery:services:0:host"] = "other.example.com",
            ["discovery:services:0:port"] = "9999"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_retries_when_no_instances_are_found()
    {
        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().BuildAsRoot();

        var options = new ConfigServerClientOptions
        {
            Retry =
            {
                Enabled = true,
                MaxAttempts = 2,
                InitialInterval = 10
            }
        };

        var discoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        await service.ProvideRuntimeReplacementsAsync([discoveryClient], TestContext.Current.CancellationToken);

        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        discoveryClient.GetInstancesCallCount.Should().Be(3);
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        using ConfigurationRoot configurationRoot =
            new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).BuildAsRoot();

        var discoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);

        await service.ProvideRuntimeReplacementsAsync([discoveryClient], TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().ContainSingle().Which.Should().Be(discoveryClient);
    }

    [Fact]
    public async Task Shutdown_shuts_down_discovery_clients_and_disposes_temporary_services()
    {
        var client = new TestDiscoveryClient();
        var disposeSpy = new DisposeSpy();

        ConfigServerDiscoveryService service = CreateDiscoveryServiceWithCustomDiscoveryClients(disposeSpy, client);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        service.HasTemporaryDiscoveryClients.Should().BeTrue();

        await service.ShutdownAsync(TestContext.Current.CancellationToken);

        client.ShutdownCallCount.Should().Be(1);
        disposeSpy.IsDisposed.Should().BeTrue();
        service.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    [Fact]
    public async Task Shutdown_continues_when_a_discovery_client_fails_and_rethrows_the_exception()
    {
        var failingClient = new TestDiscoveryClient
        {
            ShutdownException = new InvalidOperationException("Shutdown failed.")
        };

        var workingClient = new TestDiscoveryClient();
        var disposeSpy = new DisposeSpy();

        ConfigServerDiscoveryService service = CreateDiscoveryServiceWithCustomDiscoveryClients(disposeSpy, failingClient, workingClient);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        Func<Task> action = async () => await service.ShutdownAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Shutdown failed.");

        workingClient.ShutdownCallCount.Should().Be(1);
        disposeSpy.IsDisposed.Should().BeTrue();
        service.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    [Fact]
    public async Task Shutdown_throws_all_exceptions_when_multiple_discovery_clients_fail()
    {
        var failingClient1 = new TestDiscoveryClient
        {
            ShutdownException = new InvalidOperationException("Shutdown 1 failed.")
        };

        var failingClient2 = new TestDiscoveryClient
        {
            ShutdownException = new InvalidOperationException("Shutdown 2 failed.")
        };

        var workingClient = new TestDiscoveryClient();
        var disposeSpy = new DisposeSpy();

        ConfigServerDiscoveryService service = CreateDiscoveryServiceWithCustomDiscoveryClients(disposeSpy, failingClient1, failingClient2, workingClient);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        Func<Task> action = async () => await service.ShutdownAsync(TestContext.Current.CancellationToken);

        ExceptionAssertions<AggregateException> assertion = await action.Should().ThrowExactlyAsync<AggregateException>();
        assertion.Which.InnerExceptions.Select(exception => exception.Message).Should().BeEquivalentTo("Shutdown 1 failed.", "Shutdown 2 failed.");

        workingClient.ShutdownCallCount.Should().Be(1);
        disposeSpy.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Failing_to_create_discovery_clients_disposes_temporary_services()
    {
        var disposeSpy = new DisposeSpy();
        var exception = new InvalidOperationException("Creating the discovery client failed.");

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().Add(FastTestConfigurations.Discovery).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);

        service.ConfigureTemporaryServicesForTest(services =>
        {
            services.AddSingleton(_ => disposeSpy);

            services.AddSingleton<IDiscoveryClient>(serviceProvider =>
            {
                // Creates the disposable first, so there is something that needs to be disposed when this fails.
                serviceProvider.GetRequiredService<DisposeSpy>();
                throw exception;
            });
        });

        Func<Task> action = async () => await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage(exception.Message);

        disposeSpy.IsDisposed.Should().BeTrue();
        service.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigServerInstances_skips_failing_discovery_clients_and_uses_the_others()
    {
        var failingClient = new TestDiscoveryClient
        {
            GetInstancesException = new InvalidOperationException("Lookup failed.")
        };

        var workingClient = new TestDiscoveryClient
        {
            Instances =
            [
                new ConfigurationServiceInstance
                {
                    ServiceId = "configserver",
                    Host = "config.example.com",
                    Port = 8888
                }
            ]
        };

        ConfigServerDiscoveryService service = CreateDiscoveryServiceWithCustomDiscoveryClients(new DisposeSpy(), failingClient, workingClient);

        var options = new ConfigServerClientOptions
        {
            Discovery =
            {
                ServiceId = "configserver"
            }
        };

        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);

        result.Should().ContainSingle().Which.Host.Should().Be("config.example.com");

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Replacing_discovery_clients_activates_replacements_even_when_shutting_down_temporary_clients_fails()
    {
        var temporaryClient = new TestDiscoveryClient
        {
            ShutdownException = new InvalidOperationException("Shutdown failed.")
        };

        var disposeSpy = new DisposeSpy();
        var replacementClient = new TestDiscoveryClient();

        ConfigServerDiscoveryService service = CreateDiscoveryServiceWithCustomDiscoveryClients(disposeSpy, temporaryClient);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        Func<Task> action = async () => await service.ProvideRuntimeReplacementsAsync([replacementClient], TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Shutdown failed.");

        service.DiscoveryClients.Should().ContainSingle().Which.Should().Be(replacementClient);
        disposeSpy.IsDisposed.Should().BeTrue();
        service.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    private static ConfigServerDiscoveryService CreateDiscoveryServiceWithCustomDiscoveryClients(DisposeSpy disposeSpy,
        params TestDiscoveryClient[] discoveryClients)
    {
        // Disables the built-in discovery clients from doing anything.
        ConfigurationRoot configurationRoot = new ConfigurationBuilder().Add(FastTestConfigurations.Discovery).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);

        service.ConfigureTemporaryServicesForTest(services =>
        {
            // Registered with a factory, so the container owns the instance and disposes it, like it does for services it creates itself.
            services.AddSingleton(_ => disposeSpy);

            foreach (TestDiscoveryClient discoveryClient in discoveryClients)
            {
                services.AddSingleton<IDiscoveryClient>(serviceProvider =>
                {
                    serviceProvider.GetRequiredService<DisposeSpy>();
                    return discoveryClient;
                });
            }
        });

        return service;
    }

    private sealed class DisposeSpy : IDisposable
    {
        private volatile bool _isDisposed;

        public bool IsDisposed => _isDisposed;

        public void Dispose()
        {
            _isDisposed = true;
        }
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
        private int _getInstancesCallCount;
        private int _shutdownCallCount;

        public int GetInstancesCallCount => Volatile.Read(ref _getInstancesCallCount);
        public int ShutdownCallCount => Volatile.Read(ref _shutdownCallCount);
        public Exception? ShutdownException { get; init; }
        public Exception? GetInstancesException { get; init; }
        public IList<IServiceInstance> Instances { get; init; } = [];

        public string Description => nameof(TestDiscoveryClient);

#pragma warning disable CS0067 // The event is never used
        public event EventHandler<DiscoveryInstancesFetchedEventArgs>? InstancesFetched;
#pragma warning restore CS0067 // The event is never used

        public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _getInstancesCallCount);

            if (GetInstancesException != null)
            {
                throw GetInstancesException;
            }

            return Task.FromResult(Instances);
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _shutdownCallCount);

            if (ShutdownException != null)
            {
                throw ShutdownException;
            }

            return Task.CompletedTask;
        }
    }
}
