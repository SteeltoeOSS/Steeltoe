// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientPeriodicTasksTest
{
    private const string EmptyRegistryJson = """
        {
          "applications": {
            "versions__delta": "1",
            "apps__hashcode": "",
            "application": []
          }
        }
        """;

    [Fact]
    public async Task Periodically_fetches_registry_updates()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "true",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:RegistryFetchIntervalSeconds"] = "1"
        };

        int fetchCount = 0;
        var fetchedAgain = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var handler = new DelegateToMockHttpClientHandler();

        // The registry is empty, which makes every fetch a full fetch.
        handler.Mock.When(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond(_ =>
        {
            if (Interlocked.Increment(ref fetchCount) == 2)
            {
                fetchedAgain.TrySetResult();
            }

            return CreateJsonResponse(EmptyRegistryJson);
        });

        var timeProvider = new FakeTimeProvider();

        await using HostWrapper host = CreateHost(appSettings, handler, timeProvider);
        await host.StartAsync(TestContext.Current.CancellationToken);

        // The registry is fetched at startup. Only the timer can cause the fetch that follows.
        timeProvider.Advance(1.Seconds());
        await WaitForAsync(fetchedAgain);

        fetchedAgain.Task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Registers_again_when_instance_changes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        int registrationCount = 0;
        var registeredAgain = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.When(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(_ =>
        {
            if (Interlocked.Increment(ref registrationCount) == 2)
            {
                registeredAgain.TrySetResult();
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        handler.RespondToAnyRequest(HttpStatusCode.OK);

        await using HostWrapper host = CreateHost(appSettings, handler);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = host.Services.GetRequiredService<EurekaApplicationInfoManager>();
        infoManager.UpdateInstance(InstanceStatus.OutOfService, null, null);

        await WaitForAsync(registeredAgain);

        registeredAgain.Task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Stops_sending_heartbeats_after_shutdown()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Instance:LeaseRenewalIntervalInSeconds"] = "1",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond("application/json", "{}");
        handler.RespondToAnyRequest(HttpStatusCode.OK);

        var timeProvider = new FakeTimeProvider();

        await using HostWrapper host = CreateHost(appSettings, handler, timeProvider);
        var discoveryClient = host.Services.GetRequiredService<EurekaDiscoveryClient>();

        var firstCycleCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        discoveryClient.HeartbeatCycleCompleted += (_, _) => firstCycleCompleted.TrySetResult();

        await host.StartAsync(TestContext.Current.CancellationToken);

        timeProvider.Advance(1.Seconds());
        await WaitForAsync(firstCycleCompleted);

        await discoveryClient.ShutdownAsync(TestContext.Current.CancellationToken);

        int cyclesAfterShutdown = 0;
        discoveryClient.HeartbeatCycleCompleted += (_, _) => Interlocked.Increment(ref cyclesAfterShutdown);

        // A timer that is still running fires during this advance and reports its cycle right away, because the client no longer does any work.
        timeProvider.Advance(5.Seconds());

        cyclesAfterShutdown.Should().Be(0);
    }

    [Fact]
    public async Task Unregisters_when_host_stops()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NoContent);
        handler.Mock.Expect(HttpMethod.Delete, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond(HttpStatusCode.OK);

        await using HostWrapper host = CreateHost(appSettings, handler);
        await host.StartAsync(TestContext.Current.CancellationToken);

        await host.StopAsync(TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    private static HostWrapper CreateHost(Dictionary<string, string?> appSettings, DelegateToMockHttpClientHandler handler, TimeProvider? timeProvider = null)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

        if (timeProvider != null)
        {
            builder.Services.AddSingleton(timeProvider);
        }

        builder.Services.AddEurekaDiscoveryClient();

        HostWrapper host = HostWrapper.Wrap(builder.Build());
        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        return host;
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static async Task WaitForAsync(TaskCompletionSource source)
    {
        // The timeout is only a safety net against hanging forever. Under CI pressure the timers may take much longer than their interval.
        await source.Task.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken);
    }
}
