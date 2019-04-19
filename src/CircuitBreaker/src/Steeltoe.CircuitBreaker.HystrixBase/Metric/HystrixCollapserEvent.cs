// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
