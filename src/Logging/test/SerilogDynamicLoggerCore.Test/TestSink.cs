// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger.Test
{
    public class TestSink : ILogEventSink
    {
        private List<string> logs = new List<string>();

        public void Emit(LogEvent logEvent)
        {
            logs.Add(logEvent.RenderMessage());
        }

        public List<string> GetLogs()
        {
            return logs;
        }
    }
}
