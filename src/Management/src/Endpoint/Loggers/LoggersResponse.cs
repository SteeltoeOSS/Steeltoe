// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Loggers;
public class LoggersResponse
{
    public Dictionary<string, object> Data { get; }
    public bool HasError { get; }

    public LoggersResponse(Dictionary<string, object> data, bool hasError)
    {
        HasError = hasError;
        Data = data;
    }
}
