// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Loggers;

public sealed class LoggersResponse
{
    private static readonly ReadOnlyCollection<string> EmptyLevels = Array.Empty<string>().AsReadOnly();
    private static readonly ReadOnlyDictionary<string, LoggerLevels> EmptyLoggers = new(new Dictionary<string, LoggerLevels>());
    internal static readonly ReadOnlyDictionary<string, LoggerGroup> EmptyGroups = new(new Dictionary<string, LoggerGroup>());

    public static LoggersResponse Error { get; } = new(EmptyLevels, EmptyLoggers, EmptyGroups, true);

    [JsonIgnore]
    public bool HasError { get; }

    [JsonPropertyName("levels")]
    public IList<string> Levels { get; }

    [JsonPropertyName("loggers")]
    public IDictionary<string, LoggerLevels> Loggers { get; }

    [JsonPropertyName("groups")]
    public IDictionary<string, LoggerGroup> Groups { get; }

    public LoggersResponse(IList<string> levels, IDictionary<string, LoggerLevels> loggers, IDictionary<string, LoggerGroup> groups)
        : this(levels, loggers, groups, false)
    {
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(loggers);
        ArgumentNullException.ThrowIfNull(groups);
    }

    private LoggersResponse(IList<string> levels, IDictionary<string, LoggerLevels> loggers, IDictionary<string, LoggerGroup> groups, bool hasError)
    {
        Levels = levels;
        Loggers = loggers;
        Groups = groups;
        HasError = hasError;
    }
}
