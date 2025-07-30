// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Connection;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class SimpleRoutingConnectionFactory : AbstractRoutingConnectionFactory
{
    public override string ServiceName { get; set; } = "SimpleRoutingConnectionFactory";

    public override object DetermineCurrentLookupKey()
    {
        return SimpleResourceHolder.Get(this);
    }
}