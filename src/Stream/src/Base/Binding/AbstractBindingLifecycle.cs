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

        public async virtual Task Start()
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

        public async virtual Task Stop()
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
