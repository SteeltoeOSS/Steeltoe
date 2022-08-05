// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

public abstract class AbstractBinding : IBinding
{
    public virtual IDictionary<string, object> ExtendedInfo => new Dictionary<string, object>();

    public virtual string Name => null;

    public virtual string BindingName => null;

    public virtual bool IsInput =>
        throw new InvalidOperationException($"Binding implementation `{GetType().Name}` must implement this operation before it is called");

    public virtual bool IsRunning => false;

    public virtual Task PauseAsync()
    {
        return StopAsync();
    }

    public virtual Task ResumeAsync()
    {
        return StartAsync();
    }

    public virtual Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task UnbindAsync()
    {
        return Task.CompletedTask;
    }
}
