// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions;

public class HystrixBadRequestException : Exception
{
    public HystrixBadRequestException()
    {
    }

    public HystrixBadRequestException(string message)
        : base(message)
    {
    }

    public HystrixBadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
