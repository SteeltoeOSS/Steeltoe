// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Services;
using Steeltoe.Integration.Util;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Endpoint
{
    public abstract class AbstractEndpoint : ISmartLifecycle, IServiceNameAware
    {
        private readonly object _lifecyclelock = new ();
        private IIntegrationServices _integrationServices;

        protected AbstractEndpoint(IApplicationContext context)
        {
            ApplicationContext = context;
            ServiceName = GetType().FullName + "." + Guid.NewGuid();
        }

        public IIntegrationServices IntegrationServices
        {
            get
            {
                if (_integrationServices == null)
                {
                    _integrationServices = IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
                }

                return _integrationServices;
            }
        }

        public IApplicationContext ApplicationContext { get; }

        public virtual string ComponentType { get; set; }

        public virtual string ServiceName { get; set; }

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
