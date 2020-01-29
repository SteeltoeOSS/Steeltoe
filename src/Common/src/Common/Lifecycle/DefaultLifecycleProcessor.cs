﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Common.Lifecycle
{
    public class DefaultLifecycleProcessor : ILifecycleProcessor
    {
        private readonly List<ILifecycle> _lifecyclesServices;

        public DefaultLifecycleProcessor(IEnumerable<ILifecycle> lifecyclesServices)
        {
            _lifecyclesServices = lifecyclesServices.ToList();
        }

        public int TimeoutPerShutdownPhase { get; set; } = 30000;

        public bool IsRunning { get; set; }

        public async Task Start()
        {
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
            await StartServices(true);
            IsRunning = true;
        }

        public async Task OnClose()
        {
            await StopServices();
            IsRunning = false;
        }

        internal static int GetPhase(ILifecycle bean)
        {
            return bean is IPhased ? ((IPhased)bean).Phase : 0;
        }

        private async Task StartServices(bool autoStartupOnly)
        {
            var phases = new Dictionary<int, LifecycleGroup>();
            foreach (var service in _lifecyclesServices)
            {
                if (!autoStartupOnly || (service is ISmartLifecycle && ((ISmartLifecycle)service).IsAutoStartup))
                {
                    var phase = GetPhase(service);
                    phases.TryGetValue(phase, out var group);
                    if (group == null)
                    {
                        group = new LifecycleGroup(phase, TimeoutPerShutdownPhase, autoStartupOnly);
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
                    group = new LifecycleGroup(phase, TimeoutPerShutdownPhase, false);
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
            private readonly int phase;

            private readonly int timeout;

            private readonly bool autoStartupOnly;

            private readonly List<LifecycleGroupMember> members = new List<LifecycleGroupMember>();

            private int smartMemberCount;

            public LifecycleGroup(int phase, int timeout, bool autoStartupOnly)
            {
                this.phase = phase;
                this.timeout = timeout;
                this.autoStartupOnly = autoStartupOnly;
            }

            public void Add(ILifecycle bean)
            {
                members.Add(new LifecycleGroupMember(bean));
                if (bean is ISmartLifecycle)
                {
                    smartMemberCount++;
                }
            }

            public async Task Start()
            {
                if (members.Count <= 0)
                {
                    return;
                }

                members.Sort();
                foreach (var member in members)
                {
                    await DoStart(member.Bean);
                }
            }

            public Task Stop()
            {
                if (members.Count <= 0)
                {
                    return Task.CompletedTask;
                }

                members.Sort();
                members.Reverse();

                var tasks = new List<Task>();
                foreach (var member in members)
                {
                    tasks.Add(DoStop(member.Bean));
                }

                try
                {
                    Task.WaitAll(tasks.ToArray(), timeout);
                }
                catch (Exception)
                {
                    // Log
                }

                foreach (var task in tasks)
                {
                    if (!task.IsCompleted)
                    {
                        // TODO: Log each that didn't stop
                        break;
                    }
                }

                return Task.CompletedTask;
            }

            private async Task DoStart(ILifecycle bean)
            {
                if (bean != null && bean != this && !bean.IsRunning && (!autoStartupOnly || !(bean is ISmartLifecycle) || ((ISmartLifecycle)bean).IsAutoStartup))
                {
                    try
                    {
                        await bean.Start();
                    }
                    catch (Exception ex)
                    {
                        throw new LifecycleException("Failed to start bean '" + bean + "'", ex);
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
}
