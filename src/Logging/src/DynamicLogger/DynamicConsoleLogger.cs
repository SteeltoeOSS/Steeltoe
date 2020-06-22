﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging
{
    public class DynamicConsoleLogger : ILogger
    {
        private IEnumerable<IDynamicMessageProcessor> _messageProcessors;

        internal DynamicConsoleLogger(ILogger iLogger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        {
            _messageProcessors = messageProcessors;
            Delegate = iLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => Delegate.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => Filter.Invoke(Name, logLevel);

        public ILogger Delegate { get; private set; }

        public Func<string, LogLevel, bool> Filter { get; internal set; }

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
            => Delegate.Log(logLevel, eventId, exception, message);
    }
}
