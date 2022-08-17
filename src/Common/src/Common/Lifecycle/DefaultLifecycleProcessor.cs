// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;

namespace Steeltoe.Common.Lifecycle;

public class DefaultLifecycleProcessor : ILifecycleProcessor
{
    private readonly ILogger _logger;
    private readonly IApplicationContext _context;
    private List<ILifecycle> _lifecycleServices;

    public int TimeoutPerShutdownPhase { get; set; } = 30000;

    public bool IsRunning { get; set; }

    public DefaultLifecycleProcessor(IApplicationContext context, ILogger logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        BuildServicesList();
        await StartServicesAsync(false);
        IsRunning = true;
    }

    public async Task StopAsync()
    {
        await StopServicesAsync();
        IsRunning = false;
    }

    public async Task OnRefreshAsync()
    {
        BuildServicesList();
        await StartServicesAsync(true);
        IsRunning = true;
    }

    public async Task OnCloseAsync()
    {
        await StopAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static int GetPhase(ILifecycle bean)
    {
        return bean is IPhased phased ? phased.Phase : 0;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnCloseAsync().Wait();
        }
    }

    private async Task StartServicesAsync(bool autoStartupOnly)
    {
        var phases = new Dictionary<int, LifecycleGroup>();

        foreach (ILifecycle service in _lifecycleServices)
        {
            if (!autoStartupOnly || (service is ISmartLifecycle lifecycle && lifecycle.IsAutoStartup))
            {
                int phase = GetPhase(service);
                phases.TryGetValue(phase, out LifecycleGroup group);

                if (group == null)
                {
                    group = new LifecycleGroup(TimeoutPerShutdownPhase, autoStartupOnly, _logger);
                    phases.Add(phase, group);
                }

                group.Add(service);
            }
        }

        if (phases.Count > 0)
        {
            var keys = new List<int>(phases.Keys);
            keys.Sort();

            foreach (int key in keys)
            {
                await phases[key].StartAsync();
            }
        }
    }

    private async Task StopServicesAsync()
    {
        var phases = new Dictionary<int, LifecycleGroup>();

        foreach (ILifecycle service in _lifecycleServices)
        {
            int phase = GetPhase(service);
            phases.TryGetValue(phase, out LifecycleGroup group);

            if (group == null)
            {
                group = new LifecycleGroup(TimeoutPerShutdownPhase, false, _logger);
                phases.Add(phase, group);
            }

            group.Add(service);
        }

        if (phases.Count > 0)
        {
            var keys = new List<int>(phases.Keys);
            keys.Sort();
            keys.Reverse();

            foreach (int key in keys)
            {
                await phases[key].StopAsync();
            }
        }
    }

    private void BuildServicesList()
    {
        if (_lifecycleServices == null)
        {
            List<ILifecycle> lifeCycles = _context.GetServices<ILifecycle>().ToList();
            IEnumerable<ISmartLifecycle> smartCycles = _context.GetServices<ISmartLifecycle>();

            foreach (ISmartLifecycle smart in smartCycles)
            {
                if (!lifeCycles.Contains(smart))
                {
                    lifeCycles.Add(smart);
                }
            }

            _lifecycleServices = lifeCycles;
        }
    }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    private sealed class LifecycleGroupMember : IComparable<LifecycleGroupMember>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    {
        public ILifecycle Bean { get; }

        public LifecycleGroupMember(ILifecycle bean)
        {
            Bean = bean;
        }

        public int CompareTo(LifecycleGroupMember other)
        {
            int thisPhase = GetPhase(Bean);
            int otherPhase = GetPhase(other.Bean);
            return thisPhase.CompareTo(otherPhase);
        }
    }

    private sealed class LifecycleGroup
    {
        private readonly int _timeout;

        private readonly bool _autoStartupOnly;

        private readonly List<LifecycleGroupMember> _members = new();

        private readonly ILogger _logger;

        public LifecycleGroup(int timeout, bool autoStartupOnly, ILogger logger = null)
        {
            _timeout = timeout;
            _autoStartupOnly = autoStartupOnly;
            _logger = logger;
        }

        public void Add(ILifecycle bean)
        {
            _members.Add(new LifecycleGroupMember(bean));
        }

        public async Task StartAsync()
        {
            if (_members.Count <= 0)
            {
                return;
            }

            _members.Sort();

            foreach (LifecycleGroupMember member in _members)
            {
                await DoStartAsync(member.Bean);
            }
        }

        public Task StopAsync()
        {
            if (_members.Count <= 0)
            {
                return Task.CompletedTask;
            }

            _members.Sort();
            _members.Reverse();

            var tasks = new List<Task>();

            foreach (LifecycleGroupMember member in _members)
            {
                tasks.Add(DoStopAsync(member.Bean));
            }

            try
            {
                Task.WaitAll(tasks.ToArray(), _timeout);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception waiting for lifecycle tasks to stop");
            }

            foreach (Task task in tasks)
            {
                if (!task.IsCompleted)
                {
                    _logger?.LogWarning("Not all lifecycle tasks completed");
                    break;
                }
            }

            return Task.CompletedTask;
        }

        private async Task DoStartAsync(ILifecycle bean)
        {
            if (bean is { IsRunning: false })
            {
                bool canStart = !_autoStartupOnly || bean is not ISmartLifecycle lifecycle || lifecycle.IsAutoStartup;

                if (canStart)
                {
                    try
                    {
                        await bean.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        throw new LifecycleException($"Failed to start bean(service) '{bean}'", ex);
                    }
                }
            }
        }

        private Task DoStopAsync(ILifecycle bean)
        {
            if (bean != null && bean.IsRunning)
            {
                return bean.StopAsync();
            }

            return Task.CompletedTask;
        }
    }
}
