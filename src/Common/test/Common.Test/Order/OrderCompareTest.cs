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
using Xunit;

namespace Steeltoe.Common.Test.Order
{
    public class OrderCompareTest
    {
        private readonly OrderComparer orderComparer = OrderComparer.Instance;

        [Fact]
        public void CompareOrderedInstancesBefore()
        {
            Assert.Equal(-1, orderComparer.Compare(new StubOrdered(100), new StubOrdered(2000)));
        }

        [Fact]
        public void CompareOrderedInstancesSame()
        {
            Assert.Equal(0, orderComparer.Compare(new StubOrdered(100), new StubOrdered(100)));
        }

        [Fact]
        public void CcompareOrderedInstancesAfter()
        {
            Assert.Equal(1, orderComparer.Compare(new StubOrdered(982300), new StubOrdered(100)));
        }

        [Fact]
        public void CompareOrderedInstancesNullFirst()
        {
            Assert.Equal(1, orderComparer.Compare(null, new StubOrdered(100)));
        }

        [Fact]
        public void CompareOrderedInstancesNullLast()
        {
            Assert.Equal(-1, orderComparer.Compare(new StubOrdered(100), null));
        }

        [Fact]
        public void CompareOrderedInstancesDoubleNull()
        {
            Assert.Equal(0, orderComparer.Compare(null, null));
        }

        private sealed class StubOrdered : IOrdered
        {
            private readonly int order;

            public StubOrdered(int order)
            {
                this.order = order;
            }

            public int Order
            {
                get { return order; }
            }
        }
    }
}
