// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser;

public class RequestCollapserFactory
{
    private readonly ICollapserTimer _timer;
    private readonly HystrixConcurrencyStrategy _concurrencyStrategy;

    public RequestCollapserFactory(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions properties)
    {
        /* strategy: ConcurrencyStrategy */
        _concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
        _timer = timer;
        Scope = scope;
        CollapserKey = collapserKey;
        Properties = properties;
    }

    public static void Reset()
    {
        GlobalScopedCollapsers.Clear();
        RequestScopedCollapsers.Clear();
        HystrixTimer.Reset();
    }

    internal static void ResetRequest()
    {
        RequestScopedCollapsers.Clear();
    }

    internal static RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> GetRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>(string key)
    {
        if (!RequestScopedCollapsers.TryGetValue(key, out var result))
        {
            return null;
        }

        return (RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>)result;
    }

    public IHystrixCollapserKey CollapserKey { get; }

    public RequestCollapserScope Scope { get; }

    public IHystrixCollapserOptions Properties { get; }

    public RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetRequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
    {
        if (Scope == RequestCollapserScope.REQUEST)
        {
            return GetCollapserForUserRequest(commandCollapser);
        }
        else if (Scope == RequestCollapserScope.GLOBAL)
        {
            return GetCollapserForGlobalScope(commandCollapser);
        }
        else
        {
            // logger.warn("Invalid Scope: {}  Defaulting to REQUEST scope.", getScope());
            return GetCollapserForUserRequest(commandCollapser);
        }
    }

    // String is CollapserKey.name() (we can't use CollapserKey directly as we can't guarantee it implements hashcode/equals correctly)
    private static readonly ConcurrentDictionary<string, object> GlobalScopedCollapsers = new ();

    private RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetCollapserForGlobalScope<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
    {
        var result = GlobalScopedCollapsers.GetOrAddEx(CollapserKey.Name, _ => new RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, Properties, _timer, _concurrencyStrategy));
        return (RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>)result;
    }

    // String is HystrixCollapserKey.name() (we can't use HystrixCollapserKey directly as we can't guarantee it implements hashcode/equals correctly)
    private static readonly ConcurrentDictionary<string, object> RequestScopedCollapsers = new ();

    private RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetCollapserForUserRequest<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
    {
        return GetRequestVariableForCommand(commandCollapser).Value;
    }

    private RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> GetRequestVariableForCommand<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
    {
        var result = RequestScopedCollapsers.GetOrAddEx(commandCollapser.CollapserKey.Name, _ => new RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, Properties, _timer, _concurrencyStrategy));
        return (RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>)result;
    }

    internal sealed class RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> : HystrixRequestVariableDefault<RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>>
    {
        public RequestCollapserRequestVariable(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser, IHystrixCollapserOptions properties, ICollapserTimer timer, HystrixConcurrencyStrategy concurrencyStrategy)
            : base(() => new RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, properties, timer, concurrencyStrategy), collapser => collapser.Shutdown())
        {
        }
    }
}
