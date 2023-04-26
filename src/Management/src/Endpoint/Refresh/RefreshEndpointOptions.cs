// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Refresh;

public sealed class RefreshEndpointOptions : EndpointOptionsBase
{
    private const bool DefaultReturnConfiguration = true;
    public override IEnumerable<string> AllowedVerbs { get; } = new List<string>
    {
        "Post"
    };
    public bool ReturnConfiguration { get; set; } = DefaultReturnConfiguration;
}
