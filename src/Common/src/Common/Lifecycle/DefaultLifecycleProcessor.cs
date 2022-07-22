// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Common.Lifecycle;
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
public class DefaultLifecycleProcessor : ILifecycleProcessor
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
{
    private readonly ILogger _logger;
    private readonly IApplicationContext _context;
    private List<ILifecycle> _lifecyclesServices;

    public DefaultLifecycleProcessor(IApplicationContext context, ILogger logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public int TimeoutPerShutdownPhase { get; set; } = 30000;

    public bool IsRunning { get; set; }

    public async Task Start()
    {
        BuildServicesList();
        await StartServices(false);
        IsRunning = true;
    }

    public async Task Stop()
    {
        await StopServices();
        IsRunning = false;
    }

    public async Task OnRefresh()
    {
        BuildServicesList();
        await StartServices(true);
        IsRunning = true;
    }

    public async Task OnClose() => await Stop();

    public void Dispose() => OnClose().Wait();

    internal static int GetPhase(ILifecycle bean)
    {
        return bean is IPhased phased ? phased.Phase : 0;
    }

    private async Task StartServices(bool autoStartupOnly)
    {
        var phases = new Dictionary<int, LifecycleGroup>();
        foreach (var service in _lifecyclesServices)
        {
            if (!autoStartupOnly || (service is ISmartLifecycle lifecycle && lifecycle.IsAutoStartup))
            {
                var phase = GetPhase(service);
                phases.TryGetValue(phase, out var group);
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
            foreach (var key in keys)
            {
                await phases[key].Start();
            }
        }
    }

    private async Task StopServices()
    {
        var phases = new Dictionary<int, LifecycleGroup>();
        foreach (var service in _lifecyclesServices)
        {
            var phase = GetPhase(service);
            phases.TryGetValue(phase, out var group);
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
            foreach (var key in keys)
            {
                await phases[key].Stop();
            }
        }
    }

    private void BuildServicesList()
    {
        if (_lifecyclesServices == null)
        {
            var lifeCycles = _context.GetServices<ILifecycle>().ToList();
            var smartCycles = _context.GetServices<ISmartLifecycle>();

            foreach (var smart in smartCycles)
            {
                if (!lifeCycles.Contains(smart))
                {
                    lifeCycles.Add(smart);
                }
            }

            _lifecyclesServices = lifeCycles;
        }
    }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    private class LifecycleGroupMember : IComparable<LifecycleGroupMember>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    {
        public ILifecycle Bean { get; }

        public LifecycleGroupMember(ILifecycle bean)
        {
            Bean = bean;
        }

        public int CompareTo(LifecycleGroupMember other)
        {
            var thisPhase = GetPhase(Bean);
            var otherPhase = GetPhase(other.Bean);
            return thisPhase.CompareTo(otherPhase);
        }
    }

    private class LifecycleGroup
    {
        private readonly int _timeout;

        private readonly bool _autoStartupOnly;

        private readonly List<LifecycleGroupMember> _members = new ();

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

        public async Task Start()
        {
            if (_members.Count <= 0)
            {
                return;
            }

            _members.Sort();
            foreach (var member in _members)
            {
                await DoStart(member.Bean);
            }
        }

        public Task Stop()
        {
            if (_members.Count <= 0)
            {
                return Task.CompletedTask;
            }

            _members.Sort();
            _members.Reverse();

            var tasks = new List<Task>();
            foreach (var member in _members)
            {
                tasks.Add(DoStop(member.Bean));
            }

            try
            {
                Task.WaitAll(tasks.ToArray(), _timeout);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception waiting for lifecycle tasks to stop");
            }

            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    _logger?.LogWarning("Not all lifecycle tasks completed");
                    break;
                }
            }

            return Task.CompletedTask;
        }

        private async Task DoStart(ILifecycle bean)
        {
            if (bean != null && bean != this && !bean.IsRunning && (!_autoStartupOnly || bean is not ISmartLifecycle lifecycle || lifecycle.IsAutoStartup))
            {
                try
                {
                    await bean.Start();
                }
                catch (Exception ex)
                {
                    throw new LifecycleException("Failed to start bean(service) '" + bean + "'", ex);
                }
            }
        }

        private Task DoStop(ILifecycle bean)
        {
            if (bean != null && bean.IsRunning)
            {
                return bean.Stop();
            }

            return Task.CompletedTask;
        }
    }
}