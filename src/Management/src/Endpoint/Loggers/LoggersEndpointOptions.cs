// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Loggers;

public sealed class LoggersEndpointOptions : HttpMiddlewareOptions
{
    public override IList<string> AllowedVerbs { get; set; } = new List<string>
    {
        "Get",
        "Post"
    };

    public override bool RequiresExactMatch()
    {
        return false;
    }
}
