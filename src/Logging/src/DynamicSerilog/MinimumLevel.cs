// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog.Events;

namespace Steeltoe.Logging.DynamicSerilog;

public sealed class MinimumLevel
{
    public LogEventLevel Default { get; set; } = (LogEventLevel)(-1);
    public IDictionary<string, LogEventLevel> Override { get; } = new Dictionary<string, LogEventLevel>();
}
