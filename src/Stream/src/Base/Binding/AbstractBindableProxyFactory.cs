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
using System.Linq;

namespace Steeltoe.Stream.Binding
{
    public class AbstractBindableProxyFactory : AbstractBindable
    {
        protected IDictionary<string, Bindable> _bindables;
        protected IList<IBindingTargetFactory> _bindingTargetFactories;

        protected Dictionary<string, Lazy<object>> _boundInputTargets = new Dictionary<string, Lazy<object>>();
        protected Dictionary<string, Lazy<object>> _boundOutputTargets = new Dictionary<string, Lazy<object>>();

        public AbstractBindableProxyFactory(Type bindingType, IEnumerable<IBindingTargetFactory> bindingTargetFactories)
            : base(bindingType)
        {
            _bindingTargetFactories = bindingTargetFactories.ToList();
            Initialize();
        }

        public override ICollection<string> Inputs => _boundInputTargets.Keys;

        public override ICollection<string> Outputs => _boundOutputTargets.Keys;

        public override ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService)
        {
            var bindings = new List<IBinding>();
            foreach (var boundTarget in _boundInputTargets)
            {
                var result = bindingService.BindConsumer(boundTarget.Value.Value, boundTarget.Key);
                bindings.AddRange(result);
            }

            return bindings;
        }

        public override ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService)
        {
            var bindings = new List<IBinding>();
            foreach (var boundTarget in _boundOutputTargets)
            {
                var result = bindingService.BindProducer(boundTarget.Value.Value, boundTarget.Key);
                bindings.Add(result);
            }

            return bindings;
        }

        public override void UnbindInputs(IBindingService bindingService)
        {
            foreach (var boundTarget in _boundInputTargets)
            {
                bindingService.UnbindConsumers(boundTarget.Key);
            }
        }

        public override void UnbindOutputs(IBindingService bindingService)
        {
            foreach (var boundTarget in _boundOutputTargets)
            {
                bindingService.UnbindProducers(boundTarget.Key);
            }
        }

        public override object GetBoundTarget(string name)
        {
            var result = GetBoundInputTarget(name);
            if (result == null)
            {
                result = GetBoundOutputTarget(name);
            }

            return result;
        }

        public override object GetBoundInputTarget(string name)
        {
            _boundInputTargets.TryGetValue(name, out var result);
            return result.Value;
        }

        public override object GetBoundOutputTarget(string name)
        {
            _boundOutputTargets.TryGetValue(name, out var result);
            return result.Value;
        }

        protected void Initialize()
        {
            _bindables = BindingHelpers.CollectBindables(BindingType);
            foreach (var bindable in _bindables.Values)
            {
                var factory = GetBindingTargetFactory(bindable.BindingTargetType);
                if (bindable.IsInput)
                {
                    var creator = new Lazy<object>(() => factory.CreateInput(bindable.Name));
                    _boundInputTargets.Add(bindable.Name, creator);
                }
                else
                {
                    var creator = new Lazy<object>(() => factory.CreateOutput(bindable.Name));
                    _boundOutputTargets.Add(bindable.Name, creator);
                }
            }
        }

        protected virtual IBindingTargetFactory GetBindingTargetFactory(Type bindingTargetType)
        {
            IBindingTargetFactory result = null;

            foreach (var factory in _bindingTargetFactories)
            {
                if (factory.CanCreate(bindingTargetType))
                {
                    if (result == null)
                    {
                        result = factory;
                    }
                    else
                    {
                        throw new InvalidOperationException("Multiple factories found for binding target type: " + bindingTargetType);
                    }
                }
            }

            if (result == null)
            {
                throw new InvalidOperationException("No factory found for binding target type: " + bindingTargetType);
            }

            return result;
        }
    }
}
