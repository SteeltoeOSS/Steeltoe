// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.Transport;

/// <summary>
/// The exception that is thrown when a communication failure with a Eureka server occurs.
/// </summary>
public sealed class EurekaTransportException : Exception
{
    public EurekaTransportException(string? message)
        : base(message)
    {
    }

    public EurekaTransportException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
