// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Steeltoe.Common.TestResources;

public static class HelperExtensions
{
    /// <summary>
    /// Add a mock for <see cref="IActionDescriptorCollectionProvider" />, to be used in tests.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static void AddActionDescriptorCollectionProviderMock(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock.Setup(provider => provider.ActionDescriptors).Returns(new ActionDescriptorCollection([], 0));
            return actionDescriptorCollectionProviderMock.Object;
        });
    }
}
