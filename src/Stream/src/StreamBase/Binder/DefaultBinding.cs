// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder;

public class DefaultBinding<T> : AbstractBinding
{
    protected readonly T Target;
    protected readonly ILifecycle Lifecycle;
    private readonly bool _restartable;

    private bool _paused;

    public DefaultBinding(string name, string group, T target, ILifecycle lifecycle)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        Name = name;
        Group = group;
        Target = target;
        Lifecycle = lifecycle;
        _restartable = !string.IsNullOrEmpty(group);
    }

    public DefaultBinding(string name, T target, ILifecycle lifecycle)
        : this(name, null, target, lifecycle)
    {
        _restartable = true;
    }

    public override string Name { get; }

    public virtual string Group { get; }

    public override string BindingName => Name;

    public virtual string State
    {
        get
        {
            var state = "N/A";
            if (Lifecycle != null)
            {
                if (IsPausable)
                {
                    state = _paused ? "paused" : RunningState;
                }
                else
                {
                    state = RunningState;
                }
            }

            return state;
        }
    }

    public override bool IsRunning
    {
        get { return Lifecycle != null && Lifecycle.IsRunning; }
    }

    public virtual bool IsPausable
    {
        get { return Lifecycle is IPausable; }
    }

    public override Task Start()
    {
        if (!IsRunning && Lifecycle != null && _restartable)
        {
            return Lifecycle.Start();
        } // else this.logger.warn("Can not re-bind an anonymous binding")

        return Task.CompletedTask;
    }

    public override Task Stop()
    {
        if (IsRunning)
        {
            return Lifecycle.Stop();
        }

        return Task.CompletedTask;
    }

    public override async Task Pause()
    {
        if (Lifecycle is IPausable pausable)
        {
            await pausable.Pause();
            _paused = true;
        }
        else
        {
            // this.logger.warn("Attempted to pause a component that does not support Pausable " + this.lifecycle);
        }
    }

    public override async Task Resume()
    {
        if (Lifecycle is IPausable pausable)
        {
            await pausable.Resume();
            _paused = false;
        }
        else
        {
            // this.logger.warn("Attempted to resume a component that does not support Pausable " + this.lifecycle);
        }
    }

    public override async Task Unbind()
    {
        await Stop();
        AfterUnbind();
    }

    protected internal virtual ILifecycle Endpoint
    {
        get { return Lifecycle; }
    }

    public override string ToString()
    {
        return $" Binding [name={Name}, target={Target}, lifecycle={Lifecycle}]";
    }

    protected virtual void AfterUnbind()
    {
    }

    private string RunningState
    {
        get { return IsRunning ? "running" : "stopped"; }
    }
}
