// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog.Events;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public class MinimumLevel
{
    public LogEventLevel Default { get; set; } = (LogEventLevel)(-1);

    public Dictionary<string, LogEventLevel> Override { get; set; }
}
