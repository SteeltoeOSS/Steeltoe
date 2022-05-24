// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Support
{
    public class MessagePostProcessorUtilsTest
    {
        [Fact]
        public void TestOrderIng()
        {
            var pps = new[]
            {
                new MPP(),
                new OMPP().Order(3),
                new OMPP().Order(1),
                new POMPP().Order(6),
                new POMPP().Order(2)
            };
            var list = new List<IMessagePostProcessor>();
            list.AddRange(pps);
            var sorted = MessagePostProcessorUtils.Sort(list);
            using var iterator = sorted.GetEnumerator();

            iterator.MoveNext();
            Assert.IsType<POMPP>(iterator.Current);
            Assert.Equal(2, ((IOrdered)iterator.Current).Order);

            iterator.MoveNext();
            Assert.IsType<POMPP>(iterator.Current);
            Assert.Equal(6, ((IOrdered)iterator.Current).Order);

            iterator.MoveNext();
            Assert.IsType<OMPP>(iterator.Current);
            Assert.Equal(1, ((IOrdered)iterator.Current).Order);

            iterator.MoveNext();
            Assert.IsType<OMPP>(iterator.Current);
            Assert.Equal(3, ((IOrdered)iterator.Current).Order);

            iterator.MoveNext();
            Assert.IsType<MPP>(iterator.Current);
        }

        private class MPP : IMessagePostProcessor
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

        private class OMPP : MPP, IOrdered
        {
            private int _order;

            int IOrdered.Order => _order;

            public OMPP Order(int order)
            {
                _order = order;
                return this;
            }
        }

        private sealed class POMPP : OMPP, IPriorityOrdered
        {
        }
    }
}
