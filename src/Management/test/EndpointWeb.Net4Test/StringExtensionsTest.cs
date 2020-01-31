// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
