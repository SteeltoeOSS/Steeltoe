// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class MessagePostProcessorUtilsTest
{
    [Fact]
    public void TestOrderIng()
    {
        var pps = new[]
        {
            new MessagePostProcessor(),
            new OrderedMessagePostProcessor().Order(3),
            new OrderedMessagePostProcessor().Order(1),
            new PriorityOrderedMessagePostProcessor().Order(6),
            new PriorityOrderedMessagePostProcessor().Order(2)
        };
        var list = new List<IMessagePostProcessor>();
        list.AddRange(pps);
        var sorted = MessagePostProcessorUtils.Sort(list);
        using var iterator = sorted.GetEnumerator();

        iterator.MoveNext();
        Assert.IsType<PriorityOrderedMessagePostProcessor>(iterator.Current);
        Assert.Equal(2, ((IOrdered)iterator.Current).Order);

        iterator.MoveNext();
        Assert.IsType<PriorityOrderedMessagePostProcessor>(iterator.Current);
        Assert.Equal(6, ((IOrdered)iterator.Current).Order);

        iterator.MoveNext();
        Assert.IsType<OrderedMessagePostProcessor>(iterator.Current);
        Assert.Equal(1, ((IOrdered)iterator.Current).Order);

        iterator.MoveNext();
        Assert.IsType<OrderedMessagePostProcessor>(iterator.Current);
        Assert.Equal(3, ((IOrdered)iterator.Current).Order);

        iterator.MoveNext();
        Assert.IsType<MessagePostProcessor>(iterator.Current);
    }

    private class MessagePostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message)
        {
            return null;
        }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return null;
        }
    }

    private class OrderedMessagePostProcessor : MessagePostProcessor, IOrdered
    {
        private int _order;

        int IOrdered.Order => _order;

        public OrderedMessagePostProcessor Order(int order)
        {
            _order = order;
            return this;
        }
    }

    private sealed class PriorityOrderedMessagePostProcessor : OrderedMessagePostProcessor, IPriorityOrdered
    {
    }
}
