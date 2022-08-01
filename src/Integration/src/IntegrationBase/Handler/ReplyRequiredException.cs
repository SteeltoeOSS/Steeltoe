// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

public class ReplyRequiredException : MessagingException
{
    public ReplyRequiredException(IMessage failedMessage, string description)
        : base(failedMessage, description)
    {
    }

    public ReplyRequiredException(IMessage failedMessage, string description, Exception e)
        : base(failedMessage, description, e)
    {
    }
}
