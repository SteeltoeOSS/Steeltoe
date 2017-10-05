//
// Copyright 2015 the original author or authors.
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
//

using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class ConnectorExceptionTest
    {
        [Fact]
        public void Constructor_CapturesMessage()
        {
            var ex = new ConnectorException("Test");
            Assert.Equal("Test", ex.Message);

            var ex2 = new ConnectorException("Test2", new Exception());
            Assert.Equal("Test2", ex2.Message);
        }

        [Fact]
        public void Constructor_CapturesNestedException()
        {
            var inner = new Exception();
            var ex = new ConnectorException("Test2", inner);
            Assert.Equal(inner, ex.InnerException);
        }
    }
}
