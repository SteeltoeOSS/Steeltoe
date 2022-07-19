// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public partial class PlaceholderResolverProviderTest
{
    [Fact]
    public void AdjustConfigManagerBuilder_CorrectlyReflectNewValues()
    {
        var manager = new ConfigurationManager();
        var template = new Dictionary<string, string> { { "placeholder", "${value}" } };
        var valueProviderA = new Dictionary<string, string> { { "value", "a" } };
        var valueProviderB = new Dictionary<string, string> { { "value", "b" } };
        manager.AddInMemoryCollection(template);
        manager.AddInMemoryCollection(valueProviderA);
        manager.AddInMemoryCollection(valueProviderB);
        manager.AddPlaceholderResolver();
        var result = manager.GetValue<string>("placeholder");
        Assert.Equal("b", result);

        // TODO: Investigate and fix caching issue with IConfiguration
        // var builder = (IConfigurationBuilder)manager;
        // var firstSource = builder.Sources.OfType<MemoryConfigurationSource>().First(x => x.InitialData is not null && x.InitialData.SequenceEqual(valueProviderB));
        // builder.Sources.Remove(firstSource);
        // result = manager.GetValue<string>("placeholder");
        // Assert.Equal("a", result);
    }
}
#endif
