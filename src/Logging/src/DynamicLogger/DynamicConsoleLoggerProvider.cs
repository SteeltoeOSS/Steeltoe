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
    private readonly IDisposable? _optionsChangeListener;

    public DynamicConsoleLoggerProvider(IOptionsMonitor<LoggerFilterOptions> filterOptionsMonitor, ConsoleLoggerProvider consoleLoggerProvider,
        IEnumerable<IDynamicMessageProcessor> messageProcessors)
        : base(consoleLoggerProvider, GetMinimumLevelsFromOptionsMonitor(filterOptionsMonitor), messageProcessors)
    {
        ArgumentNullException.ThrowIfNull(filterOptionsMonitor);

        _optionsChangeListener = filterOptionsMonitor.OnChange((options, _) =>
        {
            LogLevelsConfiguration configuration = GetMinimumLevelsFromOptions(options);
            RefreshConfiguration(configuration);
        });
    }

    private static LogLevelsConfiguration GetMinimumLevelsFromOptionsMonitor(IOptionsMonitor<LoggerFilterOptions> filterOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(filterOptionsMonitor);

        return GetMinimumLevelsFromOptions(filterOptionsMonitor.CurrentValue);
    }

    private static LogLevelsConfiguration GetMinimumLevelsFromOptions(LoggerFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var minLevelsPerCategory = new Dictionary<string, LogLevel>();

        foreach (LoggerFilterRule rule in GetRulesForConsoleProvider(options))
        {
            string categoryName = rule.CategoryName ?? string.Empty;

            if (categoryName.Contains('*'))
            {
                throw new NotSupportedException("Logger categories with wildcards are not supported.");
            }

            if (rule.LogLevel != null)
            {
                minLevelsPerCategory[categoryName] = rule.LogLevel.Value;
            }
        }

        return new LogLevelsConfiguration(minLevelsPerCategory.AsReadOnly());
    }

    private static IEnumerable<LoggerFilterRule> GetRulesForConsoleProvider(LoggerFilterOptions options)
    {
        // Return global rules first, so that they are overridden by console-specific rules.
        foreach (LoggerFilterRule rule in options.Rules.Where(rule => IsProviderAgnostic(rule.ProviderName)))
        {
            yield return rule;
        }

        foreach (LoggerFilterRule rule in options.Rules.Where(rule => IsConsoleProvider(rule.ProviderName)))
        {
            yield return rule;
        }
    }

    private static bool IsProviderAgnostic(string? providerName)
    {
        return string.IsNullOrEmpty(providerName) || string.Equals(providerName, "Logging", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsoleProvider(string? providerName)
    {
        return string.Equals(providerName, "Console", StringComparison.OrdinalIgnoreCase) || string.Equals(providerName,
            typeof(ConsoleLoggerProvider).FullName, StringComparison.OrdinalIgnoreCase);
    }

    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(scopeProvider);

        ((ConsoleLoggerProvider)InnerLoggerProvider).SetScopeProvider(scopeProvider);
    }

    protected override void Dispose(bool disposing)
    {
        _optionsChangeListener?.Dispose();
        base.Dispose(disposing);
    }
}
