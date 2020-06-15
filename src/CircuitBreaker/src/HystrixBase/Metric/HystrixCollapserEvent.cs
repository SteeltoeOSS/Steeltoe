// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCollapserEvent : IHystrixEvent
    {
        private readonly IHystrixCollapserKey collapserKey;
        private readonly CollapserEventType eventType;
        private readonly int count;

        protected HystrixCollapserEvent(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
        {
            this.collapserKey = collapserKey;
            this.eventType = eventType;
            this.count = count;
        }

        public static HystrixCollapserEvent From(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
        {
            return new HystrixCollapserEvent(collapserKey, eventType, count);
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return collapserKey; }
        }

        public CollapserEventType EventType
        {
            get { return eventType; }
        }

        public int Count
        {
            get { return count; }
        }

        public override string ToString()
        {
            return "HystrixCollapserEvent[" + collapserKey.Name + "] : " + eventType + " : " + count;
        }
    }
}
