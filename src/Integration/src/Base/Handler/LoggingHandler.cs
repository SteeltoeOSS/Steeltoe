// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public class LoggingHandler : AbstractMessageHandler
    {
        public LogLevel Level { get; }

        public override string ComponentType => "logging-channel-adapter";

        public ILogger MessageLogger { get; }

        public LoggingHandler(IServiceProvider serviceProvider, LogLevel level, ILogger<LoggingHandler> logger)
            : base(serviceProvider)
        {
            Level = level;
            MessageLogger = logger;
        }

        protected override void HandleMessageInternal(IMessage message)
        {
            switch (Level)
            {
                case LogLevel.Critical:
                    if (MessageLogger.IsEnabled(LogLevel.Critical))
                    {
                        MessageLogger.LogCritical(CreateLogMessage(message));
                    }

                    break;
                case LogLevel.Error:
                    if (MessageLogger.IsEnabled(LogLevel.Error))
                    {
                        MessageLogger.LogError(CreateLogMessage(message));
                    }

                    break;
                case LogLevel.Warning:
                    if (MessageLogger.IsEnabled(LogLevel.Warning))
                    {
                        MessageLogger.LogWarning(CreateLogMessage(message));
                    }

                    break;
                case LogLevel.Information:
                    if (MessageLogger.IsEnabled(LogLevel.Information))
                    {
                        MessageLogger.LogInformation(CreateLogMessage(message));
                    }

                    break;
                case LogLevel.Debug:
                    if (MessageLogger.IsEnabled(LogLevel.Debug))
                    {
                        MessageLogger.LogDebug(CreateLogMessage(message));
                    }

                    break;
                case LogLevel.Trace:
                    if (MessageLogger.IsEnabled(LogLevel.Trace))
                    {
                        MessageLogger.LogTrace(CreateLogMessage(message));
                    }

                    break;
                default:
                    break;
            }

            return;
        }

        protected virtual string CreateLogMessage(IMessage message)
        {
            return message.Payload.ToString();
        }
    }
}
