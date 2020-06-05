// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class Link
    {
#pragma warning disable SA1300 // Accessible fields must begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public string href { get; private set; }

        public bool templated { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Accessible fields must begin with upper-case letter

        public Link(string href)
        {
            this.href = href;
            templated = href.Contains("{");
        }
    }
}
