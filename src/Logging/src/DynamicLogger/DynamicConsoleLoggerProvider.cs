// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Logging.DynamicLogger;

[ProviderAlias("Dynamic")]
public class DynamicConsoleLoggerProvider : DynamicLoggerProviderBase
{
    protected readonly IOptionsMonitor<LoggerFilterOptions> FilterOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicConsoleLoggerProvider" /> class.
    /// </summary>
    /// <param name="filterOptions">
    /// Logger filters.
    /// </param>
    /// <param name="consoleLoggerProvider">
    /// The system console logger provider, used for delegation.
    /// </param>
    /// <param name="messageProcessors">
    /// message processors to apply to message.
    /// </param>
    public DynamicConsoleLoggerProvider(IOptionsMonitor<LoggerFilterOptions> filterOptions, ConsoleLoggerProvider consoleLoggerProvider,
        IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        : base(() => consoleLoggerProvider, GetInitialLevelsFromOptions(filterOptions), messageProcessors)
    {
    }

    private static InitialLevels GetInitialLevelsFromOptions(IOptionsMonitor<LoggerFilterOptions> filterOptions)
    {
        var runningLevelFilters = new Dictionary<string, Filter>();
        var originalLevels = new Dictionary<string, LogLevel>();
        Filter filter = null;

        foreach (LoggerFilterRule rule in filterOptions.CurrentValue.Rules.Where(p =>
            string.IsNullOrEmpty(p.ProviderName) || p.ProviderName == "Console" || p.ProviderName == "Logging"))
        {
            originalLevels[rule.CategoryName ?? "Default"] = rule.LogLevel ?? LogLevel.None;

            if (rule.CategoryName == "Default" || string.IsNullOrEmpty(rule.CategoryName))
            {
                filter = (_, level) => level >= (rule.LogLevel ?? LogLevel.None);
            }
            else
            {
                runningLevelFilters[rule.CategoryName] = (_, level) => level >= (rule.LogLevel ?? LogLevel.None);
            }
        }

        return new InitialLevels
        {
            DefaultLevelFilter = filter,
            OriginalLevels = originalLevels,
            RunningLevelFilters = runningLevelFilters
        };
    }
}
