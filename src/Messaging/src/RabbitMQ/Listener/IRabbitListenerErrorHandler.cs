// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public interface IRabbitListenerErrorHandler : IServiceNameAware
    {
        object HandleError(IMessage amqpMessage, IMessage message, ListenerExecutionFailedException exception);
    }
}
