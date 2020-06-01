// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Common.Configuration.Test
{
    public class PropertyPlaceholderHelperTest
    {
        [Fact]
        public void ResolvePlaceholders_ResolvesSinglePlaceholder()
        {
            // Arrange
            string text = "foo=${foo}";
            ConfigurationBuilder builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "foo", "bar" }
                };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            string result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesSingleSpringPlaceholder()
        {
            // Arrange
            var text = "foo=${foo.bar}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "foo:bar", "bar" }
                };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            var result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultiplePlaceholders()
        {
            // Arrange
            string text = "foo=${foo},bar=${bar}";
            ConfigurationBuilder builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "foo", "bar" },
                    { "bar", "baz" }
                };
            builder.AddInMemoryCollection(dic1);

            // Act and Assert
            string result = PropertyPlaceholderHelper.ResolvePlaceholders(text, builder.Build());
            Assert.Equal("foo=bar,bar=baz", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleSpringPlaceholders()
        {
            // Arrange
            var text = "foo=${foo.boo},bar=${bar.far}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "foo:boo", "bar" },
                    { "bar:far", "baz" }
                };
            builder.AddInMemoryCollection(dic1);

            // Act and Assert
            var result = PropertyPlaceholderHelper.ResolvePlaceholders(text, builder.Build());
            Assert.Equal("foo=bar,bar=baz", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursivePlaceholders()
        {
            // Arrange
            string text = "foo=${bar}";
            ConfigurationBuilder builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "bar", "${baz}" },
                    { "baz", "bar" }
                };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            string result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursiveSpringPlaceholders()
        {
            // Arrange
            var text = "foo=${bar.boo}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "bar:boo", "${baz.faz}" },
                    { "baz:faz", "bar" }
                };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            var result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursiveInPlaceholders()
        {
            // Arrange
            string text1 = "foo=${b${inner}}";
            ConfigurationBuilder builder1 = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "bar", "bar" },
                    { "inner", "ar" }
                };
            builder1.AddInMemoryCollection(dic1);
            var config1 = builder1.Build();

            string text2 = "${top}";
            ConfigurationBuilder builder2 = new ConfigurationBuilder();
            var dic2 = new Dictionary<string, string>()
                {
                    { "top", "${child}+${child}" },
                    { "child", "${${differentiator}.grandchild}" },
                    { "differentiator", "first" },
                    { "first.grandchild", "actualValue" }
                };
            builder2.AddInMemoryCollection(dic2);
            var config2 = builder2.Build();

            // Act and Assert
            string result1 = PropertyPlaceholderHelper.ResolvePlaceholders(text1, config1);
            Assert.Equal("foo=bar", result1);
            string result2 = PropertyPlaceholderHelper.ResolvePlaceholders(text2, config2);
            Assert.Equal("actualValue+actualValue", result2);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursiveInSpringPlaceholders()
        {
            // Arrange
            var text1 = "foo=${b${inner.placeholder}}";
            var builder1 = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "bar", "bar" },
                    { "inner:placeholder", "ar" }
                };
            builder1.AddInMemoryCollection(dic1);
            var config1 = builder1.Build();

            var text2 = "${top}";
            var builder2 = new ConfigurationBuilder();
            var dic2 = new Dictionary<string, string>()
                {
                    { "top", "${child}+${child}" },
                    { "child", "${${differentiator}.grandchild}" },
                    { "differentiator", "first" },
                    { "first:grandchild", "actualValue" }
                };
            builder2.AddInMemoryCollection(dic2);
            var config2 = builder2.Build();

            // Act and Assert
            var result1 = PropertyPlaceholderHelper.ResolvePlaceholders(text1, config1);
            Assert.Equal("foo=bar", result1);
            var result2 = PropertyPlaceholderHelper.ResolvePlaceholders(text2, config2);
            Assert.Equal("actualValue+actualValue", result2);
        }

        [Fact]
        public void ResolvePlaceholders_UnresolvedPlaceholderIsIgnored()
        {
            // Arrange
            string text = "foo=${foo},bar=${bar}";
            ConfigurationBuilder builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>()
                {
                    { "foo", "bar" }
                };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            string result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=bar,bar=${bar}", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesArrayRefPlaceholder()
        {
            // Arrange
            var json1 = @"
{
    ""vcap"": {
        ""application"": {
          ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
          ""application_name"": ""my-app"",
          ""application_uris"": [
            ""my-app.10.244.0.34.xip.io""
          ],
          ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
          ""limits"": {
            ""disk"": 1024,
            ""fds"": 16384,
            ""mem"": 256
          },
          ""name"": ""my-app"",
          ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
          ""space_name"": ""my-space"",
          ""uris"": [
            ""my-app.10.244.0.34.xip.io"",
            ""my-app2.10.244.0.34.xip.io""
          ],
          ""users"": null,
          ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
        }
    }
}";
            var path = CreateTempFile(json1);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(directory);

            builder.AddJsonFile(fileName);
            var config = builder.Build();

            string text = "foo=${vcap:application:uris[1]}";

            // Act and Assert
            string result = PropertyPlaceholderHelper.ResolvePlaceholders(text, config);
            Assert.Equal("foo=my-app2.10.244.0.34.xip.io", result);
        }

        [Fact]
        public void GetResolvedConfigurationPlaceholders_ReturnsValues_WhenResolved()
        {
            // arrange
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(
                new Dictionary<string, string>()
                {
                    { "foo", "${bar}" },
                    { "bar", "baz" }
                });

            // act
            var resolved = PropertyPlaceholderHelper.GetResolvedConfigurationPlaceholders(builder.Build());

            // assert
            Assert.Contains(resolved, f => f.Key == "foo");
            Assert.DoesNotContain(resolved, f => f.Key == "bar");
            Assert.Equal("baz", resolved.First(k => k.Key == "foo").Value);
        }

        [Fact]
        public void GetResolvedConfigurationPlaceholders_ReturnsEmpty_WhenUnResolved()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(
                new Dictionary<string, string>()
                {
                    { "foo", "${bar}" }
                });

            // act
            var resolved = PropertyPlaceholderHelper.GetResolvedConfigurationPlaceholders(builder.Build());

            // assert
            Assert.Contains(resolved, f => f.Key == "foo");
            Assert.Equal(string.Empty, resolved.First(k => k.Key == "foo").Value);
        }

        private static string CreateTempFile(string contents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
    }
}
