// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Common.Util.Test
{
    public class DefaultIdGeneratorTest
    {
        [Fact]
        public void TestGenerateId()
        {
            Assert.NotEqual(default(Guid).ToString(), new DefaultIdGenerator().GenerateId());
        }
    }
}
