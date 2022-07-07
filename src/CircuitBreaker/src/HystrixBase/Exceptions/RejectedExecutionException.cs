// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions;

[Serializable]
public class RejectedExecutionException : Exception
{
    public RejectedExecutionException(string message)
        : base(message)
    {
    }

    public RejectedExecutionException()
    {
    }

    public RejectedExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected RejectedExecutionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
