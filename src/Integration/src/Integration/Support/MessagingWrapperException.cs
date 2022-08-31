// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public class MessagingWrapperException : MessagingException
{
    public MessagingWrapperException(IMessage failedMessage, MessagingException innerException)
        : base(failedMessage, innerException)
    {
    }
}