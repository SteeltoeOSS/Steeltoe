// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Events;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
    public interface IRecoveryListener
    {
        void HandleConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs args);

        void HandleRecoverySucceeded(object sender, EventArgs args);
    }
}
