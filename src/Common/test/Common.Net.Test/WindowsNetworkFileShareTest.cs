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

using System.Net;
using System.Runtime.InteropServices;
using Xunit;

namespace Steeltoe.Common.Net.Test
{
    public class WindowsNetworkFileShareTest
    {
        [Fact]
        public void GetErrorForKnownNumber_ReturnsKnownError()
        {
            Assert.Equal("Error: Access Denied", WindowsNetworkFileShare.GetErrorForNumber(5));
            Assert.Equal("Error: No Network", WindowsNetworkFileShare.GetErrorForNumber(1222));
        }

        [Fact]
        public void GetErrorForUnknownNumber_ReturnsUnKnownError()
        {
            Assert.Equal("Error: Unknown, 9999", WindowsNetworkFileShare.GetErrorForNumber(9999));
        }

        [Fact]
        public void WindowsNetworkFileShare_Constructor_SetsValuesOn_ConnectSuccess()
        {
            // arrange
            var fakeMPR = new FakeMPR();

            // act
            _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password"), fakeMPR);

            // assert
            Assert.Equal("user", fakeMPR._username);
            Assert.Equal("password", fakeMPR._password);
            Assert.Equal(@"\\server\path", fakeMPR._networkpath);
        }

        [Fact]
        public void WindowsNetworkFileShare_Constructor_ConcatsUserAndDomain()
        {
            // arrange
            var fakeMPR = new FakeMPR();

            // act
            _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password", "domain"), fakeMPR);

            // assert
            Assert.Equal(@"domain\user", fakeMPR._username);
            Assert.Equal("password", fakeMPR._password);
            Assert.Equal(@"\\server\path", fakeMPR._networkpath);
        }

        [Fact]
        public void WindowsNetworkFileShare_Constructor_ThrowsOn_ConnectFail()
        {
            // arrange
            var fakeMPR = new FakeMPR(false);

            // act
            var exception = Assert.Throws<ExternalException>(() => new WindowsNetworkFileShare("doesn't-matter", new NetworkCredential("user", "password"), fakeMPR));

            // assert
            Assert.Equal("Error connecting to remote share - Code: 1200, Error: Bad Device", exception.Message);
        }
    }
}
