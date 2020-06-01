// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Test
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class TestOptions2 : AbstractOptions
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public TestOptions2()
            : base()
        {
        }

        public TestOptions2(string section, IConfiguration config)
            : base(section, config)
        {
        }
    }
}
