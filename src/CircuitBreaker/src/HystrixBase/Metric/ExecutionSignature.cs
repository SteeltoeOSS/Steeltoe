// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class ExecutionSignature
    {
        private readonly string _commandName;
        private readonly ExecutionResult.EventCounts _eventCounts;
        private readonly string _cacheKey;
        private readonly int _cachedCount;
        private readonly IHystrixCollapserKey _collapserKey;
        private readonly int _collapserBatchSize;

        private ExecutionSignature(IHystrixCommandKey commandKey, ExecutionResult.EventCounts eventCounts, string cacheKey, int cachedCount, IHystrixCollapserKey collapserKey, int collapserBatchSize)
        {
            this._commandName = commandKey.Name;
            this._eventCounts = eventCounts;
            this._cacheKey = cacheKey;
            this._cachedCount = cachedCount;
            this._collapserKey = collapserKey;
            this._collapserBatchSize = collapserBatchSize;
        }

        public static ExecutionSignature From(IHystrixInvokableInfo execution)
        {
            return new ExecutionSignature(execution.CommandKey, execution.EventCounts, null, 0, execution.OriginatingCollapserKey, execution.NumberCollapsed);
        }

        public static ExecutionSignature From(IHystrixInvokableInfo execution, string cacheKey, int cachedCount)
        {
            return new ExecutionSignature(execution.CommandKey, execution.EventCounts, cacheKey, cachedCount, execution.OriginatingCollapserKey, execution.NumberCollapsed);
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }

            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            ExecutionSignature that = (ExecutionSignature)o;

            if (!_commandName.Equals(that._commandName))
            {
                return false;
            }

            if (!_eventCounts.Equals(that._eventCounts))
            {
                return false;
            }

            return !(_cacheKey != null ? !_cacheKey.Equals(that._cacheKey) : that._cacheKey != null);
        }

        public override int GetHashCode()
        {
            int result = _commandName.GetHashCode();
            result = (31 * result) + _eventCounts.GetHashCode();
            result = (31 * result) + (_cacheKey != null ? _cacheKey.GetHashCode() : 0);
            return result;
        }

        public string CommandName
        {
            get { return _commandName; }
        }

        public ExecutionResult.EventCounts Eventcounts
        {
            get { return _eventCounts; }
        }

        public int CachedCount
        {
            get { return _cachedCount; }
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return _collapserKey; }
        }

        public int CollapserBatchSize
        {
            get { return _collapserBatchSize; }
        }
    }
}
