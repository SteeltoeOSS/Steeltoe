// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Exception thrown by Config Server client when problems occur.
/// </summary>
public class ConfigServerException : Exception
{
    public ConfigServerException()
    {
    }

    public ConfigServerException(string message)
        : base(message)
    {
    }

    public ConfigServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
