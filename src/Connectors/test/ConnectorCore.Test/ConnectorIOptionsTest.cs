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

using Xunit;

namespace Steeltoe.Connector.Test
{
    public class ConnectorIOptionsTest
    {
        [Fact]
        public void Value_Returns_Expected()
        {
            var myOpt = new MyOption();
            var opt = new ConnectorIOptions<MyOption>(myOpt);

            Assert.NotNull(opt.Value);
            Assert.True(opt.Value == myOpt);
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    internal class MyOption
#pragma warning restore SA1402 // File may only contain a single class
    {
        public MyOption()
        {
        }
    }
}
