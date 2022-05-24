// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ServiceFactoryAccessorTests
    {
        private IServiceProvider serviceProvider;

        public ServiceFactoryAccessorTests()
        {
            var config = new ConfigurationBuilder().Build();
            var collection = new ServiceCollection();
            collection.AddSingleton<IConfiguration>(config);
            collection.AddSingleton<IApplicationContext>(p => new GenericApplicationContext(p, config));
            collection.AddSingleton(typeof(Car));
            collection.AddSingleton(typeof(Boat));
            serviceProvider = collection.BuildServiceProvider();
        }

        [Fact]
        public void TestServiceAccess()
        {
            var appContext = serviceProvider.GetService<IApplicationContext>();
            var context = new StandardEvaluationContext
            {
                ServiceResolver = new ServiceFactoryResolver(appContext)
            };

            var car = appContext.GetService<Car>();
            var boat = appContext.GetService<Boat>();

            var expr = new SpelExpressionParser().ParseRaw("@'T(Steeltoe.Common.Expression.Internal.Contexts.ServiceFactoryAccessorTests$Car)car'.Colour");
            Assert.Equal("red", expr.GetValue<string>(context));
            expr = new SpelExpressionParser().ParseRaw("@car.Colour");
            Assert.Equal("red", expr.GetValue<string>(context));

            expr = new SpelExpressionParser().ParseRaw("@'T(Steeltoe.Common.Expression.Internal.Contexts.ServiceFactoryAccessorTests$Boat)boat'.Colour");
            Assert.Equal("blue", expr.GetValue<string>(context));
            expr = new SpelExpressionParser().ParseRaw("@boat.Colour");
            Assert.Equal("blue", expr.GetValue<string>(context));

            var noServiceExpr = new SpelExpressionParser().ParseRaw("@truck");
            Assert.Throws<SpelEvaluationException>(() => noServiceExpr.GetValue(context));
        }

        public class Car : IServiceNameAware
        {
            public string Colour => "red";

            public string ServiceName { get; set; } = "car";
        }

        public class Boat : IServiceNameAware
        {
            public string Colour => "blue";

            public string ServiceName { get; set; } = "boat";
        }
    }
}
