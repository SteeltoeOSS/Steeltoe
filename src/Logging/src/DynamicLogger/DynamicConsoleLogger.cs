// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Console.Internal;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging
{
    public class DynamicConsoleLogger : ILogger
    {
        private readonly IEnumerable<IDynamicMessageProcessor> _messageProcessors;
        private readonly ConsoleLogger _delegate;

        internal DynamicConsoleLogger(ConsoleLogger consoleLogger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        {
            _messageProcessors = messageProcessors;
            _delegate = consoleLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => _delegate.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _delegate.IsEnabled(logLevel);

        public IConsole Console
        {
            get => _delegate.Console;

            set => _delegate.Console = value;
        }

        public Func<string, LogLevel, bool> Filter
        {
            get => _delegate.Filter;

            set => _delegate.Filter = value;
        }

        [Obsolete("Changing this property has no effect. Use " + nameof(ConsoleLoggerOptions) + "." + nameof(ConsoleLoggerOptions.IncludeScopes) + " instead")]
        public bool IncludeScopes { get; set; }

        public string Name => _delegate.Name;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (_messageProcessors != null)
            {
                foreach (var processor in _messageProcessors)
                {
                    message = processor.Process(message);
                }
            }

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, _delegate.Name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
            => _delegate.WriteMessage(logLevel, _delegate.Name, eventId, message, exception);
    }
}
