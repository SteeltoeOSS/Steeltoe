// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Test.Options;

internal class TestOptions : AbstractOptions
{
    public TestOptions(IConfigurationRoot root, string prefix)
        : base(root, prefix)
    {
    }

    public TestOptions(IConfiguration config)
        : base(config)
    {
    }

    public string Foo { get; set; }
}