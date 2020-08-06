// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Core;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.RabbitMQ.Support
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
