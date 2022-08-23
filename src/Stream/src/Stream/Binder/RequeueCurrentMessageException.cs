// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

public class RequeueCurrentMessageException : Exception
{
    public RequeueCurrentMessageException()
    {
    }

    public RequeueCurrentMessageException(string message)
        : base(message)
    {
    }

    public RequeueCurrentMessageException(Exception innerException)
        : base(null, innerException)
    {
    }

    public RequeueCurrentMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
