// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Config;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class SourceBindingWithGlobalPropertiesTest : AbstractTest
{
    [Fact]
    public async Task TestGlobalPropertiesSet()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.default.contentType=application/json",
            "spring.cloud.stream.bindings.output.destination=ticktock", "spring.cloud.stream.default.producer.requiredGroups:0=someGroup",
            "spring.cloud.stream.default.producer.partitionCount=1", "spring.cloud.stream.bindings.output.producer.headerMode=none",
            "spring.cloud.stream.bindings.output.producer.partitionCount=4", "spring.cloud.stream.defaultBinder=mock").BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        IBinder binder = factory.GetBinder(null);
        Assert.NotNull(binder);

        var bindingServiceProperties = provider.GetService<IOptions<BindingServiceOptions>>();
        Assert.NotNull(bindingServiceProperties.Value);
        BindingOptions bindingProperties = bindingServiceProperties.Value.GetBindingOptions("output");
        Assert.NotNull(bindingProperties);
        Assert.Equal("application/json", bindingProperties.ContentType);
        Assert.Equal("ticktock", bindingProperties.Destination);
        Assert.NotNull(bindingProperties.Producer);
        Assert.Single(bindingProperties.Producer.RequiredGroups);
        Assert.Contains("someGroup", bindingProperties.Producer.RequiredGroups);
        Assert.Equal(4, bindingProperties.Producer.PartitionCount);
        Assert.Equal(HeaderMode.None, bindingProperties.Producer.HeaderMode);
    }
}
