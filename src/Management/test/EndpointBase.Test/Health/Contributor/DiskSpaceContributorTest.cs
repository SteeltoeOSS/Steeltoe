// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Contributor.Test
{
    public class DiskSpaceContributorTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var contrib = new DiskSpaceContributor();
            Assert.Equal("diskSpace", contrib.Id);
        }

        [Fact]
        public void Health_InitializedWithDefaults_ReturnsExpected()
        {
            var contrib = new DiskSpaceContributor();
            Assert.Equal("diskSpace", contrib.Id);
            var result = contrib.Health();
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.NotNull(result.Details);
            Assert.True(result.Details.ContainsKey("total"));
            Assert.True(result.Details.ContainsKey("free"));
            Assert.True(result.Details.ContainsKey("threshold"));
            Assert.True(result.Details.ContainsKey("status"));
        }
    }
}
