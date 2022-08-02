// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Services;
using Steeltoe.Integration.Util;

namespace Steeltoe.Integration.Endpoint;

public abstract class AbstractEndpoint : ISmartLifecycle, IServiceNameAware
{
    private readonly object _lifecycleLock = new();
    private IIntegrationServices _integrationServices;

    public IIntegrationServices IntegrationServices
    {
        get
        {
            _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
            return _integrationServices;
        }
    }

    public IApplicationContext ApplicationContext { get; }

    public virtual string ComponentType { get; set; }

    public virtual string ServiceName { get; set; }

    public virtual string ComponentName { get; set; }

    public bool IsAutoStartup { get; set; } = true;

    public bool IsRunning { get; set; }

    public int Phase { get; set; }

    protected AbstractEndpoint(IApplicationContext context)
    {
        ApplicationContext = context;
        ServiceName = $"{GetType().FullName}.{Guid.NewGuid()}";
    }

    public Task Start()
    {
        bool doTheStart = false;

        lock (_lifecycleLock)
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
        bool doTheStop = false;

        lock (_lifecycleLock)
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
        bool doTheStop = false;

        lock (_lifecycleLock)
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
