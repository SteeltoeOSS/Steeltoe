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

namespace Steeltoe.Stream.Binder
{
    internal class FinalRethrowingErrorMessageHandler : ILastSubscriberMessageHandler
    {
        private readonly ILastSubscriberAwareChannel _errorChannel;

        private readonly bool defaultErrorChannelPresent;

        public FinalRethrowingErrorMessageHandler(ILastSubscriberAwareChannel errorChannel, bool defaultErrorChannelPresent)
        {
            _errorChannel = errorChannel;
            this.defaultErrorChannelPresent = defaultErrorChannelPresent;
        }

        public void HandleMessage(IMessage message)
        {
            if (_errorChannel.Subscribers > (defaultErrorChannelPresent ? 2 : 1))
            {
                // user has subscribed; default is 2, this and the bridge to the
                // errorChannel
                return;
            }

            if (message.Payload is MessagingException)
            {
                throw (MessagingException)message.Payload;
            }
            else
            {
                throw new MessagingException((IMessage)null, (Exception)message.Payload);
            }
        }
    }
}
