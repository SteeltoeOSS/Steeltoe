// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging
{
    public interface ILoggerConfiguration
    {
        string Name { get; }

        LogLevel? ConfiguredLevel { get; }

        LogLevel EffectiveLevel { get; }
    }
}