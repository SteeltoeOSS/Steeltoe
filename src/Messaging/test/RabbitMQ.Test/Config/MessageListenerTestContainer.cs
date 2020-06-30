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

using Steeltoe.Messaging.Rabbit.Listener;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class MessageListenerTestContainer : IMessageListenerContainer
    {
        internal bool StopInvoked;
        internal bool StartInvoked;
        internal bool DestroyInvoked;
        internal bool InitializationInvoked;
        internal IRabbitListenerEndpoint Endpoint;

        public MessageListenerTestContainer(IRabbitListenerEndpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public bool IsStarted => StartInvoked && InitializationInvoked;

        public bool IsStopped => StopInvoked && DestroyInvoked;

        public bool IsAutoStartup => true;

        public bool IsRunning
        {
            get => StartInvoked && !StopInvoked;
        }

        public int Phase => 0;

        public string ServiceName { get; set; } = nameof(MessageListenerTestContainer);

        public void Dispose()
        {
            if (!StopInvoked)
            {
                throw new InvalidOperationException("Stop should have been invoked before destroy on " + this);
            }

            DestroyInvoked = true;
        }

        public void Initialize()
        {
            InitializationInvoked = true;
        }

        public void LazyLoad()
        {
        }

        public void SetupMessageListener(IMessageListener messageListener)
        {
        }

        public Task Start()
        {
            if (!InitializationInvoked)
            {
                throw new InvalidOperationException("afterPropertiesSet should have been invoked before start on " + this);
            }

            if (StartInvoked)
            {
                throw new InvalidOperationException("Start already invoked on " + this);
            }

            StartInvoked = true;
            return Task.CompletedTask;
        }

        public Task Stop(Action callback)
        {
            StopInvoked = true;
            callback();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (StopInvoked)
            {
                throw new InvalidOperationException("Stop already invoked on " + this);
            }

            StopInvoked = true;
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("TestContainer{");
            sb.Append("endpoint=").Append(Endpoint);
            sb.Append(", startInvoked=").Append(StartInvoked);
            sb.Append(", initializationInvoked=").Append(InitializationInvoked);
            sb.Append(", stopInvoked=").Append(StopInvoked);
            sb.Append(", destroyInvoked=").Append(DestroyInvoked);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
