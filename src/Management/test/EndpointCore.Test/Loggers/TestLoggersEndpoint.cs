// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Loggers.Test;

internal class TestLoggersEndpoint : LoggersEndpoint
{
    public TestLoggersEndpoint(ILoggersOptions options, IDynamicLoggerProvider loggerProvider = null, ILogger<LoggersEndpoint> logger = null)
        : base(options, loggerProvider, logger)
    {
    }

    public override Dictionary<string, object> Invoke(LoggersChangeRequest request)
    {
        return new Dictionary<string, object>();
    }
}