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


using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixApplicationBuilderExtensionsTest : HystrixTestBase
    {
        [Fact]
        public void UseHystrixMetricsStream_ThrowsIfBuilderNull()
        {
            IApplicationBuilder builder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => builder.UseHystrixMetricsStream());
            Assert.Contains(nameof(builder), ex.Message);
        }
    }
}
