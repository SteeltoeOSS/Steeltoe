// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

public class HandlerMethodArgumentResolverComposite : IHandlerMethodArgumentResolver
{
    private readonly List<IHandlerMethodArgumentResolver> _argumentResolvers = new();

    private readonly ConcurrentDictionary<ParameterInfo, IHandlerMethodArgumentResolver> _argumentResolverCache = new();

    public int Count => _argumentResolvers.Count;

    public ICollection<IHandlerMethodArgumentResolver> Resolvers => new List<IHandlerMethodArgumentResolver>(_argumentResolvers);

    public HandlerMethodArgumentResolverComposite AddResolver(IHandlerMethodArgumentResolver argumentResolver)
    {
        _argumentResolvers.Add(argumentResolver);
        return this;
    }

    public HandlerMethodArgumentResolverComposite AddResolvers(params IHandlerMethodArgumentResolver[] resolvers)
    {
        if (resolvers != null)
        {
            _argumentResolvers.AddRange(resolvers);
        }

        return this;
    }

    public HandlerMethodArgumentResolverComposite AddResolvers(ICollection<IHandlerMethodArgumentResolver> resolvers)
    {
        if (resolvers != null)
        {
            _argumentResolvers.AddRange(resolvers);
        }

        return this;
    }

    public void Clear()
    {
        _argumentResolvers.Clear();
    }

    public bool SupportsParameter(ParameterInfo parameter)
    {
        return GetArgumentResolver(parameter) != null;
    }

    public object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        IHandlerMethodArgumentResolver resolver = GetArgumentResolver(parameter);

        if (resolver == null)
        {
            throw new InvalidOperationException(
                $"Unsupported parameter type [{parameter.ParameterType.Name}]. {nameof(SupportsParameter)} should be called first.");
        }

        return resolver.ResolveArgument(parameter, message);
    }

    private IHandlerMethodArgumentResolver GetArgumentResolver(ParameterInfo parameter)
    {
        if (!_argumentResolverCache.TryGetValue(parameter, out IHandlerMethodArgumentResolver result))
        {
            foreach (IHandlerMethodArgumentResolver resolver in _argumentResolvers)
            {
                if (resolver.SupportsParameter(parameter))
                {
                    result = resolver;
                    _argumentResolverCache.TryAdd(parameter, result);
                    break;
                }
            }
        }

        return result;
    }
}
