// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpointOptions : EndpointOptionsBase//, ILoggersOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:loggers";

    // public EndpointOptionsBase EndpointOptions { get; set; }
    public override IEnumerable<string> AllowedVerbs  { get; } = new List<string>
        {
            "Get",
            "Post"
        };
    public override bool ExactMatch { get; } // defaults false;
 
}
