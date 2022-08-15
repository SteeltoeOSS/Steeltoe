// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class HystrixCommandCompletion : HystrixCommandEvent
{
    private static readonly IList<HystrixEventType> AllEventTypes = HystrixEventTypeHelper.Values;
    protected readonly ExecutionResult ExecutionResult;
    protected readonly HystrixRequestContext InnerRequestContext;

    public override bool IsResponseThreadPoolRejected => ExecutionResult.IsResponseThreadPoolRejected;

    public override bool IsExecutionStart => false;

    public override bool IsExecutedInThread => ExecutionResult.IsExecutedInThread;

    public override bool IsCommandCompletion => true;

    public HystrixRequestContext RequestContext => InnerRequestContext;

    public ExecutionResult.EventCounts Eventcounts => ExecutionResult.Eventcounts;

    public long ExecutionLatency => ExecutionResult.ExecutionLatency;

    public long TotalLatency => ExecutionResult.UserThreadLatency;

    public override bool DidCommandExecute => ExecutionResult.ExecutionOccurred;

    internal HystrixCommandCompletion(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey,
        HystrixRequestContext requestContext)
        : base(commandKey, threadPoolKey)
    {
        ExecutionResult = executionResult;
        InnerRequestContext = requestContext;
    }

    public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
    {
        return From(executionResult, commandKey, threadPoolKey, HystrixRequestContext.ContextForCurrentThread);
    }

    public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey,
        HystrixRequestContext requestContext)
    {
        return new HystrixCommandCompletion(executionResult, commandKey, threadPoolKey, requestContext);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var foundEventTypes = new List<HystrixEventType>();

        sb.Append(CommandKey.Name).Append('[');

        foreach (HystrixEventType eventType in AllEventTypes)
        {
            if (ExecutionResult.Eventcounts.Contains(eventType))
            {
                foundEventTypes.Add(eventType);
            }
        }

        int i = 0;

        foreach (HystrixEventType eventType in foundEventTypes)
        {
            sb.Append(eventType.ToString());
            int eventCount = ExecutionResult.Eventcounts.GetCount(eventType);

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
