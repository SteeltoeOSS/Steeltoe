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
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggersChangeRequestTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOnNulls()
        {
            Assert.Throws<ArgumentException>(() => new LoggersChangeRequest(null, "foobar"));
            Assert.Throws<ArgumentException>(() => new LoggersChangeRequest("foobar", null));
        }

        [Fact]
        public void Constructor_SetsProperties()
        {
            var cr = new LoggersChangeRequest("foo", "bar");
            Assert.Equal("foo", cr.Name);
            Assert.Equal("bar", cr.Level);
        }
    }
}
