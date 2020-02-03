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

using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public abstract class AbstractReplyProducingMessageHandler : AbstractMessageProducingHandler
    {
        protected AbstractReplyProducingMessageHandler(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public bool RequiresReply { get; set; }

        protected override void HandleMessageInternal(IMessage message)
        {
            var result = HandleRequestMessage(message);
            if (result != null)
            {
                SendOutputs(result, message);
            }
            else if (RequiresReply)
            {
                throw new ReplyRequiredException(
                    message,
                    "No reply produced by handler '" + GetType().Name + "', and its 'requiresReply' property is set to true.");
            }
        }

        protected abstract object HandleRequestMessage(IMessage requestMessage);
    }
}
