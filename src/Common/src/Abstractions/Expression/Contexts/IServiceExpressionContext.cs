// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public interface IServiceExpressionContext
    {
        IApplicationContext ApplicationContext { get; }

        bool ContainsService(string serviceName);

        bool ContainsService(string serviceName, Type serviceType);

        object GetService(string serviceName);

        object GetService(string serviceName, Type serviceType);
    }
}
