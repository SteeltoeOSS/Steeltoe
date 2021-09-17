// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using System;
using System.Collections.Generic;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    public class SerilogDynamicProvider : DynamicLoggerProviderBase
    {
        private static readonly object _sync = new ();
        private static Serilog.Core.Logger _serilogger;

        /// <summary>
        ///  Initializes a new instance of the <see cref="SerilogDynamicProvider"/> class.
        /// </summary>
        /// <param name="serilogOptionsMonitor">Serilog Options Monitor <see cref="SerilogOptions"/></param>
        /// <param name="messageProcessors"> Any message processors <see cref="IDynamicMessageProcessor"/></param>
        public SerilogDynamicProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
           : base(() => GetDelegateLogger(serilogOptionsMonitor), GetInitialLevelsFromOptions(serilogOptionsMonitor), messageProcessors)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDynamicProvider"/> class.
        /// Any Serilog settings can be passed in the IConfiguration as needed.
        /// </summary>
        /// <param name="configuration">Serilog readable <see cref="IConfiguration"/></param>
        /// <param name="logger">Serilog logger<see cref="Serilog.Core.Logger"/></param>
        /// <param name="loggingLevelSwitch">Serilog global log level switch<see cref="Serilog.Core.LoggingLevelSwitch"/></param>
        /// <param name="options">Subset of Serilog options managed by wrapper<see cref="ISerilogOptions"/></param>
        [Obsolete("Will be removed in a future release; Use SerilogDynamicProvider(IOptionsMonitor<SerilogOptions>, IEnumerable<IDynamicMessageProcessor>) instead ")]
        public SerilogDynamicProvider(IConfiguration configuration, ISerilogOptions options, Logger logger = null, LoggingLevelSwitch loggingLevelSwitch = null)
            : base(() => GetDelegateLogger(configuration), GetInitialLevelsFromOptions(configuration), null)
        {
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(configuration);
        }

        [Obsolete("Will be removed in a future release; Use SerilogDynamicProvider(IConfiguration, ISerilogOptions, Logger, LoggingLevelSwitch) instead")]
        public SerilogDynamicProvider(IConfiguration configuration, Logger logger, LoggingLevelSwitch loggingLevelSwitch, ISerilogOptions options = null)
           : this(configuration, options, logger, loggingLevelSwitch)
        {
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

            return DoGetDelegateLogger(serilogOptions);
        }

        private static ILoggerProvider GetDelegateLogger(IConfiguration configuration)
        {
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(configuration);

            return DoGetDelegateLogger(serilogOptions);
        }

        private static ILoggerProvider DoGetDelegateLogger(SerilogOptions serilogOptions)
        {
            lock (_sync)
            {
                _serilogger ??= serilogOptions.GetSerilogConfiguration().CreateLogger(); // Cannot create more than once, so protect with a lock and static property
            }

            return new Serilog.Extensions.Logging.SerilogLoggerProvider(_serilogger);
        }

        private static InitialLevels GetInitialLevelsFromOptions(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
        {
            var serilogOptions = serilogOptionsMonitor.CurrentValue;
            return DoGetInitialLevelsFromOptions(serilogOptions);
        }

        private static InitialLevels GetInitialLevelsFromOptions(IConfiguration configuration)
        {
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(configuration);

            return DoGetInitialLevelsFromOptions(serilogOptions);
        }

        private static InitialLevels DoGetInitialLevelsFromOptions(SerilogOptions serilogOptions)
        {
            var defaultLevel = (LogLevel)serilogOptions.MinimumLevel.Default;
            var originalLevels = new Dictionary<string, LogLevel>();
            var runningLevelFilters = new Dictionary<string, Filter>();
            originalLevels["Default"] = defaultLevel;

            foreach (var overrideLevel in serilogOptions.MinimumLevel.Override)
            {
                var logLevel = (LogLevel)overrideLevel.Value;
                originalLevels[overrideLevel.Key] = logLevel;
                runningLevelFilters[overrideLevel.Key] = (category, level) => level >= logLevel;
            }

            return new InitialLevels
            {
                DefaultLevelFilter = (category, level) => level >= defaultLevel,
                RunningLevelFilters = runningLevelFilters,
                OriginalLevels = originalLevels
            };
        }
    }
}
