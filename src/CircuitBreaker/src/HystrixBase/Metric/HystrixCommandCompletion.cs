// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class HystrixCommandCompletion : HystrixCommandEvent
{
    protected readonly ExecutionResult ExecutionResult;
    protected readonly HystrixRequestContext InnerRequestContext;

    private static readonly IList<HystrixEventType> AllEventTypes = HystrixEventTypeHelper.Values;

    internal HystrixCommandCompletion(
        ExecutionResult executionResult,
        IHystrixCommandKey commandKey,
        IHystrixThreadPoolKey threadPoolKey,
        HystrixRequestContext requestContext)
        : base(commandKey, threadPoolKey)
    {
        this.ExecutionResult = executionResult;
        this.InnerRequestContext = requestContext;
    }

    public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
    {
        return From(executionResult, commandKey, threadPoolKey, HystrixRequestContext.ContextForCurrentThread);
    }

    public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, HystrixRequestContext requestContext)
    {
        return new HystrixCommandCompletion(executionResult, commandKey, threadPoolKey, requestContext);
    }

    public override bool IsResponseThreadPoolRejected
    {
        get { return ExecutionResult.IsResponseThreadPoolRejected; }
    }

    public override bool IsExecutionStart
    {
        get { return false; }
    }

    public override bool IsExecutedInThread
    {
        get { return ExecutionResult.IsExecutedInThread; }
    }

    public override bool IsCommandCompletion
    {
        get { return true; }
    }

    public HystrixRequestContext RequestContext
    {
        get { return InnerRequestContext; }
    }

    public ExecutionResult.EventCounts Eventcounts
    {
        get { return ExecutionResult.Eventcounts; }
    }

    public long ExecutionLatency
    {
        get { return ExecutionResult.ExecutionLatency; }
    }

    public long TotalLatency
    {
        get { return ExecutionResult.UserThreadLatency; }
    }

    public override bool DidCommandExecute
    {
        get { return ExecutionResult.ExecutionOccurred; }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var foundEventTypes = new List<HystrixEventType>();

        sb.Append(CommandKey.Name).Append('[');
        foreach (var eventType in AllEventTypes)
        {
            if (ExecutionResult.Eventcounts.Contains(eventType))
            {
                foundEventTypes.Add(eventType);
            }
        }

        var i = 0;
        foreach (var eventType in foundEventTypes)
        {
            sb.Append(eventType.ToString());
            var eventCount = ExecutionResult.Eventcounts.GetCount(eventType);
            if (eventCount > 1)
            {
                sb.Append('x').Append(eventCount);
            }

            if (i < foundEventTypes.Count - 1)
            {
                sb.Append(", ");
            }

            i++;
        }

        sb.Append("][").Append(ExecutionLatency).Append(" ms]");
        return sb.ToString();
    }
}
