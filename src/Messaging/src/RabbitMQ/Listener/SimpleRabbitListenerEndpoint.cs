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
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Listener
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
                    .Append(" | messageListener='").Append(MessageListener).Append("'");
        }
    }
}
