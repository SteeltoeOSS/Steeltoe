// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;

namespace Steeltoe.Stream.Binding;

public abstract class AbstractBindingLifecycle : ISmartLifecycle
{
    private readonly List<IBindable> _bindables;
    protected readonly IBindingService BindingService;

    private volatile bool _running;

    public virtual bool IsRunning => _running;

    public virtual bool IsAutoStartup => true;

    public virtual int Phase => int.MaxValue;

    protected AbstractBindingLifecycle(IBindingService bindingService, IEnumerable<IBindable> bindables)
    {
        BindingService = bindingService;
        _bindables = bindables.ToList();
    }

    public virtual async Task StartAsync()
    {
        if (!_running)
        {
            foreach (IBindable bindable in _bindables)
            {
                await Task.Run(() => DoStartWithBindable(bindable)).ConfigureAwait(false);
            }

            _running = true;
        }
    }

    public virtual async Task StopAsync()
    {
        if (_running)
        {
            foreach (IBindable bindable in _bindables)
            {
                await Task.Run(() => DoStopWithBindable(bindable)).ConfigureAwait(false);
            }

            _running = false;
        }
    }

    public async Task StopAsync(Action callback)
    {
        await StopAsync();
        callback?.Invoke();
    }

    protected abstract void DoStartWithBindable(IBindable bindable);

    protected abstract void DoStopWithBindable(IBindable bindable);
}
