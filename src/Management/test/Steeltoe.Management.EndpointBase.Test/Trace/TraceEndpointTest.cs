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
