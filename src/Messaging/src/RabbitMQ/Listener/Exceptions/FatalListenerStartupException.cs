// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;

public class FatalListenerStartupException : Exception
{
    public FatalListenerStartupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
