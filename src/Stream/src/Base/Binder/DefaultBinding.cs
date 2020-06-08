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

using Steeltoe.Common.Lifecycle;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder
{
    public class DefaultBinding<T> : AbstractBinding
    {
        protected readonly T _target;
        protected readonly ILifecycle _lifecycle;
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
            _target = target;
            _lifecycle = lifecycle;
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
                if (_lifecycle != null)
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
            get { return _lifecycle != null && _lifecycle.IsRunning; }
        }

        public virtual bool IsPausable
        {
            get { return _lifecycle is IPausable; }
        }

        public override Task Start()
        {
            if (!IsRunning && _lifecycle != null && _restartable)
            {
                return _lifecycle.Start();
            }  // else this.logger.warn("Can not re-bind an anonymous binding");
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            if (IsRunning)
            {
                return _lifecycle.Stop();
            }

            return Task.CompletedTask;
        }

        public override async Task Pause()
        {
            if (_lifecycle is IPausable)
            {
                await ((IPausable)_lifecycle).Pause();
                _paused = true;
            }
            else
            {
                // this.logger.warn("Attempted to pause a component that does not support Pausable " + this.lifecycle);
            }
        }

        public override async Task Resume()
        {
            if (_lifecycle is IPausable)
            {
                await ((IPausable)_lifecycle).Resume();
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
            get { return _lifecycle; }
        }

        public override string ToString()
        {
            return " Binding [name=" + Name + ", target=" + _target + ", lifecycle="
                    + _lifecycle.ToString()
                + "]";
        }

        protected virtual void AfterUnbind()
        {
        }

        private string RunningState
        {
            get { return IsRunning ? "running" : "stopped"; }
        }
    }
}
