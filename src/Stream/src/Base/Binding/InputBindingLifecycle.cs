﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.Stream.Binder;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    public class InputBindingLifecycle : AbstractBindingLifecycle
    {
        internal List<IBinding> _inputBindings = new List<IBinding>();

        public InputBindingLifecycle(IBindingService bindingService, IEnumerable<IBindable> bindables)
        : base(bindingService, bindables)
        {
        }

        public override int Phase { get; } = int.MaxValue - 1000;

        protected override void DoStartWithBindable(IBindable bindable)
        {
            var bindableBindings = bindable.CreateAndBindInputs(_bindingService);

            if (bindableBindings != null)
            {
                _inputBindings.AddRange(bindableBindings);
            }
        }

        protected override void DoStopWithBindable(IBindable bindable)
        {
            bindable.UnbindInputs(_bindingService);
        }
    }
}
