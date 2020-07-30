// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Rabbit.Core
{
    public enum AcknowledgeMode
    {
        /// <summary>
        /// No acks
        /// </summary>
        NONE,

        /// <summary>
        /// Manual acks - user must ack/nack via a channel aware listener.
        /// </summary>
        MANUAL,

        /// <summary>
        /// The container will issue the ack/nack based on whether
        /// the listener returns normally, or throws an exception.
        /// </summary>
        AUTO
    }
}
