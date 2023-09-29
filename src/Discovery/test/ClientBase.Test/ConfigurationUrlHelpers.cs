// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Discovery.ClientBase.Test;

public class ConfigurationUrlHelpers
{
    [Fact]
    public void GetAspNetCoreUrlsHandlesTrailingSemicolon()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() { { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233;" } }).Build();

        var urls = config.GetAspNetCoreUrls()
                        .Select(x => x.ToString());

        Assert.Equal(4, urls.Count());
        Assert.DoesNotContain(string.Empty, urls);
        Assert.Contains("https://myapp:1234/", urls);
        Assert.Contains("http://0.0.0.0:1233/", urls);
        Assert.Contains("http://---asterisk---:1233/", urls);
        Assert.Contains("http://---asterisk---:1233/", urls);
    }
}
