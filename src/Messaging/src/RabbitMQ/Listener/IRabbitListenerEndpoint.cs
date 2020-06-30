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

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IRabbitListenerEndpoint
    {
        string Id { get; set; }

        int? Concurrency { get; set; }

        bool? AutoStartup { get; set; }

        IApplicationContext ApplicationContext { get; set; }

        void SetupListenerContainer(IMessageListenerContainer messageListenerContainer);

        ISmartMessageConverter MessageConverter { get; set; }

        bool BatchListener { get; set; }

        IBatchingStrategy BatchingStrategy { get; set; }

        AcknowledgeMode? AckMode { get; set; }

        IReplyPostProcessor ReplyPostProcessor { get; set; }

        string Group { get; set; }
    }
}
