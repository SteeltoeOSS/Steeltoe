// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class SimpleRoutingConnectionFactory : AbstractRoutingConnectionFactory
    {
        public override string ServiceName { get; set; } = "SimpleRoutingConnectionFactory";

        public override object DetermineCurrentLookupKey()
        {
            return SimpleResourceHolder.Get(this);
        }
    }
}
