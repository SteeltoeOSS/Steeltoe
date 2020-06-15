// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test
{
    public class ThreadDumpEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNullRepo()
        {
            Assert.Throws<ArgumentNullException>(() => new ThreadDumpEndpoint(new ThreadDumpEndpointOptions(), null));
        }

        [Fact]
        public void Invoke_CallsDumpThreads()
        {
            var dumper = new TestThreadDumper();
            var ep = new ThreadDumpEndpoint(new ThreadDumpEndpointOptions(), dumper);
            var result = ep.Invoke();
            Assert.NotNull(result);
            Assert.True(dumper.DumpThreadsCalled);
        }
    }
}
