// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public class SimpleRabbitListenerEndpoint : AbstractRabbitListenerEndpoint
    {
        public IMessageListener MessageListener { get; set; }

        public SimpleRabbitListenerEndpoint(IApplicationContext context, IMessageListener listener = null, ILoggerFactory loggerFactory = null)
            : base(context, loggerFactory)
        {
            MessageListener = listener;
        }

        protected override IMessageListener CreateMessageListener(IMessageListenerContainer container)
        {
            return MessageListener;
        }

        protected override StringBuilder GetEndpointDescription()
        {
            return base.GetEndpointDescription()
                    .Append(" | messageListener='").Append(MessageListener).Append('\'');
        }
    }
}
