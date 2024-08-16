// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Steeltoe.Logging.DynamicLogger;

/// <summary>
/// Implements <see cref="DynamicLoggerProvider" /> for logging to the console.
/// </summary>
[ProviderAlias("Dynamic")]
public sealed class DynamicConsoleLoggerProvider : DynamicLoggerProvider, ISupportExternalScope
{
    public DynamicConsoleLoggerProvider(IOptionsMonitor<LoggerFilterOptions> filterOptionsMonitor, ConsoleLoggerProvider consoleLoggerProvider,
        IEnumerable<IDynamicMessageProcessor> messageProcessors)
        : base(consoleLoggerProvider, GetMinimumLevelsFromOptions(filterOptionsMonitor), messageProcessors)
    {
    }

    private static LoggerFilterConfiguration GetMinimumLevelsFromOptions(IOptionsMonitor<LoggerFilterOptions> filterOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(filterOptionsMonitor);

        var effectiveFilters = new Dictionary<string, LoggerFilter>();
        var configurationMinLevels = new Dictionary<string, LogLevel>();
        LoggerFilter defaultFilter = level => level >= DefaultLogLevel;

        foreach (LoggerFilterRule rule in filterOptionsMonitor.CurrentValue.Rules.Where(IsConsoleProvider))
        {
            string ruleCategoryName = rule.CategoryName ?? DefaultCategoryName;

            if (ruleCategoryName.Contains('*'))
            {
                throw new NotSupportedException("Logger categories with wildcards are not supported.");
            }

            configurationMinLevels[ruleCategoryName] = rule.LogLevel ?? DefaultLogLevel;

            if (ruleCategoryName == DefaultCategoryName)
            {
                defaultFilter = level => level >= (rule.LogLevel ?? DefaultLogLevel);
            }
            else
            {
                effectiveFilters[ruleCategoryName] = level => level >= (rule.LogLevel ?? DefaultLogLevel);
            }
        }

        return new LoggerFilterConfiguration(configurationMinLevels, effectiveFilters, defaultFilter);
    }

    private static bool IsConsoleProvider(LoggerFilterRule rule)
    {
        return string.IsNullOrEmpty(rule.ProviderName) || rule.ProviderName == "Console" || rule.ProviderName == "Logging";
    }

    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(scopeProvider);

        ((ConsoleLoggerProvider)InnerLoggerProvider).SetScopeProvider(scopeProvider);
    }
}
