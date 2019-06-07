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

using System;
using Xunit;

namespace Steeltoe.Common.Test
{
    public class SecurityUtilitiesTest
    {
        [Fact]
        public void SanitizeInput_ReturnsNullAndEmptyUnchanged()
        {
            Assert.Null(SecurityUtilities.SanitizeInput(null));
            Assert.Equal(string.Empty, SecurityUtilities.SanitizeInput(string.Empty));
        }

        [Fact]
        public void SanitizeInput_EncodesHtml()
        {
            Assert.Equal("&gt;some string&lt;", SecurityUtilities.SanitizeInput(">some string<"));
        }

        [Fact]
        public void SanitizeInput_RemovesCrlf()
        {
            Assert.DoesNotContain("\r", SecurityUtilities.SanitizeInput("some\rparagraph\rwith\rcarriage\rreturns"));
            Assert.DoesNotContain("\n", SecurityUtilities.SanitizeInput("some\nparagraph\nwith\nline\nendings"));
        }
    }
}
