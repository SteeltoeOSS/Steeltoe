// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Contributor.Test
{
    public class BuildInfoContributorTest
    {
        [Fact]
        public void BuildAddsVersionInfo()
        {
            // arrange
            var contributor = new BuildInfoContributor();
            var builder = new InfoBuilder();

            // act
            contributor.Contribute(builder);
            var results = builder.Build();

            // assert
            Assert.True(results.ContainsKey("applicationVersionInfo"));
            Assert.NotNull(results["applicationVersionInfo"]);
            Assert.True(results.ContainsKey("steeltoeVersionInfo"));
            Assert.NotNull(results["steeltoeVersionInfo"]);
        }
    }
}
