// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration
{
    public class MessagingTemplate : GenericMessagingTemplate
    {
        public MessagingTemplate(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public MessagingTemplate(IServiceProvider serviceProvider, IMessageChannel defaultChannel)
            : base(serviceProvider)
        {
            DefaultDestination = defaultChannel;
        }

        public override IMessage SendAndReceive(IMessageChannel destination, IMessage requestMessage)
        {
            return base.SendAndReceive(destination, requestMessage);
        }

        public object ReceiveAndConvert(IMessageChannel destination, int timeout)
        {
            var message = DoReceive(destination, timeout);
            if (message != null)
            {
                return DoConvert<object>(message);
            }
            else
            {
                return Task.FromResult<object>(null);
            }
        }

        public IMessage Receive(IMessageChannel destination, int timeout)
        {
            return DoReceive(destination, timeout);
        }
    }
}
