// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Common.Http.Test;

public class ConfigurationUrlHelpers
{
    [InlineData("https://myapp:1234;", "https://myapp:1234/", null, 1)]
    [InlineData("http://0.0.0.0:1233;", "http://0.0.0.0:1233/", null, 1)]
    [InlineData("http://+;", "http://0.0.0.0/", "http://[::]/", 2)]
    [InlineData("http://*:1233;", "http://0.0.0.0:1233/", "http://[::]:1233/", 2)]
    [InlineData("http://[::]:1233;", "http://[::]:1233/", null, 1)]
    [Theory]
    public void GetAspNetCoreUrlsHandlesScenarios(string configUri, string expectedUriOne, string expectedUriTwo, int count)
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", configUri }
        }).Build();

        IEnumerable<string> urls = config.GetAspNetCoreUrls().Select(x => x.ToString());

        Assert.Equal(count, urls.Count());
        Assert.Contains(expectedUriOne, urls);

        if (!string.IsNullOrEmpty(expectedUriTwo))
        {
            Assert.Contains(expectedUriTwo, urls);
        }
    }
}
