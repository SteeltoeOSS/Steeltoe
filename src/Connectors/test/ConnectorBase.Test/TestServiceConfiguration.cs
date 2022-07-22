// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Connector.Test;

internal class TestServiceConfiguration : AbstractServiceConnectorOptions
{
    public TestServiceConfiguration(IConfiguration config)
        : base(config)
    {
    }

    public string Test { get; set; }
}