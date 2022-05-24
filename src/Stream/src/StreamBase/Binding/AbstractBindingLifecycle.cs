// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binding
{
    public abstract class AbstractBindingLifecycle : ISmartLifecycle
    {
        protected readonly IBindingService _bindingService;

        private readonly List<IBindable> _bindables;

        private volatile bool _running;

        protected AbstractBindingLifecycle(IBindingService bindingService, IEnumerable<IBindable> bindables)
        {
            _bindingService = bindingService;
            _bindables = bindables.ToList();
        }

        public virtual async Task Start()
        {
            if (!_running)
            {
                foreach (var bindable in _bindables)
                {
                    await Task.Run(() => DoStartWithBindable(bindable)).ConfigureAwait(false);
                }

                _running = true;
            }
        }

        public virtual bool IsRunning
        {
            get { return _running; }
        }

        public virtual bool IsAutoStartup
        {
            get { return true; }
        }

        public virtual int Phase => int.MaxValue;

        public virtual async Task Stop()
        {
            if (_running)
            {
                foreach (var bindable in _bindables)
                {
                    await Task.Run(() => DoStopWithBindable(bindable)).ConfigureAwait(false);
                }

                _running = false;
            }
        }

        public async Task Stop(Action callback)
        {
            await Stop();
            callback?.Invoke();
        }

        protected abstract void DoStartWithBindable(IBindable bindable);

        protected abstract void DoStopWithBindable(IBindable bindable);
    }
}
