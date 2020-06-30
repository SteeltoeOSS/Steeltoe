// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration
{
    public class MessagingTemplate : MessageChannelTemplate
    {
        public MessagingTemplate(IApplicationContext context, ILogger logger = null)
            : base(context, logger)
        {
        }

        public MessagingTemplate(IApplicationContext context, IMessageChannel defaultChannel, ILogger logger = null)
            : base(context, logger)
        {
            DefaultSendDestination = DefaultReceiveDestination = defaultChannel;
        }

        public IMessageChannel DefaultDestination
        {
            get
            {
                // Default Receive and Send are kept the same
                return DefaultReceiveDestination;
            }

            set
            {
                DefaultSendDestination = DefaultReceiveDestination = value;
            }
        }

        public override IMessageChannel DefaultReceiveDestination
        {
            get => base.DefaultReceiveDestination;
            set => base.DefaultReceiveDestination = base.DefaultSendDestination = value;
        }

        public override IMessageChannel DefaultSendDestination
        {
            get => base.DefaultSendDestination;
            set => base.DefaultSendDestination = DefaultReceiveDestination = value;
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
