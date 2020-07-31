// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    // TODO: Move this to the Hypermedia namespace in 3.0
    [Obsolete("Warning: this will move to the Hypermedia namespace in 3.0")]
    public class Link
    {
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
        public string href;
        public bool templated;
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter

        public Link()
        {
        }

        public Link(string href)
        {
            this.href = href;
            templated = href.Contains("{");
        }
    }
}
