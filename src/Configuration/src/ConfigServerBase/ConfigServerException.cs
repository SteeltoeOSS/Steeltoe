// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Exception thrown by Config Server client when problems occur
/// </summary>
[Serializable]
public class ConfigServerException : Exception
{
    public ConfigServerException(string message, Exception error)
        : base(message, error)
    {
    }

    public ConfigServerException(string message)
        : base(message)
    {
    }

    public ConfigServerException()
    {
    }

    protected ConfigServerException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}