// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
