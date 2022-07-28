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

    internal static RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument> GetRequestVariable<TBatchReturn, TResponse, TRequestArgument>(string key)
    {
        if (!RequestScopedCollapsers.TryGetValue(key, out var result))
        {
            return null;
        }

        return (RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument>)result;
    }

    public IHystrixCollapserKey CollapserKey { get; }

    public RequestCollapserScope Scope { get; }

    public IHystrixCollapserOptions Properties { get; }

    public RequestCollapser<TBatchReturn, TResponse, TRequestArgument> GetRequestCollapser<TBatchReturn, TResponse, TRequestArgument>(HystrixCollapser<TBatchReturn, TResponse, TRequestArgument> commandCollapser)
    {
        if (Scope == RequestCollapserScope.Request)
        {
            return GetCollapserForUserRequest(commandCollapser);
        }
        else if (Scope == RequestCollapserScope.Global)
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

    private RequestCollapser<TBatchReturn, TResponse, TRequestArgument> GetCollapserForGlobalScope<TBatchReturn, TResponse, TRequestArgument>(HystrixCollapser<TBatchReturn, TResponse, TRequestArgument> commandCollapser)
    {
        var result = GlobalScopedCollapsers.GetOrAddEx(CollapserKey.Name, _ => new RequestCollapser<TBatchReturn, TResponse, TRequestArgument>(commandCollapser, Properties, _timer, _concurrencyStrategy));
        return (RequestCollapser<TBatchReturn, TResponse, TRequestArgument>)result;
    }

    // String is HystrixCollapserKey.name() (we can't use HystrixCollapserKey directly as we can't guarantee it implements hashcode/equals correctly)
    private static readonly ConcurrentDictionary<string, object> RequestScopedCollapsers = new ();

    private RequestCollapser<TBatchReturn, TResponse, TRequestArgument> GetCollapserForUserRequest<TBatchReturn, TResponse, TRequestArgument>(HystrixCollapser<TBatchReturn, TResponse, TRequestArgument> commandCollapser)
    {
        return GetRequestVariableForCommand(commandCollapser).Value;
    }

    private RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument> GetRequestVariableForCommand<TBatchReturn, TResponse, TRequestArgument>(HystrixCollapser<TBatchReturn, TResponse, TRequestArgument> commandCollapser)
    {
        var result = RequestScopedCollapsers.GetOrAddEx(commandCollapser.CollapserKey.Name, _ => new RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument>(commandCollapser, Properties, _timer, _concurrencyStrategy));
        return (RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument>)result;
    }

    internal sealed class RequestCollapserRequestVariable<TBatchReturn, TResponse, TRequestArgument> : HystrixRequestVariableDefault<RequestCollapser<TBatchReturn, TResponse, TRequestArgument>>
    {
        public RequestCollapserRequestVariable(HystrixCollapser<TBatchReturn, TResponse, TRequestArgument> commandCollapser, IHystrixCollapserOptions properties, ICollapserTimer timer, HystrixConcurrencyStrategy concurrencyStrategy)
            : base(() => new RequestCollapser<TBatchReturn, TResponse, TRequestArgument>(commandCollapser, properties, timer, concurrencyStrategy), collapser => collapser.Shutdown())
        {
        }
    }
}
