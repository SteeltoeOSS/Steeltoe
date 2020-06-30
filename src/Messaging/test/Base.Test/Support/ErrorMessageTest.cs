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

namespace Steeltoe.Messaging.Support.Test
{
    public class ErrorMessageTest
    {
        [Fact]
        public void TestToString()
        {
            var em = new ErrorMessage(new Exception("foo"));
            var emString = em.ToString();
            Assert.DoesNotContain("original", emString);

            em = new ErrorMessage(new Exception("foo"), Message.Create<string>("bar"));
            emString = em.ToString();
            Assert.Contains("original", emString);
            Assert.Contains(em.OriginalMessage.ToString(), emString);
        }

        [Fact]
        public void TestAnyExceptionType()
        {
            var em = new ErrorMessage(new InvalidOperationException("foo"));
            var emString = em.ToString();
            Assert.Contains("InvalidOperationException", emString);
        }
    }
}
