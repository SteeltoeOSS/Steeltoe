// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging;

public class DynamicLoggerConfiguration : ILoggerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerConfiguration"/> class.
    /// </summary>
    /// <param name="name">Namespace</param>
    /// <param name="configured">Original log level</param>
    /// <param name="effective">Currently effective log level</param>
    public DynamicLoggerConfiguration(string name, LogLevel? configured, LogLevel effective)
    {
        Name = name;
        ConfiguredLevel = configured;
        EffectiveLevel = effective;
    }

    /// <summary>
    /// Gets namespace this configuration is applied to
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets level from base app configuration (if present)
    /// </summary>
    public LogLevel? ConfiguredLevel { get; }

    /// <summary>
    /// Gets running level of the logger
    /// </summary>
    public LogLevel EffectiveLevel { get; }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is DynamicLoggerConfiguration lc &&
               Name == lc.Name &&
               ConfiguredLevel == lc.ConfiguredLevel &&
               EffectiveLevel == lc.EffectiveLevel;
    }

    public override string ToString()
    {
        return $"[{Name},{ConfiguredLevel},{EffectiveLevel}]";
    }
}
