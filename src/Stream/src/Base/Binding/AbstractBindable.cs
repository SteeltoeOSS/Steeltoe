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

using Steeltoe.Stream.Binder;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    public abstract class AbstractBindable : IBindable
    {
        private static readonly ICollection<IBinding> _bindings = new List<IBinding>();
        private static readonly ICollection<string> _empty = new List<string>();

        protected AbstractBindable()
        {
        }

        protected AbstractBindable(Type binding)
        {
            BindingType = binding;
        }

        public virtual Type BindingType { get; }

        public virtual ICollection<string> Inputs => _empty;

        public virtual ICollection<string> Outputs => _empty;

        public virtual ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService)
        {
            return _bindings;
        }

        public virtual ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService)
        {
            return _bindings;
        }

        public virtual object GetBoundInputTarget(string name)
        {
            return null;
        }

        public virtual object GetBoundOutputTarget(string name)
        {
            return null;
        }

        public virtual object GetBoundTarget(string name)
        {
            return null;
        }

        public virtual void UnbindInputs(IBindingService bindingService)
        {
        }

        public virtual void UnbindOutputs(IBindingService bindingService)
        {
        }
    }
}
