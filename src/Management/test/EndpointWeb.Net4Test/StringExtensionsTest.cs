// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class StringExtensionsTest
    {
        [Fact]
        public void StartsWithSegments_ReturnsFalseWithNoMatch()
        {
            // arrange
            var incomingPath = "ShouldNotBeFound";
            var configuredPath = "notconfigured";

            // act
            var result = incomingPath.StartsWithSegments(configuredPath, null, out var remnant);

            // assert
            Assert.False(result);
            Assert.Equal(string.Empty, remnant);
        }

        [Fact]
        public void StartsWithSegments_ReturnsTrueAndSetsRemnantWithMatch()
        {
            // arrange
            var configuredPath = "found";
            var incomingPath = "found/something";

            // act
            var result = incomingPath.StartsWithSegments(configuredPath, null, out var remnant);

            // assert
            Assert.True(result);
            Assert.Equal("/something", remnant);
        }

        [Fact]
        public void StartsWithSegments_WithBasePath_ReturnsTrueAndSetsRemnantWithMatch()
        {
            // arrange
            var configuredPath1 = "cloudfoundryapplication/loggers";
            var incomingPath1 = "cloudfoundryapplication/loggers/something";
            var configuredPath2 = "actuator/metrics";
            var incomingPath2 = "actuator/metrics/something";
            var basePaths = new List<string> { "actuator", "cloudfoundryapplication" };

            // act
            var result1 = incomingPath1.StartsWithSegments(configuredPath1, basePaths, out var remnant1);
            var result2 = incomingPath2.StartsWithSegments(configuredPath2, basePaths, out var remnant2);

            // assert
            Assert.True(result1);
            Assert.Equal("/something", remnant1);
            Assert.True(result2);
            Assert.Equal("/something", remnant2);
        }
    }
}
