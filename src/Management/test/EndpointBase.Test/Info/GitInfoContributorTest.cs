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

using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class GitInfoContributorTest : BaseTest
    {
        [Fact]
        public void ReadGitPropertiesMissingPropertiesFile()
        {
            var config = new GitInfoContributor().ReadGitProperties("foobar");
            Assert.Null(config);
        }

        [Fact]
        public void ReadEmptyGitPropertiesFile()
        {
            var path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "empty.git.properties";
            var config = new GitInfoContributor().ReadGitProperties(path);
            Assert.Null(config);
        }

        [Fact]
        public void ReadMalformedGitPropertiesFile()
        {
            var path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "garbage.git.properties";
            var config = new GitInfoContributor().ReadGitProperties(path);
            Assert.NotNull(config);
            Assert.Null(config["git"]);
        }

        [Fact]
        public void ReadGoodPropertiesFile()
        {
            var path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "git.properties";
            var config = new GitInfoContributor().ReadGitProperties(path);
            Assert.NotNull(config);
            Assert.Equal("true", config["git:dirty"]);

            // Verify `\:` strings get converted if they exist in the dates/URLs
            Assert.Equal("https://github.com/spring-projects/spring-boot.git", config["git:remote:origin:url"]);
            Assert.Equal("2017-07-12T12:40:39-0600", config["git:build:time"]);
            Assert.Equal("2017-06-08T06:47:02-0600", config["git:commit:time"]);
        }

        [Fact]
        public void ContributeWithNullBUilderThrows()
        {
            // Uses git.properties file in test project
            var contrib = new GitInfoContributor();
            Assert.Throws<ArgumentNullException>(() => contrib.Contribute(null));
        }

        [Fact]
        public void ContributeAddsToBuilder()
        {
            // Uses git.properties file in test project
            var contrib = new GitInfoContributor();
            var builder = new InfoBuilder();
            contrib.Contribute(builder);

            var result = builder.Build();
            Assert.NotNull(result);
            var gitDict = result["git"] as Dictionary<string, object>;
            Assert.NotNull(gitDict);
            Assert.Equal(7, gitDict.Count);
            Assert.True(gitDict.ContainsKey("build"));
            Assert.True(gitDict.ContainsKey("branch"));
            Assert.True(gitDict.ContainsKey("commit"));
            Assert.True(gitDict.ContainsKey("closest"));
            Assert.True(gitDict.ContainsKey("dirty"));
            Assert.True(gitDict.ContainsKey("remote"));
            Assert.True(gitDict.ContainsKey("tags"));

            // Verify times are correctly converted
            var gitDict2 = gitDict["build"] as Dictionary<string, object>;
            Assert.NotNull(gitDict2);
            Assert.True(gitDict2.ContainsKey("time"));
            var time = gitDict2["time"];
            Assert.Equal(1499884839000, time);

            // Verify times are correctly converted
            var gitDict3 = gitDict["commit"] as Dictionary<string, object>;
            Assert.NotNull(gitDict3);
            Assert.True(gitDict3.ContainsKey("time"));
            time = gitDict3["time"];
            Assert.Equal(1496926022000, time);
        }
    }
}
