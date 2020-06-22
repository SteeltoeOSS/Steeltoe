﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    // TODO: This is an RabbitMQ type
    public interface IShutdownListener
    {
        void ShutdownCompleted(object sender, ShutdownEventArgs args);
    }
}
