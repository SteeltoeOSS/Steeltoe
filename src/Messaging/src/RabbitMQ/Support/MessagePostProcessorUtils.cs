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

using Steeltoe.Common.Order;
using Steeltoe.Messaging.Rabbit.Core;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public static class MessagePostProcessorUtils
    {
        public static List<IMessagePostProcessor> Sort(List<IMessagePostProcessor> processors)
        {
            var priorityOrdered = new List<IPriorityOrdered>();
            var ordered = new List<IOrdered>();
            var unOrdered = new List<IMessagePostProcessor>();
            foreach (var processor in processors)
            {
                if (processor is IPriorityOrdered)
                {
                    priorityOrdered.Add((IPriorityOrdered)processor);
                }
                else if (processor is IOrdered)
                {
                    ordered.Add((IOrdered)processor);
                }
                else
                {
                    unOrdered.Add(processor);
                }
            }

            var sorted = new List<IMessagePostProcessor>();

            priorityOrdered.Sort(OrderComparer.Instance);
            sorted.AddRange(priorityOrdered.Select((o) => (IMessagePostProcessor)o));

            ordered.Sort(OrderComparer.Instance);
            sorted.AddRange(ordered.Select((o) => (IMessagePostProcessor)o));

            sorted.AddRange(unOrdered);
            return sorted;
        }
    }
}
