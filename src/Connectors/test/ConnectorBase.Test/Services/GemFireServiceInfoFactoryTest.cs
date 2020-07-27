// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System.Linq;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class GemFireServiceInfoFactoryTest
    {
        private readonly Service boundGemFireService = new Service()
        {
            Label = "p-cloudcache",
            Tags = new string[] { "gemfire", "cloudcache", "database", "pivotal" },
            Name = "myPCCService",
            Plan = "dev-plan",
            Credentials = new Credential()
            {
                { "distributed_system_id", new Credential("0") },
                {
                    "locators",
                    new Credential()
                    {
                        { "0", new Credential("10.194.45.168[55221]") }
                    }
                },
                {
                    "urls",
                    new Credential()
                    {
                        { "gfsh", new Credential("https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/gemfire/v1") },
                        { "pulse", new Credential("https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/pulse") }
                    }
                },
                {
                    "users",
                    new Credential()
                    {
                        {
                            "0",
                            new Credential()
                            {
                                { "username", new Credential("cluster_operator_ftqmv76NgWDu8q8vNvxuQQ") },
                                {
                                    "roles",
                                    new Credential()
                                    {
                                        {
                                            "0",
                                            new Credential("cluster_operator")
                                        }
                                    }
                                },
                                { "password", new Credential("nOVqiwOmLL6O54lGmlcfw") }
                            }
                        },
                        {
                            "1",
                            new Credential()
                            {
                                { "username", new Credential("developer_G5XmmQBfhIXyz0vgcAOEg") },
                                {
                                    "roles",
                                    new Credential()
                                    {
                                        {
                                            "0",
                                            new Credential("developer")
                                        }
                                    }
                                },
                                { "password", new Credential("a04RMKLFPjYmYS4M5GvY0A") }
                            }
                        }
                    }
                },
                {
                    "wan",
                    new Credential()
                    {
                        {
                            "sender_credentials",
                            new Credential()
                            {
                                {
                                    "active",
                                    new Credential()
                                    {
                                        { "password", new Credential("INFbDQVjvxunZ1nl9ObSQ") },
                                        { "username", new Credential("gateway_sender_gaogaWQwOlJbSg4qItqEg") }
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };

        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var factory = new GemFireServiceInfoFactory();
            Assert.True(factory.Accept(boundGemFireService));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-mysql",
                Tags = new string[] { "mysql", "relational" },
                Name = "mySqlService",
                Plan = "100mb-dev",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") }
                }
            };
            var factory = new GemFireServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var factory = new GemFireServiceInfoFactory();
            var info = factory.Create(boundGemFireService) as GemFireServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("myPCCService", info.Id);
            Assert.Equal(2, info.Users.Count);
            Assert.Single(info.Locators);
            var devUser = info.Users.First(u => u.Roles.Contains("developer"));
            Assert.NotNull(devUser);
            Assert.Equal("developer_G5XmmQBfhIXyz0vgcAOEg", devUser.Username);
            Assert.Equal("a04RMKLFPjYmYS4M5GvY0A", devUser.Password);
        }
    }
}
