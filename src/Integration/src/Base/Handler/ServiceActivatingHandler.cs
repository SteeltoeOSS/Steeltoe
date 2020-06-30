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
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Handler
{
    public class ServiceActivatingHandler : AbstractReplyProducingMessageHandler, ILifecycle
    {
        private readonly IMessageProcessor _processor;

        // TODO:

        // public ServiceActivatingHandler(object instance)
        // : this(new MethodInvokingMessageProcessor(instance, typeof(ServiceActivatorAttribute)))
        // {
        // }

        // public ServiceActivatingHandler(object instance, MethodInfo method)
        // : this(new MethodInvokingMessageProcessor(instance, method))
        // {
        // }

        // public ServiceActivatingHandler(object instance, string methodName)
        // : this(new MethodInvokingMessageProcessor(instance, methodName))
        // {
        // }
        public ServiceActivatingHandler(IApplicationContext context, IMessageProcessor processor)
            : base(context)
        {
            _processor = processor;
        }

        public override string ComponentType => "service-activator";

        public virtual bool IsRunning => !(_processor is ILifecycle) || ((ILifecycle)_processor).IsRunning;

        public virtual async Task Start()
        {
            if (_processor is ILifecycle)
            {
                await ((ILifecycle)_processor).Start();
            }
        }

        public virtual async Task Stop()
        {
            if (_processor is ILifecycle)
            {
                await ((ILifecycle)_processor).Stop();
            }
        }

        public override string ToString()
        {
            return "ServiceActivator for [" + _processor + "]"
                    + (ComponentName == null ? string.Empty : " (" + ComponentName + ")");
        }

        protected override object HandleRequestMessage(IMessage message)
        {
            return _processor.ProcessMessage(message);
        }
    }
}
