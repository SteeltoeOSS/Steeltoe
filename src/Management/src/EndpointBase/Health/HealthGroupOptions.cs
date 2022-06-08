// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Health;

public class HealthGroupOptions
{
    /// <summary>
    /// Gets or sets a comma-separated list of contributors or tags to include in this group
    /// </summary>
    public string Include { get; set; }

    // TODO: ? found in spring-boot but not here
    //// string show-details
    //// string roles
    //// object status { order<csv>, http-mapping<Dictionary<string, int>> }
    //// StatusAggregator and HttpCodeStatusMapper
}
