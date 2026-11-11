// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;
using Steeltoe.Configuration.ConfigServer;

namespace Steeltoe.Connectors.Test.ParentConfiguration;

/// <summary>
/// Verifies that connectors dispose the temporary copies of the parent configuration that they build to post-process connection strings: the copy after
/// loading, the previous copy when the configuration changes, and what the copies create themselves, such as the discovery clients of Config Server.
/// </summary>
public sealed class TemporaryCopyLifecycleTest
{
    [Fact]
    public void Disposes_temporary_copy_of_parent_configuration()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.AddSilentConnector();

        using (builder.BuildAsRoot())
        {
            // One provider for the actual configuration, and one for the temporary copy used to post-process connection strings.
            countingSource.Providers.Should().HaveCount(2);
            countingSource.Providers[0].DisposeCount.Should().Be(0);
            countingSource.Providers[1].DisposeCount.Should().Be(1);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Disposes_previous_temporary_copy_of_parent_configuration_when_configuration_changes()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);

        builder.AddSilentConnector(true);

        using (builder.BuildAsRoot())
        {
            // Only the actual provider and the copy of the parent configuration whose changes are being observed remain alive.
            countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);

            for (int index = 0; index < 5; index++)
            {
                DisposeCountingConfigurationProvider lastProvider = countingSource.Providers.Last(provider => provider.DisposeCount == 0);

                lastProvider.TriggerReload();

                lastProvider.DisposeCount.Should().Be(1);
                countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);
            }
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Does_not_accumulate_temporary_copies_of_parent_configuration_when_file_changes()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "Example": {
                "Value": "initial"
              }
            }
            """);

        var countingSource = new DisposeCountingConfigurationSource();

        using (var manager = new ConfigurationManager())
        {
            manager.AddInMemoryAppSettingsJsonFile(fileProvider);
            manager.Sources.Add(countingSource);

            manager.AddSilentConnector(true);

            for (int index = 0; index < 25; index++)
            {
                fileProvider.ReplaceAppSettingsJsonFile($$"""
                    {
                      "Example": {
                        "Value": "changed-{{index}}"
                      }
                    }
                    """);

                fileProvider.NotifyChanged();

                // The actual provider, and the copy of the parent configuration that is being observed for changes.
                countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);
            }

            // Proves the changes were detected: each reload creates new copies of the parent configuration.
            countingSource.Providers.Should().HaveCountGreaterThan(25);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Disposing_temporary_copy_of_parent_configuration_shuts_down_its_temporary_discovery_clients()
    {
        // The temporary copy of the parent configuration contains its own Config Server provider, which locates Config Server through discovery clients
        // that it creates itself. Those must not outlive the copy, and must not affect the discovery clients of the actual configuration.
        using var handler = new DelegateToMockHttpClientHandler();
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        ConfigServerConfigurationProvider configServerProvider;

        using (var configurationManager = new ConfigurationManager())
        {
            AddConfigServerWithDiscovery(configurationManager, handler);
            configurationManager.AddSilentConnector();

            configServerProvider = configurationManager.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

            // In a host, these clients get replaced when the application starts.
            configServerProvider.HasTemporaryDiscoveryClients.Should().BeTrue();

            ConnectionStringPostProcessorConfigurationSource connectorSource =
                configurationManager.EnumerateSources<ConnectionStringPostProcessorConfigurationSource>().Single();

            ConfigServerConfigurationProvider copiedConfigServerProvider;

            using (ConfigurationRoot parentConfigurationRoot = connectorSource.GetParentConfiguration())
            {
                copiedConfigServerProvider = parentConfigurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Should().ContainSingle().Subject;
                copiedConfigServerProvider.HasTemporaryDiscoveryClients.Should().BeTrue();
            }

            copiedConfigServerProvider.HasTemporaryDiscoveryClients.Should().BeFalse();
            configServerProvider.HasTemporaryDiscoveryClients.Should().BeTrue();
        }

        configServerProvider.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    [Fact]
    public void Reloading_shuts_down_temporary_discovery_clients_of_previous_parent_configuration_when_change_detection_is_enabled()
    {
        using var handler = new DelegateToMockHttpClientHandler();
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeAppSettingsJsonFile("""{ "Example": { "Value": "initial" } }""");

        List<ConfigServerConfigurationProvider> configServerProviders = [];

        using (var configurationManager = new ConfigurationManager())
        {
            configurationManager.AddInMemoryAppSettingsJsonFile(fileProvider);
            AddConfigServerWithDiscovery(configurationManager, handler);

            configurationManager.AddSilentConnector(true);

            ConnectionStringPostProcessorConfigurationProvider connectorProvider =
                configurationManager.EnumerateProviders<ConnectionStringPostProcessorConfigurationProvider>().Single();

            for (int index = 0; index < 4; index++)
            {
                // Each reload replaces the temporary copy of the parent configuration that the connector observes for changes.
                ConfigServerConfigurationProvider configServerProvider = GetObservedConfigServerProvider(connectorProvider);

                configServerProviders.Should().NotContain(configServerProvider);
                configServerProviders.Add(configServerProvider);

                configServerProvider.HasTemporaryDiscoveryClients.Should().BeTrue();

                fileProvider.ReplaceAppSettingsJsonFile($$"""{ "Example": { "Value": "changed-{{index}}" } }""");
                fileProvider.NotifyChanged();
            }

            // The latest reload created a new copy, which is the only one that still has discovery clients.
            ConfigServerConfigurationProvider currentConfigServerProvider = GetObservedConfigServerProvider(connectorProvider);
            configServerProviders.Should().NotContain(currentConfigServerProvider);
            currentConfigServerProvider.HasTemporaryDiscoveryClients.Should().BeTrue();
            configServerProviders.Should().OnlyContain(provider => !provider.HasTemporaryDiscoveryClients);
        }

        configServerProviders.Should().OnlyContain(provider => !provider.HasTemporaryDiscoveryClients);
    }

    [Fact]
    public async Task Replacing_temporary_copy_of_parent_configuration_does_not_deadlock_when_Config_Server_poll_returns_changed_data()
    {
        // Each poll of Config Server returns different data. The temporary copy of the parent configuration contains its own Config Server provider, which
        // polls too. When that provider reports changed data, the connector reacts to the notification, which runs in that provider's polling callback, by
        // replacing the copy. That disposes the previous copy, which contains the very provider that is still running the notification. Disposing a Config
        // Server provider must not wait for the polling callback that raised the notification, because that callback cannot complete before the connector
        // is done reacting.
        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.When("*").Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                propertySources = new[]
                {
                    new
                    {
                        name = "source",
                        source = new
                        {
                            key = Guid.NewGuid()
                        }
                    }
                }
            })
        });

        var options = new ConfigServerClientOptions
        {
            PollingInterval = TimeSpan.FromMilliseconds(100)
        };

        var builder = new ConfigurationBuilder();

        // ReSharper disable once AccessToDisposedClosure
        builder.AddConfigServer(options, null, () => handler, NullLoggerFactory.Instance);

        builder.AddSilentConnector(true);

        ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        ConnectionStringPostProcessorConfigurationProvider connectorProvider =
            configurationRoot.Providers.OfType<ConnectionStringPostProcessorConfigurationProvider>().Single();

        // The connector reloads after it has replaced the temporary copy in response to a change.
        var connectorReloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable changeRegistration = ChangeToken.OnChange(connectorProvider.GetReloadToken, () => connectorReloaded.TrySetResult());

        // A deadlock must fail the test instead of hanging it, so waiting and disposal happen with a timeout, and disposal runs on another thread.
        Func<Task> action = async () =>
        {
            await connectorReloaded.Task.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);
            await Task.Run(configurationRoot.Dispose, CancellationToken.None).WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);
        };

        await action.Should().NotThrowAsync<TimeoutException>();
    }

    private static ConfigServerConfigurationProvider GetObservedConfigServerProvider(ConnectionStringPostProcessorConfigurationProvider connectorProvider)
    {
        ConfigurationRoot? lastParentConfiguration = connectorProvider.LastParentConfiguration;
        lastParentConfiguration.Should().NotBeNull();

        return lastParentConfiguration.Providers.OfType<ConfigServerConfigurationProvider>().Should().ContainSingle().Subject;
    }

    private static void AddConfigServerWithDiscovery(IConfigurationBuilder builder, DelegateToMockHttpClientHandler handler)
    {
        // Discovery is enabled for Config Server, but the discovery clients themselves are not.
        builder.Add(FastTestConfigurations.Discovery);

        var options = new ConfigServerClientOptions
        {
            Discovery =
            {
                Enabled = true
            }
        };

        // ReSharper disable once AccessToDisposedClosure
        builder.AddConfigServer(options, null, () => handler, NullLoggerFactory.Instance);
    }
}
