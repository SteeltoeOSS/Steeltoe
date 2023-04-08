// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Logging;
using Steeltoe.Management.Endpoint.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

internal sealed class TestLoggersEndpoint : LoggersEndpoint
{
    public TestLoggersEndpoint(IOptionsMonitor<LoggersEndpointOptions> options, ILogger<LoggersEndpoint> logger, IDynamicLoggerProvider loggerProvider = null)
        : base(options, logger, loggerProvider)
    {
    }

    public override Dictionary<string, object> Invoke(LoggersChangeRequest request)
    {
        return new Dictionary<string, object>();
    }
}
