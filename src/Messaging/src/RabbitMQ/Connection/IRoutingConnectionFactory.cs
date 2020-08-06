// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
    public interface IRoutingConnectionFactory
    {
        /// <summary>
        /// Returns the ConnectionFactory bound to given lookup key, or null if one does not exist.
        /// </summary>
        /// <param name="key">the lookup key to which the factory is bound</param>
        /// <returns>the factory if found</returns>
        IConnectionFactory GetTargetConnectionFactory(object key);
    }
}
