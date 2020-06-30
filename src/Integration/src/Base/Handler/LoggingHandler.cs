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
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public class LoggingHandler : AbstractMessageHandler
    {
        public LogLevel Level { get; }

        public override string ComponentType => "logging-channel-adapter";

        public ILogger MessageLogger { get; }

        public LoggingHandler(IApplicationContext context, LogLevel level, ILogger logger)
            : base(context)
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
