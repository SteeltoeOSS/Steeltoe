// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Test.Support;

public class MessagePostProcessorUtilsTest
{
    [Fact]
    public void TestOrderIng()
    {
        MessagePostProcessor[] pps =
        {
            new(),
            new OrderedMessagePostProcessor().Order(3),
            new OrderedMessagePostProcessor().Order(1),
            new PriorityOrderedMessagePostProcessor().Order(6),
            new PriorityOrderedMessagePostProcessor().Order(2)
        };

        var list = new List<IMessagePostProcessor>();
        list.AddRange(pps);
        List<IMessagePostProcessor> sorted = MessagePostProcessorUtils.Sort(list);
        using List<IMessagePostProcessor>.Enumerator iterator = sorted.GetEnumerator();

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

        public OrderedMessagePostProcessor Order(int value)
        {
            _order = value;
            return this;
        }
    }

    private sealed class PriorityOrderedMessagePostProcessor : OrderedMessagePostProcessor, IPriorityOrdered
    {
    }
}
