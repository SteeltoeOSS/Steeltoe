// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    public class SerilogDynamicProvider : DynamicConsoleLoggerProvider
    {
        private static readonly object _sync = new ();
        private static Serilog.Core.Logger _serilogger;
        private readonly SerilogOptions _serilogOptions;

        public SerilogDynamicProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
            : base(() => GetDelegateLogger(serilogOptionsMonitor), messageProcessors)
        {
            _serilogOptions = serilogOptionsMonitor?.CurrentValue ?? throw new ArgumentNullException(nameof(serilogOptionsMonitor));

            SetFiltersFromOptions();
        }

        /// <summary>
        /// Because of how Serilog is implemented, there is a single instance of logger for a given application, which gets wrapped by ILoggers.
        /// However while testing, we need to clear this instance between tests.
        /// Should not be required under normal usage.
        /// </summary>
        internal static void ClearLogger()
        {
            _serilogger = null;
        }

        private static ILoggerProvider GetDelegateLogger(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
        {
            var serilogOptions = serilogOptionsMonitor?.CurrentValue ?? throw new ArgumentNullException(nameof(serilogOptionsMonitor));

            lock (_sync)
            {
                _serilogger ??= serilogOptions.GetSerilogConfiguration().CreateLogger(); // Cannot create more than once, so protect with a lock and static property
            }

            return new Serilog.Extensions.Logging.SerilogLoggerProvider(_serilogger);
        }

        private new void SetFiltersFromOptions()
        {
            var defaultLevel = (LogLevel)_serilogOptions.MinimumLevel.Default;
            _originalLevels.TryAdd("Default", defaultLevel);
            _filter = (category, level) => level >= defaultLevel;

            foreach (var overrideLevel in _serilogOptions.MinimumLevel.Override)
            {
                var logLevel = (LogLevel)overrideLevel.Value;
                _originalLevels.TryAdd(overrideLevel.Key, logLevel);
                _runningFilters.TryAdd(overrideLevel.Key, (category, level) => level >= logLevel);
            }
        }
    }
}
