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

using System.Collections.Generic;

namespace Steeltoe.Common.Order
{
    public class OrderComparer : IComparer<IOrdered>
    {
        public static OrderComparer Instance { get; } = new OrderComparer();

        public int Compare(IOrdered o1, IOrdered o2)
        {
            var p1 = o1 is IPriorityOrdered;
            var p2 = o2 is IPriorityOrdered;

            if (p1 && !p2)
            {
                return -1;
            }
            else if (p2 && !p1)
            {
                return 1;
            }

            var i1 = GetOrder(o1);
            var i2 = GetOrder(o2);

            return GetOrder(i1, i2);
        }

        protected int GetOrder(int i1, int i2)
        {
#pragma warning disable S3358 // Ternary operators should not be nested
            return (i1 < i2) ? -1 : ((i1 == i2) ? 0 : 1);
#pragma warning restore S3358 // Ternary operators should not be nested
        }

        protected int GetOrder(IOrdered o1)
        {
            if (o1 != null)
            {
                return o1.Order;
            }

            return AbstractOrdered.LOWEST_PRECEDENCE;
        }
    }
}
