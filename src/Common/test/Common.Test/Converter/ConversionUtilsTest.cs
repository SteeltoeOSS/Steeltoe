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

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Steeltoe.Common.Converter.Test
{
    public class ConversionUtilsTest
    {
        [Fact]
        public void TestCanCreateCompatListFrom()
        {
            Assert.False(ConversionUtils.CanCreateCompatListFor(null));
            Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(IList<>)));
            Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(IList<string>)));
            Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(List<string>)));
            Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(List<>)));
            Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(LinkedList<int>)));
            Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(Collection<int>)));
            Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(ArrayList)));
        }

        [Fact]
        public void TestCreateList()
        {
            Assert.Null(ConversionUtils.CreateCompatListFor(null));
            Assert.Null(ConversionUtils.CreateCompatListFor(typeof(IList<>)));
            Assert.IsType<List<string>>(ConversionUtils.CreateCompatListFor(typeof(IList<string>)));
            Assert.IsType<List<string>>(ConversionUtils.CreateCompatListFor(typeof(List<string>)));
            Assert.Null(ConversionUtils.CreateCompatListFor(typeof(List<>)));
            Assert.Null(ConversionUtils.CreateCompatListFor(typeof(LinkedList<int>)));
            Assert.IsType<Collection<int>>(ConversionUtils.CreateCompatListFor(typeof(Collection<int>)));
            Assert.IsType<ArrayList>(ConversionUtils.CreateCompatListFor(typeof(ArrayList)));
            Assert.IsType<ArrayList>(ConversionUtils.CreateCompatListFor(typeof(IList)));
        }

        [Fact]
        public void TestMakeGenericListType()
        {
            Assert.Equal(typeof(List<string>), ConversionUtils.MakeGenericListType(typeof(IList<string>)));
        }
    }
}
