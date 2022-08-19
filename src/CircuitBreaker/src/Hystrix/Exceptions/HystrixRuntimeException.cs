// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions;

public class HystrixRuntimeException : Exception
{
    public FailureType FailureType { get; }

    public Exception FallbackException { get; }

    public Type ImplementingType { get; }

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

    public HystrixRuntimeException(FailureType failureType, Type commandType, string message)
        : base(message)
    {
        FailureType = failureType;
        ImplementingType = commandType;
        FallbackException = null;
    }

    public HystrixRuntimeException(FailureType failureType, Type commandType, string message, Exception innerException, Exception fallbackException)
        : base(message, innerException)
    {
        FailureType = failureType;
        ImplementingType = commandType;
        FallbackException = fallbackException;
    }
}
