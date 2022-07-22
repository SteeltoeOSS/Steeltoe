// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Config;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class SourceBindingWithGlobalPropertiesOnlyTest : AbstractTest
{
    [Fact]
    public async Task TestGlobalPropertiesSet()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        var provider = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring.cloud.stream.default.contentType=application/json",
                "spring.cloud.stream.default.producer.partitionKeyExpression=key")
            .BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        var binder = factory.GetBinder(null);
        Assert.NotNull(binder);

        var bindingServiceProperties = provider.GetService<IOptions<BindingServiceOptions>>();
        Assert.NotNull(bindingServiceProperties.Value);
        var bindingProperties = bindingServiceProperties.Value.GetBindingOptions("output");
        Assert.NotNull(bindingProperties);
        Assert.Equal("application/json", bindingProperties.ContentType.ToString());
        Assert.NotNull(bindingProperties.Producer);
        Assert.Equal("key", bindingProperties.Producer.PartitionKeyExpression);
    }
}