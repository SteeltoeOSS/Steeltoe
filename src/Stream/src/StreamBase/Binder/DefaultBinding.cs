// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;

namespace Steeltoe.Stream.Binder;

public class DefaultBinding<T> : AbstractBinding
{
    private readonly bool _restartable;
    protected readonly T Target;
    protected readonly ILifecycle Lifecycle;

    private bool _paused;

    private string RunningState => IsRunning ? "running" : "stopped";

    protected internal virtual ILifecycle Endpoint => Lifecycle;

    public override string Name { get; }

    public virtual string Group { get; }

    public override string BindingName => Name;

    public virtual string State
    {
        get
        {
            string state = "N/A";

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

    public override bool IsRunning => Lifecycle != null && Lifecycle.IsRunning;

    public virtual bool IsPausable => Lifecycle is IPausable;

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
    }

    public override async Task Resume()
    {
        if (Lifecycle is IPausable pausable)
        {
            await pausable.Resume();
            _paused = false;
        }
    }

    public override async Task Unbind()
    {
        await Stop();
        AfterUnbind();
    }

    public override string ToString()
    {
        return $" Binding [name={Name}, target={Target}, lifecycle={Lifecycle}]";
    }

    protected virtual void AfterUnbind()
    {
    }
}
