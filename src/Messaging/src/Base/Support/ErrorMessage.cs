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

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support
{
    public class ErrorMessage : Message<Exception>
    {
        public ErrorMessage(Exception payload)
        : base(payload)
        {
            OriginalMessage = null;
        }

        public ErrorMessage(Exception payload, IDictionary<string, object> headers)
        : base(payload, headers)
        {
            OriginalMessage = null;
        }

        public ErrorMessage(Exception payload, IMessageHeaders headers)
         : base(payload, headers)
        {
            OriginalMessage = null;
        }

        public ErrorMessage(Exception payload, IMessage originalMessage)
        : base(payload)
        {
            OriginalMessage = originalMessage;
        }

        public ErrorMessage(Exception payload, IDictionary<string, object> headers, IMessage originalMessage)
        : base(payload, headers)
        {
            OriginalMessage = originalMessage;
        }

        public ErrorMessage(Exception payload, IMessageHeaders headers, IMessage originalMessage)
        : base(payload, headers)
        {
            OriginalMessage = originalMessage;
        }

        public IMessage OriginalMessage { get; }

        public override string ToString()
        {
            if (OriginalMessage == null)
            {
                return base.ToString();
            }

            return base.ToString() + " for original " + OriginalMessage;
        }
    }
}
