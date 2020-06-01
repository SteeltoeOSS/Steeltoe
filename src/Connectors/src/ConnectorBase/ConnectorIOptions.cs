// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.CloudFoundry.Connector
{
    public class ConnectorIOptions<T> : IOptions<T>
        where T : class, new()
    {
        public ConnectorIOptions(T value)
        {
            Value = value;
        }

        public T Value { get; internal protected set; }
    }
}
