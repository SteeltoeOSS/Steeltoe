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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Middleware.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfEndpointNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestMiddleware1(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new TestMiddleware2(null, null, null));
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    internal class TestMiddleware1 : EndpointMiddleware<string>
    {
        public TestMiddleware1(IEndpoint<string> endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger logger)
            : base(endpoint, mgmtOptions, logger: logger)
        {
        }
    }

    internal class TestMiddleware2 : EndpointMiddleware<string, string>
    {
        public TestMiddleware2(IEndpoint<string, string> endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger logger)
            : base(endpoint, mgmtOptions, logger: logger)
        {
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
