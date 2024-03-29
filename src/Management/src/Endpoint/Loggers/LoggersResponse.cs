// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Loggers;

public sealed class LoggersResponse
{
    public IDictionary<string, object> Data { get; }
    public bool HasError { get; }

    public LoggersResponse(IDictionary<string, object> data, bool hasError)
    {
        ArgumentGuard.NotNull(data);

        HasError = hasError;
        Data = data;
    }
}
