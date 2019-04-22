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

using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoEndpointTest : BaseTest
    {

        [Fact]
        public void Invoke_NoContributors_ReturnsExpectedInfo()
        {

            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>();
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();
            Assert.NotNull(info);
            Assert.Equal(0, info.Count);

        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(), new TestContrib() };
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach(var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                Assert.True(tc.called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions()
        {
            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(true), new TestContrib() };
      
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                if (tc.throws)
                    Assert.False(tc.called);
                else
                    Assert.True(tc.called);
            }
        }
    }

    class TestContrib : IInfoContributor
    {
        public bool called = false;
        public bool throws = false;
        public TestContrib()
        {
            this.throws = false;
        }
        public TestContrib(bool throws)
        {
            this.throws = throws;
        }
        public void Contribute(IInfoBuilder builder)
        {
            if (throws)
            {
                throw new Exception();
            }
            called = true;
        }
    }
}
