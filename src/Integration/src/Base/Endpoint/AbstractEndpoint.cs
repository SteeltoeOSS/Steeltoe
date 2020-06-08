// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Lifecycle;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Endpoint
{
    public abstract class AbstractEndpoint : ISmartLifecycle
    {
        protected IServiceProvider _serviceProvider;
        private readonly object _lifecyclelock = new object();
        private IIntegrationServices _integrationServices;

        protected AbstractEndpoint(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IIntegrationServices IntegrationServices
        {
            get
            {
                if (_integrationServices == null)
                {
                    _integrationServices = _serviceProvider.GetService<IIntegrationServices>();
                }

                return _integrationServices;
            }
        }

        public virtual string ComponentType { get; set; }

        public virtual string Name { get; set; }

        public virtual string ComponentName { get; set; }

        public bool IsAutoStartup { get; set; } = true;

        public bool IsRunning { get; set; } = false;

        public int Phase { get; set; } = 0;

        public Task Start()
        {
            var doTheStart = false;
            lock (_lifecyclelock)
            {
                if (!IsRunning)
                {
                    doTheStart = true;
                    IsRunning = true;
                }
            }

            if (doTheStart)
            {
                return DoStart();
            }

            return Task.CompletedTask;
        }

        public Task Stop(Action callback)
        {
            var doTheStop = false;

            lock (_lifecyclelock)
            {
                if (IsRunning)
                {
                    doTheStop = true;
                    IsRunning = false;
                }
            }

            if (doTheStop)
            {
                return DoStop(callback);
            }

            callback();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            var doTheStop = false;
            lock (_lifecyclelock)
            {
                if (IsRunning)
                {
                    doTheStop = true;
                    IsRunning = false;
                }
            }

            if (doTheStop)
            {
                return DoStop();
            }

            return Task.CompletedTask;
        }

        protected virtual async Task DoStop(Action callback)
        {
            await DoStop();
            callback();
        }

        protected abstract Task DoStop();

        protected abstract Task DoStart();
    }
}
