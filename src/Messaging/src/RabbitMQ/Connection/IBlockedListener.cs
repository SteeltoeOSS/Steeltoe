// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Events;
using System;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    // TODO: This is a RabbitMQ type
    public interface IBlockedListener
    {
        void HandleBlocked(object sender, ConnectionBlockedEventArgs args);

        void HandleUnblocked(object sender, EventArgs args);
    }
}
