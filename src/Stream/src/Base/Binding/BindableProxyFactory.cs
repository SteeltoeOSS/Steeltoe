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

using Steeltoe.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;

namespace Steeltoe.Stream.Binding
{
    public class BindableProxyFactory : AbstractBindableProxyFactory, IBindableProxyFactory
    {
        private readonly ConcurrentDictionary<MethodInfo, object> _targetCache = new ConcurrentDictionary<MethodInfo, object>();

        public BindableProxyFactory(Type bindingType, IEnumerable<IBindingTargetFactory> bindingTargetFactories)
            : base(bindingType, bindingTargetFactories)
        {
        }

        public virtual object Invoke(MethodInfo info)
        {
            if (_targetCache.TryGetValue(info, out var boundTarget))
            {
                return boundTarget;
            }

            var bindable = _bindables.Values.SingleOrDefault((c) => c.FactoryMethod == info);

            // Doesn't exist, return null
            if (bindable.Name == null)
            {
                return null;
            }

            // Otherwise, its valid and must be an Input or Output
            if (bindable.IsInput)
            {
                return _targetCache.GetOrAdd(info, _boundInputTargets[bindable.Name].Value);
            }
            else
            {
                return _targetCache.GetOrAdd(info, _boundOutputTargets[bindable.Name].Value);
            }
        }

        public virtual void ReplaceInputChannel(string originalChannelName, string newChannelName, ISubscribableChannel messageChannel)
        {
            _boundInputTargets.Remove(originalChannelName);
            var creator = new Lazy<object>(messageChannel);
            _boundInputTargets.Add(newChannelName, creator);
        }

        public virtual void ReplaceOutputChannel(string originalChannelName, string newChannelName, IMessageChannel messageChannel)
        {
            _boundOutputTargets.Remove(originalChannelName);
            var creator = new Lazy<object>(messageChannel);
            _boundOutputTargets.Add(newChannelName, creator);
        }
    }
}
