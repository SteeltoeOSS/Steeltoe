// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public async Task Start()
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
                await DoStart();
            }
        }

        public async Task Stop(Action callback)
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
                await DoStop(callback);
            }
            else
            {
                callback();
            }
        }

        public async Task Stop()
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
                await DoStop();
            }
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
