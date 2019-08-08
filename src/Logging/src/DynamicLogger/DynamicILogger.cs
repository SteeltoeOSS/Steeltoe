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
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging
{
    public class DynamicILogger : ILogger
    {
        private IEnumerable<IDynamicMessageProcessor> _messageProcessors;

        public DynamicILogger(ILogger iLogger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        {
            _messageProcessors = messageProcessors;
            Delegate = iLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => Delegate.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => Filter.Invoke(Name, logLevel);

        public ILogger Delegate { get; set; }

        public Func<string, LogLevel, bool> Filter { get; set; }

        public string Name { get; internal set; }

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
                WriteMessage(logLevel, Name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
            => Delegate.Log(logLevel, Name, eventId, message, exception);
    }
}
