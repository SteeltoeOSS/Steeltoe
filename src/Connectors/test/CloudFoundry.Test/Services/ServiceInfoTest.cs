// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Xunit;

namespace Steeltoe.Connectors.CloudFoundry.Test.Services;

public sealed class ServiceInfoTest
{
    [Fact]
    public void Constructor_ThrowsIfIdNull()
    {
        const string id = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfo(id));
        Assert.Contains(nameof(id), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_InitializesValues()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var info = new ApplicationInstanceInfo(configuration);
        var si = new TestServiceInfo("id", info);

        Assert.Equal("id", si.Id);
        Assert.Equal(info, si.ApplicationInfo);
    }
}
