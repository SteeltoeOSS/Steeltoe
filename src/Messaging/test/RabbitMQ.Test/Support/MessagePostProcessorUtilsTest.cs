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
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class MessagePostProcessorUtilsTest
    {
        [Fact]
        public void TestOrderIng()
        {
            var pps = new MPP[]
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
            var iterator = sorted.GetEnumerator();

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

        private class POMPP : OMPP, IPriorityOrdered
        {
        }
    }
}
