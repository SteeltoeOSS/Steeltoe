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

using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulRetryOptionsTest
    {
        [Fact]
        public void Constructor_InitsDefaults()
        {
            var opts = new ConsulRetryOptions();
            Assert.False(opts.Enabled);
            Assert.Equal(ConsulRetryOptions.DEFAULT_MAX_RETRY_ATTEMPTS, opts.MaxAttempts);
            Assert.Equal(ConsulRetryOptions.DEFAULT_INITIAL_RETRY_INTERVAL, opts.InitialInterval);
            Assert.Equal(ConsulRetryOptions.DEFAULT_RETRY_MULTIPLIER, opts.Multiplier);
            Assert.Equal(ConsulRetryOptions.DEFAULT_MAX_RETRY_INTERVAL, opts.MaxInterval);
        }
    }
}
