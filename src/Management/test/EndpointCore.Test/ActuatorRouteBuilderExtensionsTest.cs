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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Steeltoe.Management.Endpoint
{
    public class ActuatorRouteBuilderExtensionsTest
    {
        public static IEnumerable<object[]> IEndpointImplementations
        {
            get
            {
                Func<Type, bool> query = t => t.GetInterfaces().Any(type => type.FullName == "Steeltoe.Management.IEndpoint");
                var types = Assembly.Load("Steeltoe.Management.EndpointBase").GetTypes().Where(query).ToList();
                types.AddRange(Assembly.Load("Steeltoe.Management.EndpointCore").GetTypes().Where(query));
                return types.Select(t => new object[] { t });
            }
        }

        [Theory]
        [MemberData(nameof(IEndpointImplementations))]
        public void LookupMiddlewareTest(Type type)
        {
            var (middleware, options) = ActuatorRouteBuilderExtensions.LookupMiddleware(type);
            Assert.NotNull(middleware);
            Assert.NotNull(options);
        }
    }
}
