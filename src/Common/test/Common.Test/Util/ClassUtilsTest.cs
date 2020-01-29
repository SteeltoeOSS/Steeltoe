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

namespace Steeltoe.Common.Util.Test
{
    public class ClassUtilsTest
    {
        [Fact]
        public void TestIsAssignable_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignable(null, null));
            Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignable(typeof(string), null));
        }

        [Fact]
        public void TestIsAssignableValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignableValue(null, null));
        }

        [Fact]
        public void TestIsAssignableValue()
        {
            Assert.True(ClassUtils.IsAssignableValue(typeof(string), null));
            Assert.True(ClassUtils.IsAssignableValue(typeof(string), string.Empty));
            Assert.True(ClassUtils.IsAssignableValue(typeof(object), string.Empty));
        }

        [Fact]
        public void TestIsAssignable()
        {
            Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(object)));
            Assert.True(ClassUtils.IsAssignable(typeof(string), typeof(string)));
            Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(string)));
            Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(int)));
            Assert.True(ClassUtils.IsAssignable(typeof(int), typeof(int)));
            Assert.True(ClassUtils.IsAssignable(typeof(int), typeof(int)));
            Assert.False(ClassUtils.IsAssignable(typeof(string), typeof(object)));
            Assert.False(ClassUtils.IsAssignable(typeof(int), typeof(double)));
            Assert.False(ClassUtils.IsAssignable(typeof(double), typeof(int)));
        }
    }
}
