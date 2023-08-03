// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

public sealed class Link
{
    public string Href { get; set; }

    public bool Templated { get; }

    public Link(string href)
    {
        ArgumentGuard.NotNull(href);

        Href = href;
        Templated = href.Contains('{');
    }
}
