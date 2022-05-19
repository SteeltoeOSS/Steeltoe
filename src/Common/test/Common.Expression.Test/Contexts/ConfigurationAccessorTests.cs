// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ConfigurationAccessorTests
    {
        private IServiceProvider serviceProvider;

        public ConfigurationAccessorTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                new Dictionary<string, string>() { { "my.name", "myservice" } })
                .Build();
            var collection = new ServiceCollection();
            collection.AddSingleton<IConfiguration>(config);
            collection.AddSingleton<IApplicationContext>((p) => new GenericApplicationContext(p, config));

            serviceProvider = collection.BuildServiceProvider();
        }

        [Fact]
        public void TestBraceAccess()
        {
            var rootObject = new ServiceExpressionContext(serviceProvider.GetService<IApplicationContext>());
            var context = new StandardEvaluationContext(rootObject);
            context.AddPropertyAccessor(new ServiceExpressionContextAccessor());
            context.AddPropertyAccessor(new ConfigurationAccessor());
            var sep = new SpelExpressionParser();

            // basic
            var ex = sep.ParseExpression("configuration['my.name']");
            Assert.Equal("myservice", ex.GetValue<string>(context));
        }
    }
}
