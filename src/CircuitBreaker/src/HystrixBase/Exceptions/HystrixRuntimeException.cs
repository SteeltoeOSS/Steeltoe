// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions;

[Serializable]
public class HystrixRuntimeException : Exception
{
    public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message)
        : base(message)
    {
        FailureType = failureCause;
        ImplementingClass = commandClass;
        FallbackException = null;
    }

    public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message, Exception cause, Exception fallbackException)
        : base(message, cause)
    {
        FailureType = failureCause;
        ImplementingClass = commandClass;
        FallbackException = fallbackException;
    }

    public HystrixRuntimeException()
    {
    }

    public HystrixRuntimeException(string message)
        : base(message)
    {
    }

    public HystrixRuntimeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected HystrixRuntimeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public FailureType FailureType { get; }

    public Exception FallbackException { get; }

    public Type ImplementingClass { get; }
}
