// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IRabbitListenerErrorHandler : IServiceNameAware
    {
        object HandleError(Message amqpMessage, IMessage message, ListenerExecutionFailedException exception);
    }
}
