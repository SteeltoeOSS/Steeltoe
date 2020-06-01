// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class SqlServerInfoFactoryTest
    {
        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "sqlserver",
                Tags = new string[] { "sqlserver", "relational" },
                Name = "sqlserverService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:sqlserver://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsValidCUPServiceBinding()
        {
            Service s = new Service()
            {
                Label = "user-provided",
                Tags = new string[] { },
                Name = "sqlserverService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "uid", new Credential("u79024cecd1c8460ab7befc45c1de57ae") },
                    { "pw", new Credential("P39d904d42d4647878e8a29db9c4b1ce0") },
                    { "uri", new Credential("jdbc:sqlserver://10.194.59.187:1433;databaseName=d07833038adb541bba1bb6dc77df7a724") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsNoLabelNoTagsServiceBinding()
        {
            Service s = new Service()
            {
                Name = "sqlserverService",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:sqlserver://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsLabelNoTagsServiceBinding()
        {
            Service s = new Service()
            {
                Label = "sqlserver",
                Name = "sqlserverService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:sqlserver://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "p-foobar",
                Tags = new string[] { "foobar", "relational" },
                Name = "mySqlService",
                Plan = "100mb-dev",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("foobar://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:foobar://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "sqlserver",
                Tags = new string[] { "sqlserver", "relational" },
                Name = "sqlserverService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:sqlserver://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            var info = factory.Create(s) as SqlServerServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("sqlserverService", info.Id);
            Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
            Assert.Equal("192.168.0.90", info.Host);
            Assert.Equal(1433, info.Port);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
            Assert.Equal("reconnect=true", info.Query);
        }

        [Fact]
        public void Create_CreatesValidServiceBinding_NoUri()
        {
            Service s = new Service()
            {
                Label = "sqlserver",
                Tags = new string[] { "sqlserver", "relational" },
                Name = "sqlserverService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("1433") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") }
                }
            };
            SqlServerServiceInfoFactory factory = new SqlServerServiceInfoFactory();
            var info = factory.Create(s) as SqlServerServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("sqlserverService", info.Id);
            Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
            Assert.Equal("192.168.0.90", info.Host);
            Assert.Equal(1433, info.Port);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
        }
    }
}
