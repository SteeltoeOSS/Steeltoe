// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Services;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Contexts;

public class ServiceFactoryAccessorTests
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceFactoryAccessorTests()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var collection = new ServiceCollection();
        collection.AddSingleton<IConfiguration>(configurationRoot);
        collection.AddSingleton<IApplicationContext>(p => new GenericApplicationContext(p, configurationRoot));
        collection.AddSingleton(typeof(Car));
        collection.AddSingleton(typeof(Boat));
        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public void TestServiceAccess()
    {
        var appContext = _serviceProvider.GetService<IApplicationContext>();

        var context = new StandardEvaluationContext
        {
            ServiceResolver = new ServiceFactoryResolver(appContext)
        };

        IExpression expr = new SpelExpressionParser().ParseRaw($"@'T({typeof(ServiceFactoryAccessorTests).FullName}$Car)car'.Color");
        Assert.Equal("red", expr.GetValue<string>(context));
        expr = new SpelExpressionParser().ParseRaw("@car.Color");
        Assert.Equal("red", expr.GetValue<string>(context));

        expr = new SpelExpressionParser().ParseRaw($"@'T({typeof(ServiceFactoryAccessorTests).FullName}$Boat)boat'.Color");
        Assert.Equal("blue", expr.GetValue<string>(context));
        expr = new SpelExpressionParser().ParseRaw("@boat.Color");
        Assert.Equal("blue", expr.GetValue<string>(context));

        IExpression noServiceExpr = new SpelExpressionParser().ParseRaw("@truck");
        Assert.Throws<SpelEvaluationException>(() => noServiceExpr.GetValue(context));
    }

    public class Car : IServiceNameAware
    {
        public string Color => "red";

        public string ServiceName { get; set; } = "car";
    }

    public class Boat : IServiceNameAware
    {
        public string Color => "blue";

        public string ServiceName { get; set; } = "boat";
    }
}
