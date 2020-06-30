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

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class QueueBuilderTest
    {
        [Fact]
        public void BuildsDurableQueue()
        {
            var queue = QueueBuilder.Durable("name").Build();

            Assert.True(queue.IsDurable);
            Assert.Equal("name", queue.QueueName);
        }

        [Fact]
        public void BuildsNonDurableQueue()
        {
            var queue = QueueBuilder.NonDurable("name").Build();

            Assert.False(queue.IsDurable);
            Assert.Equal("name", queue.QueueName);
        }

        [Fact]
        public void BuildsAutoDeleteQueue()
        {
            var queue = QueueBuilder.Durable("name").AutoDelete().Build();

            Assert.True(queue.IsAutoDelete);
        }

        [Fact]
        public void BuildsExclusiveQueue()
        {
            var queue = QueueBuilder.Durable("name").Exclusive().Build();

            Assert.True(queue.IsExclusive);
        }

        [Fact]
        public void AddsArguments()
        {
            var queue = QueueBuilder.Durable("name")
                    .WithArgument("key1", "value1")
                    .WithArgument("key2", "value2")
                    .Build();

            var args = queue.Arguments;

            Assert.Equal("value1", args["key1"]);
            Assert.Equal("value2", args["key2"]);
        }

        [Fact]
        public void AddsMultipleArgumentsAtOnce()
        {
            var arguments = new Dictionary<string, object>();
            arguments.Add("key1", "value1");
            arguments.Add("key2", "value2");

            var queue = QueueBuilder.Durable("name").WithArguments(arguments).Build();
            var args = queue.Arguments;

            Assert.Equal("value1", args["key1"]);
            Assert.Equal("value2", args["key2"]);
        }
    }
}
