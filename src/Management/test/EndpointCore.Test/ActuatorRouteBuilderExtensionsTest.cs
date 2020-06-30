// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
