// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNullRepo()
        {
            Assert.Throws<ArgumentNullException>(() => new TraceEndpoint(new TraceEndpointOptions(), null));
        }

        [Fact]
        public void DoInvoke_CallsTraceRepo()
        {
            var repo = new TestTraceRepo();
            var ep = new TraceEndpoint(new TraceEndpointOptions(), repo);
            var result = ep.DoInvoke(repo);
            Assert.NotNull(result);
            Assert.True(repo.GetTracesCalled);
        }
    }
}
