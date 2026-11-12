// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using LockPrimitive =
#if NET9_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task Dispose_from_change_notification_raised_during_reload_cycle_does_not_wait_for_that_cycle()
    {
        using DelegateToMockHttpClientHandler handler = CreateHandlerReturningChangedDataOnEveryRequest();

        var options = new ConfigServerClientOptions
        {
            PollingInterval = 100.Milliseconds()
        };

        // Not disposing the provider here, because the change notification below disposes it. Were that to deadlock, disposing again would hang the test instead of failing it.
        // ReSharper disable once AccessToDisposedClosure
        var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
        provider.Load();

        // A consumer that disposes the provider in response to a change must not make disposal wait for the reload cycle that raised the notification.
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Also not disposed through a using statement: disposing a registration waits for its callback, which is stuck in case of a deadlock.
        IDisposable changeRegistration = ChangeToken.OnChange(provider.GetReloadToken, () =>
        {
            provider.Dispose();
            disposed.TrySetResult();
        });

        Func<Task> action = async () => await disposed.Task.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync<TimeoutException>();

        changeRegistration.Dispose();
    }

    [Fact]
    public async Task Dispose_from_another_thread_does_not_wait_for_listeners_of_change_notification_raised_during_reload_cycle()
    {
        using DelegateToMockHttpClientHandler handler = CreateHandlerReturningChangedDataOnEveryRequest();

        var options = new ConfigServerClientOptions
        {
            PollingInterval = 100.Milliseconds()
        };

        // ReSharper disable once AccessToDisposedClosure
        var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
        provider.Load();

        // The listener stays inside the change notification, which the reload cycle raises, until the test lets it go. Listeners run arbitrary code, such
        // as waiting for a lock that another thread holds while disposing, so disposal must not depend on them.
        var listenerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listenerReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        IDisposable changeRegistration = ChangeToken.OnChange(provider.GetReloadToken, () =>
        {
            listenerEntered.TrySetResult();
            listenerReleased.Task.Wait();
        });

        try
        {
            await listenerEntered.Task.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);

            Func<Task> action = async () => await Task.Run(provider.Dispose, CancellationToken.None).WaitAsync(Timeouts.Safety,
                TestContext.Current.CancellationToken);

            await action.Should().NotThrowAsync<TimeoutException>();
        }
        finally
        {
            listenerReleased.TrySetResult();
        }

        changeRegistration.Dispose();
    }

    [Fact]
    public async Task Load_and_reload_timer_never_fetch_at_the_same_time()
    {
        var timeProvider = new FakeTimeProvider();
        var requests = new ConcurrencyTracker();
        var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRequestReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requestCount = 0;

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.When("*").Respond(async _ =>
        {
            requests.Enter();

            try
            {
                if (Interlocked.Increment(ref requestCount) == 1)
                {
                    // The request of Load stays pending until the reload timer has become due.
                    firstRequestStarted.TrySetResult();
                    await firstRequestReleased.Task;
                }
            }
            finally
            {
                requests.Exit();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    propertySources = Array.Empty<object>()
                })
            };
        });

        var options = new ConfigServerClientOptions
        {
            PollingInterval = 1.Minutes()
        };

        // ReSharper disable once AccessToDisposedClosure
        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);

        Task load = Task.Run(provider.Load, CancellationToken.None);
        await firstRequestStarted.Task.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);

        // The reload timer runs its callback right here and must skip it, because Load is still fetching. Otherwise, a second request is made.
        timeProvider.Advance(options.PollingInterval);

        firstRequestReleased.SetResult();
        await load.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);

        requests.Maximum.Should().Be(1);
    }

    private static DelegateToMockHttpClientHandler CreateHandlerReturningChangedDataOnEveryRequest()
    {
        var handler = new DelegateToMockHttpClientHandler();

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

        return handler;
    }

    private sealed class ConcurrencyTracker
    {
        private readonly LockPrimitive _lock = new();
        private int _current;
        private int _maximum;

        public int Maximum
        {
            get
            {
                lock (_lock)
                {
                    return _maximum;
                }
            }
        }

        public void Enter()
        {
            lock (_lock)
            {
                _current++;
                _maximum = Math.Max(_maximum, _current);
            }
        }

        public void Exit()
        {
            lock (_lock)
            {
                _current--;
            }
        }
    }
}
