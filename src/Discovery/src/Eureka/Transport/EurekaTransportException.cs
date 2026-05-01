// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

[Serializable]
public class EurekaTransportException : Exception
{
    public EurekaTransportException(string message)
        : base(message)
    {
    }

    public EurekaTransportException(string message, Exception cause)
        : base(message, cause)
    {
    }

    public EurekaTransportException()
    {
    }

    protected EurekaTransportException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        : base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
    {
    }
}