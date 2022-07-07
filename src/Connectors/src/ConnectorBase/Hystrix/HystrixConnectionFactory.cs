// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Connector.Hystrix;

public class HystrixConnectionFactory
{
    public HystrixConnectionFactory(object realFactory)
    {
        ConnectionFactory = realFactory ?? throw new ArgumentNullException(nameof(realFactory));
    }

    public object ConnectionFactory { get; private set; }
}
