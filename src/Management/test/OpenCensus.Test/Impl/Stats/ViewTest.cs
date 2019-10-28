﻿// <copyright file="ViewTest.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Stats.Test
{
    using System;
    using System.Collections.Generic;
    using OpenCensus.Common;
    using OpenCensus.Stats.Aggregations;
    using OpenCensus.Stats.Measures;
    using OpenCensus.Tags;
    using Xunit;

    public class ViewTest
    {
        private static readonly IViewName NAME = ViewName.Create("test-view-name");
        private static readonly String DESCRIPTION = "test-view-name description";
        private static readonly IMeasure MEASURE =
            MeasureDouble.Create("measure", "measure description", "1");

        private static readonly ITagKey FOO = TagKey.Create("foo");
        private static readonly ITagKey BAR = TagKey.Create("bar");
        private static readonly List<ITagKey> keys = new List<ITagKey>() { FOO, BAR };
        private static readonly IMean MEAN = Mean.Create();
        private static readonly IDuration MINUTE = Duration.Create(60, 0);
        private static readonly IDuration TWO_MINUTES = Duration.Create(120, 0);
        private static readonly IDuration NEG_TEN_SECONDS = Duration.Create(-10, 0);

        [Fact]
        public void TestConstants()
        {
            Assert.Equal(255, ViewName.NameMaxLength);
        }

        [Fact]
        public void TestDistributionView()
        {
            IView view = View.Create(NAME, DESCRIPTION, MEASURE, MEAN, keys);
            Assert.Equal(NAME, view.Name);
            Assert.Equal(DESCRIPTION, view.Description);
            Assert.Equal(MEASURE.Name, view.Measure.Name);
            Assert.Equal(MEAN, view.Aggregation);
            Assert.Equal(2, view.Columns.Count);
            Assert.Equal(FOO, view.Columns[0]);
            Assert.Equal(BAR, view.Columns[1]);

        }

        [Fact]
        public void testViewEquals()
        {

            IView view1 = View.Create(NAME, DESCRIPTION, MEASURE, MEAN, keys);
            IView view2 = View.Create(NAME, DESCRIPTION, MEASURE, MEAN, keys);
            Assert.Equal(view1, view2);
            IView view3 = View.Create(NAME, DESCRIPTION + 2, MEASURE, MEAN, keys);
            Assert.NotEqual(view1, view3);
            Assert.NotEqual(view2, view3);

        }

        [Fact]
        public void PreventDuplicateColumns()
        {
            Assert.Throws<ArgumentException>(() => View.Create(
                NAME,
                DESCRIPTION,
                MEASURE,
                MEAN,
                new List<ITagKey>() { TagKey.Create("duplicate"), TagKey.Create("duplicate") }));
        }

        [Fact]
        public void PreventNullViewName()
        {
            Assert.Throws<ArgumentNullException>(() => View.Create(null, DESCRIPTION, MEASURE, MEAN, keys));
        }

        [Fact]
        public void PreventTooLongViewName()
        {
            char[] chars = new char[ViewName.NameMaxLength + 1];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = 'a';
            }

            String longName = new String(chars);
            Assert.Throws<ArgumentOutOfRangeException>(() => ViewName.Create(longName));
        }

        [Fact]
        public void PreventNonPrintableViewName()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ViewName.Create("\u0002"));
        }

        [Fact]
        public void TestViewName()
        {
            Assert.Equal("my name", ViewName.Create("my name").AsString);
        }

        [Fact]
        public void PreventNullNameString()
        {
            Assert.Throws<ArgumentNullException>(() => ViewName.Create(null));
        }

        [Fact]
        public void TestViewNameEquals()
        {
            Assert.Equal(ViewName.Create("view-1"), ViewName.Create("view-1"));
            Assert.NotEqual(ViewName.Create("view-1"), ViewName.Create("view-2"));

        }
    }
}
