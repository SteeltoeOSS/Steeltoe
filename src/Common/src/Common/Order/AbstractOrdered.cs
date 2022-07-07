// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Order;

public class AbstractOrdered : IOrdered
{
    public const int HIGHEST_PRECEDENCE = int.MinValue;
    public const int LOWEST_PRECEDENCE = int.MaxValue;

    public AbstractOrdered()
    {
        Order = 0;
    }

    public AbstractOrdered(int order)
    {
        Order = order;
    }

    public int Order { get; private set; }
}
