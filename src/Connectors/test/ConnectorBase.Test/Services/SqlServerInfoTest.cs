// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class SqlServerInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            var uri = "jdbc:sqlserver://192.168.0.90:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e";
            var r1 = new SqlServerServiceInfo("myId", uri, "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");

            Assert.Equal("myId", r1.Id);
            Assert.Equal("sqlserver", r1.Scheme);
            Assert.Equal("192.168.0.90", r1.Host);
            Assert.Equal(1433, r1.Port);
            Assert.Equal("7E1LxXnlH2hhlPVt", r1.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", r1.UserName);
            Assert.Equal("de5aa3a747c134b3d8780f8cc80be519e", r1.Path);
        }
    }
}
