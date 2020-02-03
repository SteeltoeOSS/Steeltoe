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

namespace Steeltoe.Integration.Support
{
    public static class IntegrationUtils
    {
        public static Exception WrapInDeliveryExceptionIfNecessary(IMessage message, string text, Exception e)
        {
            if (!(e is MessagingException me))
            {
                return new MessageDeliveryException(message, text, e);
            }

            if (me.FailedMessage == null)
            {
                return new MessageDeliveryException(message, text, e);
            }

            return me;
        }

        public static Exception WrapInHandlingExceptionIfNecessary(IMessage message, string text, Exception e)
        {
            if (!(e is MessagingException me))
            {
                return new MessageHandlingException(message, text, e);
            }

            if (me.FailedMessage == null)
            {
                return new MessageHandlingException(message, text, e);
            }

            return me;
        }
    }
}
