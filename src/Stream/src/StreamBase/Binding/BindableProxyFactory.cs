// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;

namespace Steeltoe.Stream.Binding;

public class BindableProxyFactory : AbstractBindableProxyFactory, IBindableProxyFactory
{
    private readonly ConcurrentDictionary<MethodInfo, object> _targetCache = new ();

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