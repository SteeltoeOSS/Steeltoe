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

using Microsoft.Extensions.Configuration;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.StubBinder1
{
    public class StubBinder1 : IBinder<object>
    {
        public const string BINDER_NAME = "binder1";

        public Func<string, string, object, IConsumerOptions, IBinding> BindConsumerFunc { get; set; }

        public Func<string, object, IProducerOptions, IBinding> BindProducerFunc { get; set; }

        public string Name { get; set; } = BINDER_NAME;

        public Type TargetType => typeof(object);

        public IServiceProvider ServiceProvider { get; }

        public IConfiguration Configuration { get; }

        public StubBinder1(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            ServiceProvider = serviceProvider;
            Configuration = configuration;
        }

        public IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            if (BindConsumerFunc != null)
            {
                return BindConsumerFunc(name, group, inboundTarget, consumerOptions);
            }

            return null;
        }

        public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
        {
            if (BindProducerFunc != null)
            {
                return BindProducerFunc(name, outboundTarget, producerOptions);
            }

            return null;
        }

        // public void Dispose()
        // {
        // }
    }
}
