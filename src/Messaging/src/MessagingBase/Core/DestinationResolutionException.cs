// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Core;

public class DestinationResolutionException : MessagingException
{
    public DestinationResolutionException(string description)
        : base(description)
    {
    }

    public DestinationResolutionException(string description, Exception cause)
        : base(description, cause)
    {
    }
}
