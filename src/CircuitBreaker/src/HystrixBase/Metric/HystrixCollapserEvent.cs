// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCollapserEvent : IHystrixEvent
    {
        private readonly IHystrixCollapserKey _collapserKey;
        private readonly CollapserEventType _eventType;
        private readonly int _count;

        protected HystrixCollapserEvent(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
        {
            this._collapserKey = collapserKey;
            this._eventType = eventType;
            this._count = count;
        }

        public static HystrixCollapserEvent From(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
        {
            return new HystrixCollapserEvent(collapserKey, eventType, count);
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return _collapserKey; }
        }

        public CollapserEventType EventType
        {
            get { return _eventType; }
        }

        public int Count
        {
            get { return _count; }
        }

        public override string ToString()
        {
            return "HystrixCollapserEvent[" + _collapserKey.Name + "] : " + _eventType + " : " + _count;
        }
    }
}
