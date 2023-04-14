// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Steeltoe.Common.TestResources;

public static class HelperExtensions
{
    /// <summary>
    /// Add a mock IActionDescriptorCollectionProvider for testing
    /// </summary>
    /// <param name="svc">
    /// The IServiceCollection to update
    /// </param>
    /// <returns>
    /// The updated IServiceCollection
    /// </returns>
    public static IServiceCollection AddActionDescriptorCollectionProvider(this IServiceCollection svc)
    {
        return svc.AddSingleton(_ =>
        {
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();

            actionDescriptorCollectionProviderMock.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>(), 0));

            return actionDescriptorCollectionProviderMock.Object;
        });
    }
}
