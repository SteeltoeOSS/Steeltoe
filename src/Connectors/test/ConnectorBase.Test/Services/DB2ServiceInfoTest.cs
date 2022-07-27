// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class DB2ServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        var uri = "db2://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355";
        var r1 = new DB2ServiceInfo("myId", uri);

        Assert.Equal("myId", r1.Id);
        Assert.Equal("db2", r1.Scheme);
        Assert.Equal("192.168.0.90", r1.Host);
        Assert.Equal(3306, r1.Port);
        Assert.Equal("7E1LxXnlH2hhlPVt", r1.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", r1.UserName);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", r1.Path);
        Assert.Null(r1.Query);
    }
}