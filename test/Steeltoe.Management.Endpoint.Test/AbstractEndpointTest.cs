//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Steeltoe.Management.Endpoint.Security;
using Xunit;


namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractEndpointTest : BaseTest
    {
        [Fact]
        public void ThrowsIfOptionsull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestEndpoint(null));
        }

        [Fact]
        public void ReturnsOptionValues()
        {
            var ep = new TestEndpoint(new TestOptions() { Id = "foo", Enabled = false, Sensitive = false });
            Assert.False(ep.Sensitive);
            Assert.False(ep.Enabled);
            Assert.Equal("foo", ep.Id);
            Assert.Equal(0, ep.Invoke());
        }

    }

    class TestEndpoint : AbstractEndpoint<int>
    {

        public TestEndpoint(IEndpointOptions opts) : base(opts)
        {

        }
    }

    class TestOptions : IEndpointOptions
    {
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public bool Sensitive { get; set; }
        public IManagementOptions Global { get; set; }
        public string Path { get; set; }
        public Permissions RequiredPermissions { get; set; }
        public bool IsAccessAllowed(Permissions permissions)
        {
            return false;
        }
    }
}
