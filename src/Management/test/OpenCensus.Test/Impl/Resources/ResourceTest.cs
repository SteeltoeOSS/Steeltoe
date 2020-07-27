// <copyright file="AnnotationTest.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Impl.Resources
{
    using Xunit;
    using OpenCensus.Resources;
    using OpenCensus.Tags;
    using OpenCensus.Implementation;

    public class ResourceTest
    {
        [Fact]
        public void TryParseResourceType_NullOrEmptyType_GlobalResourceSet()
        {
            // Arrange
            var rawResourceType = string.Empty;

            // Act (empty)
            var parsed = Resource.TryParseResourceType(rawResourceType, out var resourceType);

            // Assert (empty)
            Assert.False(parsed);
            Assert.Equal(Constants.GlobalResourceType, resourceType);

            // Act (null)
            parsed = Resource.TryParseResourceType(rawResourceType, out resourceType);

            // Assert (null)
            Assert.False(parsed);
            Assert.Equal(Constants.GlobalResourceType, resourceType);
        }

        [Fact]
        public void TryParseResourceType_LongResourceTypeName_GlobalResourceSet()
        {
            // Arrange
            var longResouceTypeName = "a".PadLeft(Constants.MaxResourceTypeNameLength + 1, 'a');

            // Act
            var parsed = Resource.TryParseResourceType(longResouceTypeName, out var resourceType);

            // Assert
            Assert.False(parsed);
            Assert.Equal(Constants.GlobalResourceType, resourceType);
        }

        [Fact]
        public void TryParseResourceType_NameWithSpaces_SpacesTrimmed()
        {
            // Arrange
            var rawResouceType = "  a    ";

            // Act
            var parsed = Resource.TryParseResourceType(rawResouceType, out var resourceType);

            // Assert
            Assert.True(parsed);
            Assert.Equal("a", resourceType);
        }

        [Fact]
        public void ParseResourceLabels_WrongKeyValueDelimiter_PairIgnored()
        {
            // Arrange
            var resourceLabels = "k1:v1,k2=v2";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.NotEmpty(tags);
            Assert.Single(tags);
            Assert.Equal(TagKey.Create("k2"), tags[0].Key);
            Assert.Equal(TagValue.Create("v2"), tags[0].Value);
        }

        [Fact]
        public void ParseResourceLabels_LongValueName_AllLaterLabelsIgnored()
        {
            // Arrange
            var longValue = "a".PadLeft(Constants.MaxResourceTypeNameLength + 1, 'a');
            var resourceLabels = $"k1={longValue};k2=v2";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.Empty(tags);
        }

        [Fact]
        public void ParseResourceLabels_LongKeyName_AllLaterLabelsIgnored()
        {
            // Arrange
            var longKey = "a".PadLeft(Constants.MaxResourceTypeNameLength + 1, 'a');
            var resourceLabels = $"{longKey}=v1;k2=v2";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.Empty(tags);
        }

        [Fact]
        public void ParseResourceLabels_ValueWithParenthesis_StrippedValue()
        {
            // Arrange
            var resourceLabels = "k1=\"v1\"";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.NotEmpty(tags);
            Assert.Equal(TagKey.Create("k1"), tags[0].Key);
            Assert.Equal(TagValue.Create("v1"), tags[0].Value);
        }

        [Fact]
        public void ParseResourceLabels_EmptyString_EmptyMapReturned()
        {
            // Arrange
            var resourceLabels = "";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.Empty(tags);
        }

        [Fact]
        public void ParseResourceLabels_CommaSeparated_MapReturned()
        {
            // Arrange
            var resourceLabels = "key1=val1,key2=val2";

            // Act
            var tags = Resource.ParseResourceLabels(resourceLabels);

            // Assert
            Assert.NotNull(tags);
            Assert.Equal(2, tags.Length);
            Assert.Equal(TagKey.Create("key1"), tags[0].Key);
            Assert.Equal(TagKey.Create("key2"), tags[1].Key);
            Assert.Equal(TagValue.Create("val1"), tags[0].Value);
            Assert.Equal(TagValue.Create("val2"), tags[1].Value);
        }
    }
}
