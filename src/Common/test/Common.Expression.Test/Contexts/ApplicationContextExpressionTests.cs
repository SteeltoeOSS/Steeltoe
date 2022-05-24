// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ApplicationContextExpressionTests
    {
        private IServiceProvider serviceProvider;

        public ApplicationContextExpressionTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                new Dictionary<string, string> { { "code", "123" } })
                .Build();
            var collection = new ServiceCollection();
            collection.AddSingleton<IConfiguration>(config);
            collection.AddSingleton(p =>
            {
                var tb = new TestService
                {
                    ServiceName = "tb0"
                };
                return tb;
            });

            collection.AddSingleton(p =>
            {
                var tb = new TestService
                {
                    ServiceName = "tb1"
                };
                return tb;
            });

            collection.AddSingleton<IApplicationContext>(p =>
            {
                var context = new GenericApplicationContext(p, config)
                {
                    ServiceExpressionResolver = new StandardServiceExpressionResolver()
                };
                return context;
            });

            serviceProvider = collection.BuildServiceProvider();
        }

        [Fact]
        public void GenericApplicationContext()
        {
            var context = serviceProvider.GetService<IApplicationContext>();
            var services = context.GetServices<TestService>();
            Assert.Equal(2, services.Count());
            Assert.Equal("XXXtb0YYYZZZ", Evaluate("XXX#{tb0.Name}YYYZZZ"));
            Assert.Equal("123", Evaluate("${code}"));
            Assert.Equal("123", Evaluate("${code?#{null}}"));
            Assert.Null(Evaluate("${codeX?#{null}}"));
            Assert.Equal("123 tb1", Evaluate("${code} #{tb1.Name}"));
            Assert.Equal("foo tb1", Evaluate("${bar?foo} #{tb1.Name}"));
        }

        private object Evaluate(string value)
        {
            var context = serviceProvider.GetService<IApplicationContext>();
            var result = context.ResolveEmbeddedValue(value);
            return context.ServiceExpressionResolver.Evaluate(result, new ServiceExpressionContext(context));
        }

        public class TestService : IServiceNameAware
        {
            public string Name => ServiceName;

            public string ServiceName { get; set; } = "NotSet";
        }

        public class ValueTestService : IServiceNameAware
        {
            public string ServiceName { get; set; } = "tb3";
        }
    }
}
