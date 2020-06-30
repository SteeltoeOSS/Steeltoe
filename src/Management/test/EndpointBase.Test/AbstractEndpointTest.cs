// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
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
            var ep = new TestEndpoint(new TestOptions() { Id = "foo", Enabled = false });
            Assert.False(ep.Enabled);
            Assert.Equal("foo", ep.Id);
            Assert.Equal(0, ep.Invoke());
        }
    }
}
