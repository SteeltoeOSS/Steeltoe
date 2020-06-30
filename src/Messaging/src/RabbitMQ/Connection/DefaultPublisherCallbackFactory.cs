// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class DefaultPublisherCallbackFactory : IPublisherCallbackChannelFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DefaultPublisherCallbackFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IPublisherCallbackChannel CreateChannel(IModel channel) => new PublisherCallbackChannel(channel, _loggerFactory?.CreateLogger<PublisherCallbackChannel>());
    }
}
