// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binding;

public class BindableProxyFactory : AbstractBindableProxyFactory, IBindableProxyFactory
{
    private readonly ConcurrentDictionary<MethodInfo, object> _targetCache = new();

    public BindableProxyFactory(Type bindingType, IEnumerable<IBindingTargetFactory> bindingTargetFactories)
        : base(bindingType, bindingTargetFactories)
    {
    }

    public virtual object Invoke(MethodInfo info)
    {
        if (_targetCache.TryGetValue(info, out object boundTarget))
        {
            return boundTarget;
        }

        Bindable bindable = bindables.Values.SingleOrDefault(c => c.FactoryMethod == info);

        // Doesn't exist, return null
        if (bindable.Name == null)
        {
            return null;
        }

        // Otherwise, its valid and must be an Input or Output
        if (bindable.IsInput)
        {
            return _targetCache.GetOrAdd(info, boundInputTargets[bindable.Name].Value);
        }

        return _targetCache.GetOrAdd(info, boundOutputTargets[bindable.Name].Value);
    }

    public virtual void ReplaceInputChannel(string originalChannelName, string newChannelName, ISubscribableChannel messageChannel)
    {
        boundInputTargets.Remove(originalChannelName);
        var creator = new Lazy<object>(messageChannel);
        boundInputTargets.Add(newChannelName, creator);
    }

    public virtual void ReplaceOutputChannel(string originalChannelName, string newChannelName, IMessageChannel messageChannel)
    {
        boundOutputTargets.Remove(originalChannelName);
        var creator = new Lazy<object>(messageChannel);
        boundOutputTargets.Add(newChannelName, creator);
    }
}
